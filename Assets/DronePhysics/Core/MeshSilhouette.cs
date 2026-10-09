using System;
using System.Collections.Generic;

namespace DroneLab.Physics
{
    // Offline orthographic silhouette rasterizer. Union coverage, never sum of triangle areas.
    public static class MeshSilhouette
    {
        public static DVector3[] BakeDirections()
        {
            var directions=new List<DVector3>();
            for(int x=-1;x<=1;x++) for(int y=-1;y<=1;y++) for(int z=-1;z<=1;z++)
                if(x>0 || (x==0 && y>0) || (x==0 && y==0 && z>0))
                    directions.Add(new DVector3(x,y,z).Normalized);
            return directions.ToArray(); // 13 axes, 26 signed directions.
        }
        public static double Area(DVector3[] vertices,int[] triangles,DVector3 direction,int resolution)
        {
            if(vertices==null || vertices.Length==0 || triangles==null || triangles.Length==0 || triangles.Length%3!=0)
                throw new ArgumentException("A nonempty triangle mesh is required.");
            if(resolution<16 || resolution>512) throw new ArgumentOutOfRangeException(nameof(resolution));
            if(direction.Length<1e-12) throw new ArgumentException("Nonzero projection direction required.");
            var used=new bool[vertices.Length];
            foreach(int index in triangles)
            {
                if(index<0 || index>=vertices.Length) throw new ArgumentException("Triangle index is outside the vertex array.");
                used[index]=true;
            }
            var d=direction.Normalized;
            var u=DVector3.Cross(Math.Abs(d.Y)<0.9 ? new DVector3(0,1,0) : new DVector3(1,0,0),d).Normalized;
            var v=DVector3.Cross(d,u);
            var xs=new double[vertices.Length]; var ys=new double[vertices.Length];
            double xmin=double.PositiveInfinity,xmax=double.NegativeInfinity,ymin=xmin,ymax=xmax;
            for(int i=0;i<vertices.Length;i++)
            {
                if(!used[i]) continue;
                xs[i]=DVector3.Dot(vertices[i],u); ys[i]=DVector3.Dot(vertices[i],v);
                if(double.IsNaN(xs[i]) || double.IsInfinity(xs[i]) || double.IsNaN(ys[i]) || double.IsInfinity(ys[i]))
                    throw new ArgumentException("Finite mesh coordinates required.");
                xmin=Math.Min(xmin,xs[i]); xmax=Math.Max(xmax,xs[i]); ymin=Math.Min(ymin,ys[i]); ymax=Math.Max(ymax,ys[i]);
            }
            double pixel=Math.Max(xmax-xmin,ymax-ymin)/resolution;
            if(pixel<=1e-12 || xmax<=xmin || ymax<=ymin) return 0;
            int width=Math.Min(resolution,(int)Math.Ceiling((xmax-xmin)/pixel));
            int height=Math.Min(resolution,(int)Math.Ceiling((ymax-ymin)/pixel));
            for(int i=0;i<vertices.Length;i++) { xs[i]=(xs[i]-xmin)/pixel; ys[i]=(ys[i]-ymin)/pixel; }
            var covered=new bool[width*height]; int count=0;
            for(int t=0;t<triangles.Length;t+=3)
            {
                int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
                double signed=Edge(xs[a],ys[a],xs[b],ys[b],xs[c],ys[c]);
                if(Math.Abs(signed)<1e-12) continue;
                int x0=Math.Max(0,(int)Math.Ceiling(Math.Min(xs[a],Math.Min(xs[b],xs[c]))-0.5));
                int x1=Math.Min(width-1,(int)Math.Floor(Math.Max(xs[a],Math.Max(xs[b],xs[c]))-0.5));
                int y0=Math.Max(0,(int)Math.Ceiling(Math.Min(ys[a],Math.Min(ys[b],ys[c]))-0.5));
                int y1=Math.Min(height-1,(int)Math.Floor(Math.Max(ys[a],Math.Max(ys[b],ys[c]))-0.5));
                double sign=signed>0 ? 1:-1;
                for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++)
                {
                    int index=y*width+x;
                    if(!covered[index] && sign*Edge(xs[a],ys[a],xs[b],ys[b],x+0.5,y+0.5)>=-1e-9 &&
                        sign*Edge(xs[b],ys[b],xs[c],ys[c],x+0.5,y+0.5)>=-1e-9 &&
                        sign*Edge(xs[c],ys[c],xs[a],ys[a],x+0.5,y+0.5)>=-1e-9)
                    { covered[index]=true; count++; }
                }
            }
            return count*pixel*pixel;
        }
        private static double Edge(double ax,double ay,double bx,double by,double x,double y)
            => (bx-ax)*(y-ay)-(by-ay)*(x-ax);
    }
}
