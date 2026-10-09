using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal sealed class DroneCurvePlot : VisualElement
    {
        private List<Vector2> points=new List<Vector2>();
        private float minX,maxX,minY,maxY;
        private readonly List<Label> ticks=new List<Label>();
        private readonly Label caption,hover;
        private string unitX,unitY;
        public DroneCurvePlot()
        {
            AddToClassList("drone-curve-plot");generateVisualContent+=Draw;
            caption=new Label();caption.AddToClassList("curve-caption");caption.selection.isSelectable=false;caption.pickingMode=PickingMode.Ignore;Add(caption);
            hover=new Label();hover.AddToClassList("curve-hover");hover.selection.isSelectable=false;hover.pickingMode=PickingMode.Ignore;Add(hover);
            for(int i=0;i<12;i++){var label=new Label();label.AddToClassList("curve-tick");label.selection.isSelectable=false;label.pickingMode=PickingMode.Ignore;Add(label);ticks.Add(label);}
            RegisterCallback<GeometryChangedEvent>(_=>LayoutLabels());
            RegisterCallback<PointerMoveEvent>(e=> {
                if(points.Count==0)return;var location=(Vector2)e.localPosition;var nearest=points.OrderBy(p=>Vector2.Distance(Project(p),location)).First();
                hover.text=nearest.x.ToString("G5",CultureInfo.InvariantCulture)+" "+unitX+"  /  "+nearest.y.ToString("G5",CultureInfo.InvariantCulture)+" "+unitY;
            });RegisterCallback<PointerLeaveEvent>(_=>hover.text="");
        }
        public void Set(IEnumerable<Vector2> data,string x,string y,string message=null)
        {
            points=data.Where(p=>!float.IsNaN(p.x)&&!float.IsInfinity(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.y)).OrderBy(p=>p.x).ToList();unitX=x;unitY=y;
            minX=points.Count==0?0:Mathf.Min(0,points.Min(p=>p.x));maxX=points.Count==0?1:Mathf.Max(minX+.00001f,points.Max(p=>p.x));
            minY=points.Count==0?0:Mathf.Min(0,points.Min(p=>p.y));maxY=points.Count==0?1:Mathf.Max(minY+.00001f,points.Max(p=>p.y));
            if(maxY-minY<1e-9f)maxY=minY+1;caption.text=message??(y+"  /  "+x);hover.text="";LayoutLabels();MarkDirtyRepaint();
        }
        private Rect PlotRect=>new Rect(60,34,Mathf.Max(1,contentRect.width-86),Mathf.Max(1,contentRect.height-80));
        private Vector2 Project(Vector2 p){var r=PlotRect;return new Vector2(r.x+(p.x-minX)/(maxX-minX)*r.width,r.yMax-(p.y-minY)/(maxY-minY)*r.height);}
        private void LayoutLabels()
        {
            var r=PlotRect;for(int i=0;i<6;i++) {
                ticks[i].text=Mathf.Lerp(minX,maxX,i/5f).ToString("G3",CultureInfo.InvariantCulture);ticks[i].style.left=r.x+r.width*i/5-20;ticks[i].style.top=r.yMax+9;
                ticks[i+6].text=Mathf.Lerp(minY,maxY,i/5f).ToString("G3",CultureInfo.InvariantCulture);ticks[i+6].style.left=3;ticks[i+6].style.top=r.yMax-r.height*i/5-8;
            }
        }
        private void Draw(MeshGenerationContext c)
        {
            var p=c.painter2D;var r=PlotRect;p.lineWidth=1;p.strokeColor=new Color(.27f,.3f,.32f);
            void Line(Vector2 a,Vector2 b){p.BeginPath();p.MoveTo(a);p.LineTo(b);p.Stroke();}
            for(int i=0;i<6;i++){float x=r.x+r.width*i/5,y=r.y+r.height*i/5;Line(new Vector2(x,r.y),new Vector2(x,r.yMax));Line(new Vector2(r.x,y),new Vector2(r.xMax,y));}
            p.strokeColor=new Color(.73f,.76f,.77f);Line(new Vector2(r.x,r.y),new Vector2(r.x,r.yMax));Line(new Vector2(r.x,r.yMax),new Vector2(r.xMax,r.yMax));
            if(points.Count==0)return;p.lineWidth=2;p.strokeColor=Color.white;p.BeginPath();p.MoveTo(Project(points[0]));foreach(var v in points.Skip(1))p.LineTo(Project(v));p.Stroke();
            if(points.Count<=100)foreach(var v in points){p.BeginPath();p.Arc(Project(v),3,0,360);p.Stroke();}
        }
    }
}
