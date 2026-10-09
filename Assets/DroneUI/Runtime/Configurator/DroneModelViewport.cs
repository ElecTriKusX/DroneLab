using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DroneLab.Configurator;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DroneLab.UI
{
    /// <summary>RenderTexture camera plus screen-space gizmos. All edited geometry stays in physical metres.</summary>
    internal sealed class DroneModelViewport : VisualElement, IDisposable
    {
        private const int PreviewLayer = 31;
        private readonly GameObject stage, modelFrame, procedural;
        private readonly Scene previewScene;
        private readonly Camera camera;
        private readonly RuntimeGltfModelLoader loader;
        private readonly VolumeProfile volumeProfile;
        private readonly Image image;
        private readonly GeometryOverlay overlay;
        private readonly IVisualElementScheduledItem tick;
        private RenderTexture texture;
        private DroneProfileDocument document;
        private bool disposed, dragging, orbiting, panning;
        private Vector2 previous;
        private Plane dragPlane;
        private Vector3 dragOffset;
        private Vector3 dragStartAxis;
        private readonly Dictionary<Camera,int> otherCameras=new Dictionary<Camera,int>();
        private readonly Dictionary<Light,int> otherLights=new Dictionary<Light,int>();
        private int draggedRotor=-1, axisLock=-1;
        private float yaw=35,pitch=25,distance=1;
        private Vector3 target;
        public int SelectedRotor { get; private set; }
        public string Tool { get; set; } = "Move";
        public bool Snap { get; set; }
        public bool EditingEnabled { get; set; }
        public string ViewName { get; private set; } = "3D";
        public string LastEditedField { get; private set; }
        private string schematicGeometry;
        public event Action<int> Selected;
        public event Action Changed;
        public event Action<string> Status;
        public bool HasImportedModel => loader.LoadedRoot != null;
        public Transform ModelRoot => loader.LoadedRoot;

        public DroneModelViewport()
        {
            AddToClassList("drone-viewport"); focusable=true;
            stage=new GameObject("DroneLab Configurator Preview") { hideFlags=HideFlags.DontSave, layer=PreviewLayer };
            previewScene=SceneManager.CreateScene("DroneLab Preview "+Guid.NewGuid().ToString("N"),new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            SceneManager.MoveGameObjectToScene(stage,previewScene);
            // Keep small-drone coordinates near zero, avoiding millimetre precision loss at large world offsets.
            foreach(var other in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)){otherCameras[other]=other.cullingMask;other.cullingMask&=~(1<<PreviewLayer);}
            foreach(var other in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){otherLights[other]=other.cullingMask;other.cullingMask&=~(1<<PreviewLayer);}
            modelFrame=Child("Model coordinate frame"); procedural=Child("Schematic model");
            loader=stage.AddComponent<RuntimeGltfModelLoader>();
            var cam=Child("Viewport camera"); camera=cam.AddComponent<Camera>();
            camera.cullingMask=1<<PreviewLayer; camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.07f,.07f,.07f); camera.nearClipPlane=.001f; camera.fieldOfView=38;
            var hd=cam.AddComponent<HDAdditionalCameraData>(); hd.clearColorMode=HDAdditionalCameraData.ClearColorMode.Color;
            hd.backgroundColorHDR=camera.backgroundColor; hd.volumeLayerMask=1<<PreviewLayer;
            var volume=Child("Isolated preview exposure").AddComponent<Volume>(); volume.isGlobal=true; volume.priority=10000;
            volumeProfile=ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile=volumeProfile;
            var exposure=volumeProfile.Add<Exposure>(); exposure.mode.Override(ExposureMode.Fixed); exposure.fixedExposure.Override(8);
            var tone=volumeProfile.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.Neutral);
            var light=Child("Preview key light").AddComponent<Light>(); light.type=LightType.Directional;
            light.transform.rotation=Quaternion.Euler(35,-40,0); light.cullingMask=1<<PreviewLayer; light.shadows=LightShadows.None;
            light.gameObject.AddComponent<HDAdditionalLightData>(); light.lightUnit=LightUnit.Lux; light.intensity=5000;
            var fill=Child("Preview fill light").AddComponent<Light>(); fill.type=LightType.Directional;
            fill.transform.rotation=Quaternion.Euler(145,60,0); fill.cullingMask=1<<PreviewLayer; fill.shadows=LightShadows.None;
            fill.gameObject.AddComponent<HDAdditionalLightData>(); fill.lightUnit=LightUnit.Lux; fill.intensity=1800;
            image=new Image { scaleMode=ScaleMode.StretchToFill, pickingMode=PickingMode.Ignore }; image.StretchToParentSize(); Add(image);
            overlay=new GeometryOverlay(this) { pickingMode=PickingMode.Ignore }; overlay.StretchToParentSize(); Add(overlay);
            RegisterCallback<GeometryChangedEvent>(_=>Resize());
            RegisterCallback<PointerDownEvent>(Down); RegisterCallback<PointerMoveEvent>(Move); RegisterCallback<PointerUpEvent>(Up);
            RegisterCallback<PointerCaptureOutEvent>(_=> { dragging=orbiting=panning=false; });
            RegisterCallback<WheelEvent>(e=> { LastEditedField=null;distance=Mathf.Clamp(distance*Mathf.Exp(e.delta.y*.045f),.005f,100000); UpdateCamera(); SaveView();Changed?.Invoke();e.StopPropagation(); });
            tick=schedule.Execute(()=> { if(!disposed) overlay.MarkDirtyRepaint(); }).Every(33);
            Resize(); UpdateCamera();
        }
        private GameObject Child(string name) { var go=new GameObject(name) { layer=PreviewLayer, hideFlags=HideFlags.DontSave }; go.transform.SetParent(stage.transform,false); return go; }
        private void Resize()
        {
            int w=Mathf.Clamp(Mathf.RoundToInt(resolvedStyle.width),128,2048),h=Mathf.Clamp(Mathf.RoundToInt(resolvedStyle.height),128,2048);
            if(texture!=null && texture.width==w && texture.height==h)return;
            if(texture!=null) { texture.Release(); Object.Destroy(texture); }
            texture=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32) { name="DroneLab Configurator Viewport" }; texture.Create();
            camera.targetTexture=texture; camera.aspect=(float)w/h; image.image=texture;
        }
        public void SetDocument(DroneProfileDocument value)
        {
            document=value; SelectedRotor=0;
            if(value.profile["coordinateSystem"]?["modelScaleMetersPerUnit"] is JValue scale)value.visual["scale"]=scale.DeepClone();
            yaw=(float?)value.visual["previewYaw"]??35; pitch=(float?)value.visual["previewPitch"]??25;
            camera.orthographic=(bool?)value.visual["previewOrthographic"]??false;
            ViewName=!camera.orthographic?"3D":Mathf.Abs(pitch-89.99f)<.1f?"Top":Mathf.Abs(yaw-180)<.1f?"Front":Mathf.Abs(yaw-90)<.1f?"Side":"3D";
            target=Vec(value.visual["previewTarget"]); distance=Mathf.Max(.005f,(float?)value.visual["previewDistance"]??1);
            loader.Clear(); RebuildSchematic(); UpdateCamera(); overlay.RefreshLabels();
        }
        public async Task<bool> LoadModel(string path,bool normalize)
        {
            DroneModelFiles.ValidateLocalModel(path);
            var result=await loader.LoadAsync(path,modelFrame.transform);
            if(disposed)return false;
            if(!result.success) { Status?.Invoke(result.error);return false; }
            foreach(var node in loader.LoadedRoot.GetComponentsInChildren<Transform>(true))node.gameObject.layer=PreviewLayer;
            if(normalize && RuntimeGltfModelLoader.TryGetBoundsInFrame(loader.LoadedRoot,modelFrame.transform,out var bounds)) {
                float size=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
                float wanted=Mathf.Max(Vec(document.profile["massProperties"]["dimensionsM"]).x,
                    Mathf.Max(Vec(document.profile["massProperties"]["dimensionsM"]).y,Vec(document.profile["massProperties"]["dimensionsM"]).z));
                if(size>1e-7f && wanted>0) document.visual["scale"]=(double)(wanted/size);
                document.visual["centerModel"]=true;
                document.profile["coordinateSystem"]["modelScaleMetersPerUnit"]=document.visual["scale"].DeepClone();
            }
            procedural.SetActive(false); ApplyTransform(); if(normalize)Frame();else UpdateCamera();
            Status?.Invoke(normalize ? "Модель вписана в габарит профиля. Проверьте её реальный размер в метрах." : "Модель загружена с сохранённым масштабом и материалами.");
            return true;
        }
        public void ApplyTransform()
        {
            if(!HasImportedModel)return;
            var t=loader.LoadedRoot; t.localPosition=Vector3.zero;
            t.localScale=Vector3.one*(float)((double?)document.visual["scale"]??1);
            t.localRotation=Quaternion.Euler(Vec(document.visual["rotationEulerDeg"]));
            if((bool?)document.visual["centerModel"]??true) {
                if(RuntimeGltfModelLoader.TryGetBoundsInFrame(t,modelFrame.transform,out var bounds))t.localPosition=-bounds.center;
            }
            overlay.MarkDirtyRepaint();
        }
        public Bounds ModelBounds()
        {
            if(HasImportedModel && RuntimeGltfModelLoader.TryGetBoundsInFrame(loader.LoadedRoot,modelFrame.transform,out var bounds))return bounds;
            return new Bounds(Vector3.zero,Vec(document?.profile?["massProperties"]?["dimensionsM"]));
        }
        public void GeometryChanged(bool rebuild=false) {
            string geometry=document.profile["massProperties"].ToString()+string.Join("|",Rotors.Select(r=>r["geometry"].ToString()+r["propeller"].ToString()));
            if(!HasImportedModel && (rebuild || geometry!=schematicGeometry)){RebuildSchematic();schematicGeometry=geometry;}
            overlay.RefreshLabels(); overlay.MarkDirtyRepaint();
        }
        public void Select(int rotor) { SelectedRotor=rotor; overlay.RefreshLabels(); overlay.MarkDirtyRepaint(); }
        public void View(string view)
        {
            ViewName=view;camera.orthographic=view!="3D";
            if(view=="Top") { yaw=0;pitch=89.99f; } else if(view=="Front") { yaw=180;pitch=0; }
            else if(view=="Side") { yaw=90;pitch=0; } else { yaw=35;pitch=25; }
            Frame();
        }
        public void Frame()
        {
            if(document==null)return; var b=ModelBounds();
            foreach(var rotor in Rotors)b.Encapsulate(Vec(rotor["geometry"]?["positionLocalM"]));
            float extent=Mathf.Max(.03f,Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z)));
            target=b.center; distance=extent*2.6f; UpdateCamera(); SaveView();
            LastEditedField=null;Changed?.Invoke();
        }
        private void UpdateCamera()
        {
            if(camera==null)return; var rotation=Quaternion.Euler(pitch,yaw,0);
            camera.transform.position=stage.transform.TransformPoint(target+rotation*new Vector3(0,0,-distance));
            camera.transform.rotation=rotation; camera.farClipPlane=Mathf.Max(5,distance*20);
            camera.nearClipPlane=Mathf.Max(.0001f,distance/10000); camera.orthographicSize=Mathf.Max(.001f,distance*.32f);
        }
        public void SaveView()
        {
            if(document==null)return; document.visual["previewYaw"]=yaw; document.visual["previewPitch"]=pitch;
            document.visual["previewDistance"]=distance;document.visual["previewTarget"]=Array(target);
            document.visual["previewOrthographic"]=camera.orthographic;
        }
        public byte[] Capture()
        {
            var previousRT=RenderTexture.active; var snapshot=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);
            try { RenderTexture.active=texture; snapshot.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);snapshot.Apply();return snapshot.EncodeToPNG(); }
            finally { RenderTexture.active=previousRT;Object.Destroy(snapshot); }
        }
        private JArray Rotors => document?.profile?["rotors"] as JArray ?? new JArray();
        private Ray Ray(Vector2 p)=>camera.ViewportPointToRay(new Vector3(p.x/Mathf.Max(1,contentRect.width),1-p.y/Mathf.Max(1,contentRect.height),0));
        private void Down(PointerDownEvent e)
        {
            LastEditedField=null;
            Focus(); previous=e.localPosition;
            if(e.button==1)orbiting=true;
            else if(e.button==2)panning=true;
            else if(e.button==0) {
                bool canEdit=EditingEnabled;
                int closest=-1; float best=22;
                for(int i=0;i<Rotors.Count;i++) { var q=Project(Vec(Rotors[i]["geometry"]["positionLocalM"]));float d=Vector2.Distance(q,e.localPosition);if(d<best){best=d;closest=i;} }
                axisLock=-1;
                if(canEdit && Tool=="Axis")for(int i=0;i<Rotors.Count;i++) {
                    var pos=Vec(Rotors[i]["geometry"]["positionLocalM"]);var axis=Vec(Rotors[i]["geometry"]["thrustAxisLocal"]);
                    if(Vector2.Distance(Project(pos+axis*distance*.16f),e.localPosition)<18)closest=i;
                }
                if(canEdit && Tool=="Move" && SelectedRotor>=0 && SelectedRotor<Rotors.Count) {
                    var p=Vec(Rotors[SelectedRotor]["geometry"]["positionLocalM"]);float len=distance*.13f;
                    for(int k=0;k<3;k++)if(Vector2.Distance(Project(p+Axis(k)*len),e.localPosition)<15){closest=SelectedRotor;axisLock=k;}
                }
                if(closest>=0) { SelectedRotor=draggedRotor=closest; Selected?.Invoke(closest);
                    var p=Vec(Rotors[closest]["geometry"]["positionLocalM"]); var world=stage.transform.TransformPoint(p);
                    dragStartAxis=Vec(Rotors[closest]["geometry"]["thrustAxisLocal"]);
                    dragPlane=new Plane(camera.transform.forward,world);
                    if(dragPlane.Raycast(Ray(e.localPosition),out var hit))dragOffset=world-Ray(e.localPosition).GetPoint(hit);
                    dragging=canEdit;
                }
            }
            if(dragging||orbiting||panning)this.CapturePointer(e.pointerId);e.StopPropagation();
        }
        private void Move(PointerMoveEvent e)
        {
            Vector2 delta=(Vector2)e.localPosition-previous; previous=e.localPosition;
            if(orbiting) { ViewName="3D";camera.orthographic=false;yaw-=delta.x*.35f;pitch=Mathf.Clamp(pitch+delta.y*.35f,-89.9f,89.9f);UpdateCamera(); }
            else if(panning) { target+=camera.transform.rotation*new Vector3(-delta.x,delta.y,0)*distance/Mathf.Max(1,contentRect.height)*.65f;UpdateCamera(); }
            else if(dragging && draggedRotor>=0 && draggedRotor<Rotors.Count) {
                var ray=Ray(e.localPosition); if(!dragPlane.Raycast(ray,out float hit))return;
                var geometry=(JObject)Rotors[draggedRotor]["geometry"];var old=Vec(geometry["positionLocalM"]);
                var point=stage.transform.InverseTransformPoint(ray.GetPoint(hit)+dragOffset);
                if(Tool=="Axis") {
                    // Drag the direction handle in the view plane, maintaining a unit vector.
                    var direction=(point-old)/Mathf.Max(.0001f,distance*.16f)+dragStartAxis;
                    if(direction.sqrMagnitude>1e-10f)geometry["thrustAxisLocal"]=Array(direction.normalized);
                } else {
                    if(axisLock>=0) { var constrained=old;constrained[axisLock]=point[axisLock];point=constrained; }
                    if(Snap)point=new Vector3(Mathf.Round(point.x*100)/100,Mathf.Round(point.y*100)/100,Mathf.Round(point.z*100)/100);
                    geometry["positionLocalM"]=Array(point);
                }
                overlay.MarkDirtyRepaint();
            }
            if(orbiting||panning||dragging)e.StopPropagation();
        }
        private void Up(PointerUpEvent e)
        {
            bool edit=dragging;dragging=orbiting=panning=false;if(this.HasPointerCapture(e.pointerId))this.ReleasePointer(e.pointerId);
            if(edit)LastEditedField="rotors["+draggedRotor+"].geometry."+(Tool=="Axis"?"thrustAxisLocal":"positionLocalM");
            SaveView();Changed?.Invoke();e.StopPropagation();
        }
        public Vector2 Project(Vector3 point)
        {
            var p=camera.WorldToViewportPoint(stage.transform.TransformPoint(point));
            if(p.z<=0)return new Vector2(-10000,-10000);
            return new Vector2(p.x*contentRect.width,(1-p.y)*contentRect.height);
        }
        internal static Vector3 Vec(JToken token) => token is JArray a && a.Count>=3 ? new Vector3((float)a[0],(float)a[1],(float)a[2]) : Vector3.zero;
        internal static JArray Array(Vector3 v)=>new JArray((double)v.x,(double)v.y,(double)v.z);
        private static Vector3 Axis(int k)=>k==0?Vector3.right:k==1?Vector3.up:Vector3.forward;
        private void RebuildSchematic()
        {
            for(int i=procedural.transform.childCount-1;i>=0;i--) { var go=procedural.transform.GetChild(i).gameObject;go.SetActive(false);Object.Destroy(go); }
            if(document==null)return;
            var dimensions=Vec(document.profile["massProperties"]["dimensionsM"]);
            Primitive(PrimitiveType.Cube,Vector3.zero,new Vector3(Mathf.Max(.01f,dimensions.x*.4f),Mathf.Max(.01f,dimensions.y),Mathf.Max(.01f,dimensions.z*.4f)));
            foreach(var rotor in Rotors) {
                var p=Vec(rotor["geometry"]["positionLocalM"]); float diameter=(float?)rotor["propeller"]?["diameterM"]??.127f;
                Primitive(PrimitiveType.Cylinder,p,new Vector3(diameter,.004f,diameter));
                var arm=Primitive(PrimitiveType.Cube,p*.5f,new Vector3(.015f,.01f,Mathf.Max(.01f,p.magnitude)));arm.transform.localRotation=Quaternion.LookRotation(p.sqrMagnitude>0?p:Vector3.forward);
            }
        }
        private GameObject Primitive(PrimitiveType type,Vector3 position,Vector3 scale)
        {
            var go=GameObject.CreatePrimitive(type);go.layer=PreviewLayer;go.transform.SetParent(procedural.transform,false);
            go.transform.localPosition=position;go.transform.localScale=scale;Object.Destroy(go.GetComponent<Collider>());
            // A shared HDRP material is loaded from Resources and retained by the build.
            go.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("DroneLab/ConfiguratorSchematic");return go;
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;tick.Pause();camera.enabled=false;loader.Clear();
            camera.targetTexture=null;if(texture!=null){texture.Release();Object.Destroy(texture);} Object.Destroy(volumeProfile);Object.Destroy(stage);
            if(previewScene.IsValid() && previewScene.isLoaded)SceneManager.UnloadSceneAsync(previewScene);
            foreach(var pair in otherCameras)if(pair.Key!=null)pair.Key.cullingMask=pair.Value;
            foreach(var pair in otherLights)if(pair.Key!=null)pair.Key.cullingMask=pair.Value;
        }
        private sealed class GeometryOverlay : VisualElement
        {
            private readonly DroneModelViewport v;
            private readonly List<Label> labels=new List<Label>();
            public GeometryOverlay(DroneModelViewport v){this.v=v;generateVisualContent+=Draw;}
            public void RefreshLabels()
            {
                Clear();labels.Clear();foreach(var rotor in v.Rotors){var label=new Label((string)rotor["rotorId"]);label.AddToClassList("rotor-label");label.selection.isSelectable=false;label.pickingMode=PickingMode.Ignore;Add(label);labels.Add(label);}
                var com=new Label("+ COM");com.AddToClassList("rotor-label");com.selection.isSelectable=false;com.pickingMode=PickingMode.Ignore;Add(com);labels.Add(com);
            }
            private void Draw(MeshGenerationContext c)
            {
                if(v.document==null)return;var p=c.painter2D;float grid=Mathf.Max(.01f,v.distance/4);
                p.lineWidth=1;p.strokeColor=new Color(.3f,.3f,.3f,.35f);
                for(int i=-8;i<=8;i++){Line(p,v.Project(new Vector3(i*grid,0,-8*grid)),v.Project(new Vector3(i*grid,0,8*grid)));Line(p,v.Project(new Vector3(-8*grid,0,i*grid)),v.Project(new Vector3(8*grid,0,i*grid)));}
                for(int i=0;i<v.Rotors.Count;i++) {
                    var rotor=v.Rotors[i];var pos=Vec(rotor["geometry"]["positionLocalM"]);var axis=Vec(rotor["geometry"]["thrustAxisLocal"]);
                    var center=v.Project(pos);bool selected=i==v.SelectedRotor;float len=v.distance*.16f;
                    p.lineWidth=selected?2.5f:1.5f;p.strokeColor=selected?Color.white:new Color(.72f,.72f,.72f);
                    p.BeginPath();p.Arc(center,selected?12:8,0,360);p.Stroke();
                    Arrow(p,center,v.Project(pos+axis*len));
                    if(i<labels.Count){labels[i].style.left=center.x+15;labels[i].style.top=center.y-25;labels[i].text=(string)rotor["rotorId"]+" · "+(string)rotor["geometry"]["spinDirection"];}
                    if(v.EditingEnabled && selected && v.Tool=="Move")for(int k=0;k<3;k++) {p.strokeColor=k==0?new Color(.9f,.6f,.6f):k==1?new Color(.65f,.85f,.7f):new Color(.65f,.75f,.95f);Arrow(p,center,v.Project(pos+Axis(k)*v.distance*.13f));}
                }
                var com=v.Project(Vec(v.document.profile["massProperties"]["centerOfMassLocalM"]));
                if(labels.Count>v.Rotors.Count){labels.Last().style.left=com.x;labels.Last().style.top=com.y;}
            }
            private static void Line(Painter2D p,Vector2 a,Vector2 b){if(a.x < -9000 || b.x < -9000)return;p.BeginPath();p.MoveTo(a);p.LineTo(b);p.Stroke();}
            private static void Arrow(Painter2D p,Vector2 a,Vector2 b){Line(p,a,b);var d=(b-a).normalized;if(d.sqrMagnitude<.01)return;var side=new Vector2(-d.y,d.x);Line(p,b,b-d*10+side*5);Line(p,b,b-d*10-side*5);}
        }
    }
}
