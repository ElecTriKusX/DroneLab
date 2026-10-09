using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
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
        private CancellationTokenSource silhouetteCancellation;
        private IVisualElementScheduledItem silhouetteTimer;
        private int silhouetteRevision;
        private string silhouetteStamp;
        private bool loadingModel;
        public bool SilhouetteBusy {get;private set;}
        public string SilhouetteError {get;private set;}
        public event Action SilhouetteChanged;
        private string GeometryStamp()=>ModelRoot.GetInstanceID()+"|"+document.visual["scale"]+"|"+document.visual["rotationEulerDeg"]+"|"+document.visual["rotorNodes"]+"|"+ProjectionResolution;
        public int ProjectionResolution=>Mathf.Clamp((int?)document.visual["silhouetteResolution"]??128,16,512);
        private void CancelSilhouette()
        {
            silhouetteTimer?.Pause();silhouetteRevision++;silhouetteCancellation?.Cancel();silhouetteCancellation?.Dispose();silhouetteCancellation=null;SilhouetteBusy=false;
        }
        private void QueueSilhouette()
        {
            if(disposed || loadingModel || !HasImportedModel)return;
            string stamp=GeometryStamp();
            if(stamp==silhouetteStamp) {
                if(!SilhouetteBusy && document.profile["derived"]?["projectedAreaLut"] is JArray samples && DroneMeshProjection.Apply(document.profile,samples))SilhouetteChanged?.Invoke();
                return;
            }
            CancelSilhouette();silhouetteStamp=stamp;SilhouetteBusy=true;SilhouetteError=null;DroneMeshProjection.Invalidate(document.profile);
            silhouetteTimer=schedule.Execute(async()=>await BakeSilhouette()).StartingIn(300);
        }
        public Task RecalculateSilhouette()=>BakeSilhouette();
        private async Task BakeSilhouette()
        {
            if(disposed || !HasImportedModel)return;
            silhouetteTimer?.Pause();CancelSilhouette();int revision=silhouetteRevision;
            silhouetteStamp=GeometryStamp();SilhouetteBusy=true;SilhouetteError=null;
            DroneMeshProjection.Invalidate(document.profile);SilhouetteChanged?.Invoke();
            silhouetteCancellation=new CancellationTokenSource();var cancellation=silhouetteCancellation.Token;
            var targetDocument=document;
            try {
                rotorVisuals.Restore();
                DroneMeshSnapshot.Read(ModelRoot,modelFrame.transform,(JObject)document.visual["rotorNodes"],out var vertices,out var triangles);
                int resolution=ProjectionResolution;
                var samples=await Task.Run(()=>DroneMeshProjection.Bake(vertices,triangles,resolution,cancellation),cancellation);
                if(disposed || revision!=silhouetteRevision || targetDocument!=document)return;
                DroneMeshProjection.Apply(document.profile,samples);
                Status?.Invoke($"Силуэт корпуса пересчитан: 13 осей · разрешение {resolution}."+
                    (((JObject)document.visual["rotorNodes"]).Count==0?" Винты не привязаны: их геометрия пока включена в силуэт.":""));
            } catch(OperationCanceledException) { }
            catch(Exception ex) {
                if(!disposed && revision==silhouetteRevision){SilhouetteError=ex.Message;Status?.Invoke("Не удалось пересчитать силуэт: "+ex.Message);}
            } finally {if(!disposed && revision==silhouetteRevision){SilhouetteBusy=false;SilhouetteChanged?.Invoke();}}
        }
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
        private bool disposed, dragging, draggingCom, orbiting, panning;
        private Vector2 previous;
        private Plane dragPlane;
        private Vector3 dragOffset;
        private Vector3 dragStartAxis;
        private readonly Dictionary<Camera,int> otherCameras=new Dictionary<Camera,int>();
        private readonly Dictionary<Light,int> otherLights=new Dictionary<Light,int>();
        private int draggedRotor=-1, axisLock=-1;
        private float yaw=35,pitch=25,distance=1;
        private Vector3 target;
        public int SelectedRotor { get; private set; } = -1;
        public bool CenterOfMassSelected { get; private set; }
        public bool MassEditingEnabled { get; private set; }
        public string Tool { get; set; } = "Move";
        public bool Snap { get; set; }
        public bool EditingEnabled { get; set; }
        public string ViewName { get; private set; } = "3D";
        public string LastEditedField { get; private set; }
        private string schematicGeometry;
        private Transform selectedModelNode;
        private readonly DroneRotorVisuals rotorVisuals=new DroneRotorVisuals();
        private double lastVisualTime;
        public bool TestRotorSpin { get; set; }
        public event Action<int> Selected;
        public event Action SelectionCleared;
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
            // Temporary preview cameras use one graphics queue. This is a local mitigation,
            // not proof of the cause of an intermittent HDRP GPUFence error.
            hd.customRenderingSettings=true;
            hd.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.AsyncCompute]=true;
            hd.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.AsyncCompute,false);
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
            RegisterCallback<PointerCaptureOutEvent>(_=> { dragging=draggingCom=orbiting=panning=false; });
            RegisterCallback<WheelEvent>(e=> { LastEditedField=null;distance=Mathf.Clamp(distance*Mathf.Exp(e.delta.y*.045f),.005f,100000); UpdateCamera(); SaveView();Changed?.Invoke();e.StopPropagation(); });
            tick=schedule.Execute(()=> { if(!disposed){
                double now=Time.realtimeSinceStartupAsDouble;
                if(TestRotorSpin)rotorVisuals.Step(stage.transform,Rotors,i=>i==SelectedRotor?2*Math.PI:0,Math.Min(.1,Math.Max(0,now-lastVisualTime)));
                else rotorVisuals.Restore();
                lastVisualTime=now;overlay.UpdateLabels();overlay.MarkDirtyRepaint();
            } }).Every(33);
            Resize(); UpdateCamera();
        }
        private GameObject Child(string name) { var go=new GameObject(name) { layer=PreviewLayer, hideFlags=HideFlags.DontSave }; go.transform.SetParent(stage.transform,false); return go; }
        private void Resize()
        {
            if(disposed)return;
            int w=Mathf.Clamp(Mathf.RoundToInt(resolvedStyle.width),128,2048),h=Mathf.Clamp(Mathf.RoundToInt(resolvedStyle.height),128,2048);
            if(texture!=null && texture.width==w && texture.height==h)return;
            var old=texture;
            texture=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32) { name="DroneLab Configurator Viewport" }; texture.Create();
            camera.targetTexture=texture; camera.aspect=(float)w/h; image.image=texture;
            // Detach the old target before deferred destruction; do not force Release
            // while HDRP/UI may still have submitted work using it this frame.
            if(old!=null)Object.Destroy(old);
        }
        public void SetDocument(DroneProfileDocument value)
        {
            CancelSilhouette();silhouetteStamp=null;SilhouetteError=null;
            document=value; SelectedRotor=-1;CenterOfMassSelected=false;
            selectedModelNode=null;TestRotorSpin=false;rotorVisuals.Restore();
            if(value.profile["coordinateSystem"]?["modelScaleMetersPerUnit"] is JValue scale)value.visual["scale"]=scale.DeepClone();
            yaw=(float?)value.visual["previewYaw"]??35; pitch=(float?)value.visual["previewPitch"]??25;
            camera.orthographic=(bool?)value.visual["previewOrthographic"]??false;
            ViewName=!camera.orthographic?"3D":Mathf.Abs(pitch-89.99f)<.1f?"Top":Mathf.Abs(yaw-180)<.1f?"Front":Mathf.Abs(yaw-90)<.1f?"Side":"3D";
            target=Vec(value.visual["previewTarget"]); distance=Mathf.Max(.005f,(float?)value.visual["previewDistance"]??1);
            loader.Clear(); RebuildSchematic(); UpdateCamera(); overlay.RefreshLabels();
        }
        public async Task<bool> LoadModel(string path,bool normalize)
        {
            CancelSilhouette();silhouetteStamp=null;
            DroneModelFiles.ValidateLocalModel(path);
            var result=await loader.LoadAsync(path,modelFrame.transform);
            if(disposed)return false;
            if(!result.success) { Status?.Invoke(result.error);return false; }
            selectedModelNode=null;TestRotorSpin=false;
            if(normalize)document.visual["rotorNodes"]=new JObject();
            foreach(var node in loader.LoadedRoot.GetComponentsInChildren<Transform>(true))node.gameObject.layer=PreviewLayer;
            if(normalize && RuntimeGltfModelLoader.TryGetBoundsInFrame(loader.LoadedRoot,modelFrame.transform,out var bounds)) {
                float size=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
                float wanted=Mathf.Max(Vec(document.profile["massProperties"]["dimensionsM"]).x,
                    Mathf.Max(Vec(document.profile["massProperties"]["dimensionsM"]).y,Vec(document.profile["massProperties"]["dimensionsM"]).z));
                if(size>1e-7f && wanted>0) document.visual["scale"]=(double)(wanted/size);
                document.visual["centerModel"]=true;
                document.profile["coordinateSystem"]["modelScaleMetersPerUnit"]=document.visual["scale"].DeepClone();
            }
            loadingModel=true;procedural.SetActive(false);try{ApplyTransform();}finally{loadingModel=false;}if(normalize)Frame();else UpdateCamera();
            RefreshRotorBindings();
            Status?.Invoke(normalize ? "Модель вписана в габарит профиля. Проверьте её реальный размер в метрах." : "Модель загружена с сохранённым масштабом и материалами.");
            await BakeSilhouette();
            return true;
        }
        public void ApplyTransform()
        {
            if(!HasImportedModel)return;
            rotorVisuals.Restore();
            var t=loader.LoadedRoot; t.localPosition=Vector3.zero;
            t.localScale=Vector3.one*(float)((double?)document.visual["scale"]??1);
            t.localRotation=Quaternion.Euler(Vec(document.visual["rotationEulerDeg"]));
            if((bool?)document.visual["centerModel"]??true) {
                if(RuntimeGltfModelLoader.TryGetBoundsInFrame(t,modelFrame.transform,out var bounds))t.localPosition=-bounds.center;
            }
            overlay.MarkDirtyRepaint();
            QueueSilhouette();
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
        public void Select(int rotor) { SelectedRotor=rotor;CenterOfMassSelected=false;TestRotorSpin=false;
            selectedModelNode=rotor>=0 && rotor<Rotors.Count?RuntimeGltfModelLoader.FindByPath(ModelRoot,DroneRotorVisuals.Paths(document.visual["rotorNodes"][(string)Rotors[rotor]["rotorId"]]).FirstOrDefault()??"__unbound__"):null;
            overlay.RefreshLabels(); overlay.MarkDirtyRepaint(); }
        public void SelectModelNode(Transform node){selectedModelNode=node;overlay.MarkDirtyRepaint();}
        public void RefreshRotorBindings(){rotorVisuals.Bind(ModelRoot,Rotors,(JObject)document.visual["rotorNodes"]);TestRotorSpin=false;if(rotorVisuals.Issues.Count>0)Status?.Invoke("Проверьте привязки винтов: "+string.Join("; ",rotorVisuals.Issues));}
        public void SetEditContext(bool rotors,bool mass)
        {
            if(EditingEnabled==rotors && MassEditingEnabled==mass)return;
            EditingEnabled=rotors;MassEditingEnabled=mass;
            if(!rotors){selectedModelNode=null;TestRotorSpin=false;rotorVisuals.Restore();}
            if(mass)SelectedRotor=-1;if(!mass)CenterOfMassSelected=false;overlay.RefreshLabels();overlay.MarkDirtyRepaint();
        }
        public void ClearSelection()
        {
            SelectedRotor=-1;CenterOfMassSelected=false;draggedRotor=-1;selectedModelNode=null;TestRotorSpin=false;rotorVisuals.Restore();SelectionCleared?.Invoke();overlay.MarkDirtyRepaint();
        }
        public void ScaleModelToPhysicalDimensions()
        {
            if(!HasImportedModel)throw new InvalidOperationException("Сначала импортируйте модель.");
            var bounds=ModelBounds();double current=(double?)document.visual["scale"]??1;
            double scale=DroneProfileEdits.UniformScaleToDimensions((JArray)document.profile["massProperties"]["dimensionsM"],Array(bounds.size),current);
            document.profile["coordinateSystem"]["modelScaleMetersPerUnit"]=scale;document.visual["scale"]=scale;ApplyTransform();Frame();
        }
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
            if(MassEditingEnabled){b.Encapsulate(new Bounds(Vector3.zero,Vec(document.profile["massProperties"]["dimensionsM"])));b.Encapsulate(Vec(document.profile["massProperties"]["centerOfMassLocalM"]));}
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
            LastEditedField=null;Focus();previous=e.localPosition;
            if(e.button==1)orbiting=true;
            else if(e.button==2)panning=true;
            else if(e.button==0) {
                axisLock=-1;
                if(MassEditingEnabled) {
                    var com=Vec(document.profile["massProperties"]["centerOfMassLocalM"]);
                    bool hit=Vector2.Distance(Project(com),e.localPosition)<22;
                    if(CenterOfMassSelected)for(int k=0;k<3;k++)if(Vector2.Distance(Project(com+Axis(k)*distance*.13f),e.localPosition)<15){hit=true;axisLock=k;}
                    if(hit){CenterOfMassSelected=true;SelectedRotor=-1;draggingCom=true;BeginDrag(com,e.localPosition);}
                    else ClearSelection();
                } else {
                    bool canEdit=EditingEnabled;int closest=-1;float best=22;
                    for(int i=0;i<Rotors.Count;i++){float d=Vector2.Distance(Project(Vec(Rotors[i]["geometry"]["positionLocalM"])),e.localPosition);if(d<best){best=d;closest=i;}}
                    if(canEdit && Tool=="Axis")for(int i=0;i<Rotors.Count;i++){
                        var pos=Vec(Rotors[i]["geometry"]["positionLocalM"]);var axis=Vec(Rotors[i]["geometry"]["thrustAxisLocal"]);
                        if(Vector2.Distance(Project(pos+axis*distance*.16f),e.localPosition)<18)closest=i;
                    }
                    if(canEdit && Tool=="Move" && SelectedRotor>=0 && SelectedRotor<Rotors.Count){
                        var pos=Vec(Rotors[SelectedRotor]["geometry"]["positionLocalM"]);
                        for(int k=0;k<3;k++)if(Vector2.Distance(Project(pos+Axis(k)*distance*.13f),e.localPosition)<15){closest=SelectedRotor;axisLock=k;}
                    }
                    if(closest>=0){SelectedRotor=draggedRotor=closest;CenterOfMassSelected=false;Selected?.Invoke(closest);
                        dragStartAxis=Vec(Rotors[closest]["geometry"]["thrustAxisLocal"]);BeginDrag(Vec(Rotors[closest]["geometry"]["positionLocalM"]),e.localPosition);dragging=canEdit;
                    } else ClearSelection();
                }
            }
            if(dragging||draggingCom||orbiting||panning)this.CapturePointer(e.pointerId);e.StopPropagation();
        }
        private void BeginDrag(Vector3 point,Vector2 pointer)
        {
            var world=stage.transform.TransformPoint(point);dragPlane=new Plane(camera.transform.forward,world);dragOffset=Vector3.zero;
            var ray=Ray(pointer);if(dragPlane.Raycast(ray,out var hit))dragOffset=world-ray.GetPoint(hit);
        }
        private Vector3 DragPosition(Vector3 point,Vector3 old)
        {
            if(axisLock>=0){var constrained=old;constrained[axisLock]=point[axisLock];point=constrained;}
            if(Snap){if(axisLock>=0)point[axisLock]=Mathf.Round(point[axisLock]*100)/100;else point=new Vector3(Mathf.Round(point.x*100)/100,Mathf.Round(point.y*100)/100,Mathf.Round(point.z*100)/100);}
            return point;
        }
        private void Move(PointerMoveEvent e)
        {
            Vector2 delta=(Vector2)e.localPosition-previous;previous=e.localPosition;
            if(orbiting){ViewName="3D";camera.orthographic=false;yaw+=delta.x*.35f;pitch=Mathf.Clamp(pitch+delta.y*.35f,-89.9f,89.9f);UpdateCamera();}
            else if(panning){target+=camera.transform.rotation*new Vector3(-delta.x,delta.y,0)*distance/Mathf.Max(1,contentRect.height)*.65f;UpdateCamera();}
            else if(draggingCom || dragging && draggedRotor>=0 && draggedRotor<Rotors.Count){
                var ray=Ray(e.localPosition);if(!dragPlane.Raycast(ray,out float hit))return;var point=stage.transform.InverseTransformPoint(ray.GetPoint(hit)+dragOffset);
                if(draggingCom){var mass=(JObject)document.profile["massProperties"];mass["centerOfMassLocalM"]=Array(DragPosition(point,Vec(mass["centerOfMassLocalM"])));}
                else {
                    var geometry=(JObject)Rotors[draggedRotor]["geometry"];var old=Vec(geometry["positionLocalM"]);
                    if(Tool=="Axis"){
                        var direction=(point-old)/Mathf.Max(.0001f,distance*.16f)+dragStartAxis;
                        if(direction.sqrMagnitude>1e-10f)geometry["thrustAxisLocal"]=Array(direction.normalized);
                    } else geometry["positionLocalM"]=Array(DragPosition(point,old));
                }
                overlay.MarkDirtyRepaint();
            }
            if(orbiting||panning||dragging||draggingCom)e.StopPropagation();
        }
        private void Up(PointerUpEvent e)
        {
            bool comEdit=draggingCom,rotorEdit=dragging;dragging=draggingCom=orbiting=panning=false;
            if(this.HasPointerCapture(e.pointerId))this.ReleasePointer(e.pointerId);
            if(comEdit)LastEditedField="massProperties.centerOfMassLocalM";
            else if(rotorEdit)LastEditedField="rotors["+draggedRotor+"].geometry."+(Tool=="Axis"?"thrustAxisLocal":"positionLocalM");
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
            procedural.SetActive(!HasImportedModel);
            for(int i=procedural.transform.childCount-1;i>=0;i--) { var go=procedural.transform.GetChild(i).gameObject;go.SetActive(false);Object.Destroy(go); }
            if(document==null)return;
            DroneSchematicModel.Build(procedural.transform,document,false,PreviewLayer);
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;CancelSilhouette();tick.Pause();camera.enabled=false;loader.Clear();
            camera.targetTexture=null;image.image=null;if(texture!=null){Object.Destroy(texture);texture=null;} Object.Destroy(volumeProfile);Object.Destroy(stage);
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
                if(labels.Count!=v.Rotors.Count+1) {
                    Clear();labels.Clear();
                    for(int i=0;i<v.Rotors.Count+1;i++) {
                        var label=new Label();label.AddToClassList("rotor-label");label.selection.isSelectable=false;label.pickingMode=PickingMode.Ignore;
                        Add(label);labels.Add(label);
                    }
                }
                UpdateLabels();
            }
            // Run from the scheduler / input callbacks, never from generateVisualContent.
            public void UpdateLabels()
            {
                if(v.document==null)return;
                for(int i=0;i<v.Rotors.Count && i<labels.Count;i++) {
                    var rotor=v.Rotors[i];var center=v.Project(Vec(rotor["geometry"]["positionLocalM"]));
                    string caption=(string)rotor["rotorId"]+" · "+(string)rotor["geometry"]["spinDirection"];
                    if(v.EditingEnabled && i==v.SelectedRotor)caption+=$"\nD = {(double?)rotor["propeller"]?["diameterM"]??0:G4} м · {rotor["propeller"]?["bladeCount"]} лоп.";
                    PositionLabel(labels[i],center+new Vector2(15,-25),caption);
                }
                if(labels.Count>v.Rotors.Count)PositionLabel(labels.Last(),v.Project(Vec(v.document.profile["massProperties"]["centerOfMassLocalM"])),"+ COM");
            }
            private static void PositionLabel(Label label,Vector2 position,string text)
            {
                if(label.text!=text)label.text=text;
                if(label.style.left.value.value!=position.x)label.style.left=position.x;
                if(label.style.top.value.value!=position.y)label.style.top=position.y;
            }
            private void Draw(MeshGenerationContext c)
            {
                if(v.document==null)return;var p=c.painter2D;float grid=Mathf.Max(.01f,v.distance/4);
                if(v.selectedModelNode!=null){
                    p.strokeColor=new Color(.7f,.86f,1f,.9f);p.lineWidth=1.5f;
                    foreach(var renderer in v.selectedModelNode.GetComponentsInChildren<Renderer>(true)){
                        var bounds=renderer.localBounds;
                        Vector3 Corner(int index)=>v.stage.transform.InverseTransformPoint(renderer.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((index&1)==0?-1:1,(index&2)==0?-1:1,(index&4)==0?-1:1))));
                        for(int corner=0;corner<8;corner++)for(int axis=0;axis<3;axis++)if((corner&(1<<axis))==0)Line(p,v.Project(Corner(corner)),v.Project(Corner(corner|(1<<axis))));
                    }
                }
                p.lineWidth=1;p.strokeColor=new Color(.3f,.3f,.3f,.35f);
                for(int i=-8;i<=8;i++){Line(p,v.Project(new Vector3(i*grid,0,-8*grid)),v.Project(new Vector3(i*grid,0,8*grid)));Line(p,v.Project(new Vector3(-8*grid,0,i*grid)),v.Project(new Vector3(8*grid,0,i*grid)));}
                if(v.MassEditingEnabled) {
                    var half=Vec(v.document.profile["massProperties"]["dimensionsM"])*.5f;p.strokeColor=new Color(.78f,.8f,.8f);p.lineWidth=1.5f;
                    for(int corner=0;corner<8;corner++)for(int axis=0;axis<3;axis++)if((corner & (1<<axis))==0) {
                        Vector3 Point(int index)=>new Vector3((index&1)==0?-half.x:half.x,(index&2)==0?-half.y:half.y,(index&4)==0?-half.z:half.z);
                        Line(p,v.Project(Point(corner)),v.Project(Point(corner | (1<<axis))));
                    }
                    var com=Vec(v.document.profile["massProperties"]["centerOfMassLocalM"]);var center=v.Project(com);
                    p.strokeColor=Color.white;p.lineWidth=v.CenterOfMassSelected?3:2;p.BeginPath();p.Arc(center,v.CenterOfMassSelected?12:9,0,360);p.Stroke();
                    Line(p,center-new Vector2(15,0),center+new Vector2(15,0));Line(p,center-new Vector2(0,15),center+new Vector2(0,15));
                    if(v.CenterOfMassSelected)for(int k=0;k<3;k++){p.strokeColor=k==0?new Color(.9f,.6f,.6f):k==1?new Color(.65f,.85f,.7f):new Color(.65f,.75f,.95f);Arrow(p,center,v.Project(com+Axis(k)*v.distance*.13f));}
                }
                for(int i=0;i<v.Rotors.Count;i++) {
                    var rotor=v.Rotors[i];var pos=Vec(rotor["geometry"]["positionLocalM"]);var axis=Vec(rotor["geometry"]["thrustAxisLocal"]);
                    var center=v.Project(pos);bool selected=i==v.SelectedRotor;float len=v.distance*.16f;
                    if(v.EditingEnabled)DrawPropeller(p,rotor,pos,axis,selected);
                    p.lineWidth=selected?2.5f:1.5f;p.strokeColor=selected?Color.white:new Color(.72f,.72f,.72f);
                    p.BeginPath();p.Arc(center,selected?12:8,0,360);p.Stroke();
                    Arrow(p,center,v.Project(pos+axis*len));
                    if(v.EditingEnabled && selected && v.Tool=="Move")for(int k=0;k<3;k++) {p.strokeColor=k==0?new Color(.9f,.6f,.6f):k==1?new Color(.65f,.85f,.7f):new Color(.65f,.75f,.95f);Arrow(p,center,v.Project(pos+Axis(k)*v.distance*.13f));}
                }
            }
            private void DrawPropeller(Painter2D p,JToken rotor,Vector3 position,Vector3 axis,bool selected)
            {
                float diameter=(float?)rotor["propeller"]?["diameterM"]??0;
                if(diameter<=0 || float.IsNaN(diameter) || float.IsInfinity(diameter) || axis.sqrMagnitude<1e-10f)return;
                axis.Normalize();
                var tangent=Vector3.Cross(axis,Mathf.Abs(Vector3.Dot(axis,Vector3.up))>.9f?Vector3.right:Vector3.up).normalized;
                var bitangent=Vector3.Cross(axis,tangent);float radius=diameter*.5f;
                Vector3 Rim(float angle)=>position+radius*(tangent*Mathf.Cos(angle)+bitangent*Mathf.Sin(angle));
                p.lineWidth=selected?2:1.2f;p.strokeColor=selected?new Color(.9f,.94f,.96f,.9f):new Color(.7f,.76f,.79f,.65f);
                const int segments=72;
                for(int step=0;step<segments;step++)Line(p,v.Project(Rim(step*2*Mathf.PI/segments)),v.Project(Rim((step+1)*2*Mathf.PI/segments)));
                long blades=(long?)rotor["propeller"]?["bladeCount"]??0;
                // Extreme diagnostic profiles keep their actual count in the label;
                // skip unreadable spokes rather than silently draw a different count.
                if(blades<=64)for(int blade=0;blade<blades;blade++)Line(p,v.Project(position),v.Project(Rim(blade*2*Mathf.PI/blades)));
            }
            private static void Line(Painter2D p,Vector2 a,Vector2 b){if(a.x < -9000 || b.x < -9000)return;p.BeginPath();p.MoveTo(a);p.LineTo(b);p.Stroke();}
            private static void Arrow(Painter2D p,Vector2 a,Vector2 b){Line(p,a,b);var d=(b-a).normalized;if(d.sqrMagnitude<.01)return;var side=new Vector2(-d.y,d.x);Line(p,b,b-d*10+side*5);Line(p,b,b-d*10-side*5);}
        }
    }
}
