using System;
using System.Collections.Generic;
using DroneLab.Physics;
using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    /// <summary>F5 plans world-space goals; the pilot remains the only motor-command producer.</summary>
    internal sealed class DroneFlightRoutePlanner
    {
        private readonly DronePhysicsBody body;
        private readonly DroneTestPilot pilot;
        private readonly DroneSensorRig rig;
        private readonly DroneFlightCamera camera;
        private readonly VisualElement panel;
        private readonly ScrollView content;
        private readonly VisualElement pointList;
        private readonly Label status;
        private readonly RouteOverlay overlay;
        private readonly List<Vector3> points=new List<Vector3>();
        private readonly WaypointMission mission=new WaypointMission();
        private bool visible, previousReadKeyboard;
        private DroneFlightCameraMode previousCamera;
        private float altitude=10, speed=3;
        private Vector3 routeOrigin;
        public bool Editing {
            get {
                var focused=panel.panel?.focusController?.focusedElement as VisualElement;
                for(var p=focused;p!=null;p=p.parent) if(p is FloatField || p is DoubleField || p is TextField) return visible;
                return false;
            }
        }
        public DroneFlightRoutePlanner(VisualElement parent, DronePhysicsBody selected, DroneTestPilot controller, DroneSensorRig sensors, DroneFlightCamera flightCamera)
        {
            body=selected; pilot=controller; rig=sensors; camera=flightCamera;
            overlay=new RouteOverlay(camera); parent.Add(overlay);
            panel=new VisualElement(); panel.AddToClassList("flight-route-panel"); parent.Add(panel);
            panel.RegisterCallback<PointerEnterEvent>(_=>camera.PointerOverUI=true);
            panel.RegisterCallback<PointerLeaveEvent>(_=>camera.PointerOverUI=false);
            Text(panel,"F5 · МАРШРУТ","flight-title");
            Text(panel,"WASD — камера · Q/E — высота камеры · ПКМ — обзор · ЛКМ по поверхности — точка","flight-note");
            content=new ScrollView(); content.AddToClassList("flight-route-content"); DroneFlightControls.ThemeScroll(content); panel.Add(content);
            Number(content,"Высота над выбранной поверхностью, м",altitude,2,100,x=>altitude=x);
            Number(content,"Скорость маршрута, м/с",speed,.5f,10,x=> { speed=x; if(pilot!=null) pilot.NavigationSpeedMps=x; });
            Text(content,"Точки задают прямые участки. Перед запуском проверьте высоту над препятствиями; автоматического обхода препятствий нет.","flight-note");
            Button(content,"Запустить маршрут",Start);
            Button(content,"Остановить и удерживать точку",Stop);
            Button(content,"Удалить последнюю точку",()=> { if(mission.Active || points.Count==0) return; points.RemoveAt(points.Count-1); RefreshPoints(); });
            Button(content,"Очистить маршрут",()=> { if(mission.Active) return; points.Clear(); RefreshPoints(); });
            status=Text(content,"Добавьте точки по клику на поверхность.","flight-note");
            pointList=new VisualElement(); content.Add(pointList);
            panel.style.display=DisplayStyle.None; overlay.style.display=DisplayStyle.None;
        }
        public void SetView(DroneFlightViewMode view)
        {
            bool next=view==DroneFlightViewMode.Route;
            if(next && !visible) {
                previousReadKeyboard=pilot?.readKeyboard ?? false; previousCamera=camera.Mode;
                if(pilot!=null) { pilot.readKeyboard=false; pilot.SetFlightInput(default); if(body.Armed && !pilot.PositionHold) { pilot.autoLevel=true; pilot.altitudeHold=true; pilot.HoldPosition(); } }
                camera.SetMode(DroneFlightCameraMode.Free);
            } else if(!next && visible) {
                panel.panel?.focusController?.focusedElement?.Blur();
                if(pilot!=null) { pilot.readKeyboard=previousReadKeyboard; pilot.SetFlightInput(default); }
                camera.SetMode(previousCamera); camera.SnapToTarget();
            }
            visible=next; panel.style.display=next ? DisplayStyle.Flex : DisplayStyle.None;
            overlay.style.display=view!=DroneFlightViewMode.Cinema && (next || mission.Active || mission.Completed) ? DisplayStyle.Flex : DisplayStyle.None;
            camera.PointerOverUI=false;
        }
        public void Tick(bool paused)
        {
            if(!visible || paused || Editing || !Application.isFocused || camera.PointerOverUI || mission.Active) return;
            var mouse=Mouse.current;
            if(mouse?.leftButton.wasPressedThisFrame!=true || points.Count>=63) return;
            var ray=camera.Camera.ScreenPointToRay(mouse.position.ReadValue());
            var hits=UnityEngine.Physics.RaycastAll(ray,10000,UnityEngine.Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits) {
                if(hit.collider==null || hit.collider.transform.IsChildOf(body.transform)) continue;
                points.Add(hit.point+Vector3.up*altitude); RefreshPoints();
                status.text="Точек: "+points.Count+". Высота учитывается при добавлении каждой точки."; break;
            }
        }
        private void Start()
        {
            if(pilot==null || points.Count==0) { status.text="Добавьте хотя бы одну точку."; return; }
            if(!rig.TryHorizontal(out _,out _)) { status.text="GPS недоступен: маршрут не запущен."; return; }
            var route=new List<DVector3>();
            // Climb vertically before departing the takeoff point.
            var departure=body.Body.position; departure.y=Mathf.Max(departure.y+2,points[0].y);
            route.Add(DronePhysicsBody.FromUnity(departure));
            foreach(var point in points) route.Add(DronePhysicsBody.FromUnity(point));
            mission.ArrivalRadiusM=Math.Max(.8,Math.Min(3,rig.Settings(DroneLab.Sensors.SensorKind.Gps).noiseStd*2.5));
            mission.ArrivalSpeedMps=Math.Max(.7,Math.Min(1.5,rig.Settings(DroneLab.Sensors.SensorKind.Gps).noiseStd*2));
            mission.Start(route); routeOrigin=departure; pilot.NavigationSpeedMps=speed; body.SetArmed(true);
            pilot.SetFlightInput(default); pilot.SetNavigationTarget(DronePhysicsBody.ToUnity(mission.Target));
            status.text="Взлёт к высоте маршрута · радиус прибытия "+mission.ArrivalRadiusM.ToString("0.0")+" м"; rig.Log("Маршрут запущен: "+points.Count+" точек");
        }
        private void Stop()
        {
            mission.Stop(); if(pilot!=null) { pilot.SetFlightInput(default); if(!pilot.HoldPosition()) pilot.CancelPositionHold("GPS недоступен: только удержание высоты"); }
            status.text="Маршрут остановлен"; rig.Log(status.text);
        }
        public void FixedTick(float dt)
        {
            if(!mission.Active || pilot==null) return;
            bool valid=rig.TryHorizontal(out var position,out var velocity);
            if(!valid || !pilot.PositionHold || !body.Armed) {
                mission.Stop(); pilot.CancelPositionHold(!valid ? "GPS потерян: маршрут остановлен" : "Маршрут прерван управлением / выключением моторов");
                status.text=pilot.NavigationMessage; rig.Log(status.text); return;
            }
            position=new DVector3(position.X,body.Body.position.y,position.Z);
            double actualSpeed=Math.Sqrt(velocity.Length*velocity.Length+body.Body.linearVelocity.y*body.Body.linearVelocity.y);
            if(mission.Step(position,actualSpeed,dt,true)) {
                if(mission.Active) pilot.SetNavigationTarget(DronePhysicsBody.ToUnity(mission.Target));
                else { status.text="Маршрут завершён · удержание последней точки"; rig.Log(status.text); }
            }
            if(mission.Active) status.text=mission.Index==0 ? "Взлёт к высоте маршрута" : "Точка "+mission.Index+" / "+points.Count;
        }
        public void Reset()
        { mission.Stop(); if(pilot!=null) pilot.CancelPositionHold("Возврат на старт"); status.text="Маршрут остановлен: возврат на старт"; }
        public void RefreshOverlay()
        {
            if(visible || mission.Active || mission.Completed) overlay.Refresh(mission.Active || mission.Completed ? routeOrigin : body.transform.position,points);
        }
        private void RefreshPoints()
        {
            pointList.Clear();
            for(int i=0;i<points.Count;i++) {
                int index=i; var row=new VisualElement(); row.AddToClassList("flight-route-point"); pointList.Add(row);
                var p=points[i]; Text(row,(i+1)+" · X "+p.x.ToString("0.0")+" / Z "+p.z.ToString("0.0")+" / Y "+p.y.ToString("0.0"),"flight-note");
                Button(row,"×",()=> { if(mission.Active) return; points.RemoveAt(index); RefreshPoints(); });
            }
        }
        private static Label Text(VisualElement parent,string text,string css) { var label=new Label(text); label.AddToClassList(css); parent.Add(label); return label; }
        private static void Button(VisualElement parent,string text,Action action) { var button=new Button(action) { text=text, focusable=false }; button.AddToClassList("pilot-button"); button.RegisterCallback<PointerDownEvent>(_=>button.panel?.focusController?.focusedElement?.Blur()); parent.Add(button); }
        private static void Number(VisualElement parent,string caption,float value,float min,float max,Action<float> change)
        {
            var field=new FloatField(caption) { value=value,isDelayed=true }; field.AddToClassList("flight-number"); parent.Add(field);
            field.RegisterValueChangedCallback(evt=> { float v=float.IsNaN(evt.newValue)||float.IsInfinity(evt.newValue) ? value : Mathf.Clamp(evt.newValue,min,max); field.SetValueWithoutNotify(v); change(v); });
        }
        private sealed class RouteOverlay : VisualElement
        {
            private readonly DroneFlightCamera camera;
            private readonly List<Vector2> lines=new List<Vector2>();
            private readonly Label[] labels=new Label[64];
            public RouteOverlay(DroneFlightCamera selected)
            {
                camera=selected; AddToClassList("flight-scene-overlay"); pickingMode=PickingMode.Ignore;
                for(int i=0;i<labels.Length;i++) { labels[i]=new Label((i+1).ToString()) { pickingMode=PickingMode.Ignore }; labels[i].AddToClassList("flight-vector-caption"); Add(labels[i]); }
                generateVisualContent+=Draw;
            }
            public void Refresh(Vector3 position,IReadOnlyList<Vector3> route)
            {
                lines.Clear(); foreach(var label in labels) label.style.display=DisplayStyle.None;
                var rect=contentRect;
                bool Project(Vector3 p,out Vector2 point) { var v=camera.Camera.WorldToViewportPoint(p); point=new Vector2(v.x*rect.width,(1-v.y)*rect.height); return v.z>camera.Camera.nearClipPlane; }
                Vector3 previous=position;
                for(int i=0;i<route.Count;i++) {
                    if(Project(previous,out var a) && Project(route[i],out var b) && DroneFlightMath.ClipSegment(rect,ref a,ref b)) {
                        float length=Vector2.Distance(a,b);
                        if(length>.01f) for(float t=0;t<length;t+=14) { lines.Add(Vector2.Lerp(a,b,t/length)); lines.Add(Vector2.Lerp(a,b,Mathf.Min(t+8,length)/length)); }
                    }
                    if(Project(route[i],out var p) && rect.Contains(p)) { labels[i].style.left=p.x; labels[i].style.top=p.y-25; labels[i].style.display=DisplayStyle.Flex; }
                    previous=route[i];
                }
                MarkDirtyRepaint();
            }
            private void Draw(MeshGenerationContext context)
            {
                var painter=context.painter2D; painter.strokeColor=new Color(.85f,.85f,.85f); painter.lineWidth=2;
                for(int i=0;i+1<lines.Count;i+=2) { painter.BeginPath(); painter.MoveTo(lines[i]); painter.LineTo(lines[i+1]); painter.Stroke(); }
            }
        }
    }
}
