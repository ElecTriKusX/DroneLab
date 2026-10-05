using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DroneLab.Physics
{
    public sealed class PrincipalInertia
    {
        public readonly DVector3 Moments;
        private readonly double[] rotation;
        public double[] RotationXyzw=>(double[])rotation.Clone();
        internal PrincipalInertia(DVector3 moments,double[] quaternion) { Moments=moments; rotation=quaternion; }
        public InertiaProfile ToProfile()=>new InertiaProfile { mode="ManualPrincipal",
            principalMomentsKgM2=new[]{Moments.X,Moments.Y,Moments.Z},principalAxesRotationXyzw=RotationXyzw };
        public double[,] ToMatrixKgM2()
        {
            double x=rotation[0],y=rotation[1],z=rotation[2],w=rotation[3];
            var r=new double[,] { {1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)},
                {2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)}, {2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)} };
            var m=new[]{Moments.X,Moments.Y,Moments.Z}; var result=new double[3,3];
            for(int i=0;i<3;i++) for(int j=0;j<3;j++) for(int k=0;k<3;k++) result[i,j]+=r[i,k]*m[k]*r[j,k];
            return result;
        }
    }

    public static class InertiaTensor
    {
        // Symmetric Jacobi eigensolver. Input is the actual tensor about COM in physics-root axes.
        // Scale normalization avoids using an absolute tolerance for small UAV inertias.
        public static PrincipalInertia Diagonalize(double[,] matrix)
        {
            if(matrix==null || matrix.GetLength(0)!=3 || matrix.GetLength(1)!=3) throw new ArgumentException("A 3x3 tensor is required.");
            double scale=0;
            foreach(double value in matrix) { Finite(value); scale=Math.Max(scale,Math.Abs(value)); }
            if(scale==0) throw new ArgumentException("Inertia must be positive definite.");
            var a=new double[3,3]; var v=new double[,] { {1,0,0},{0,1,0},{0,0,1} };
            for(int i=0;i<3;i++) for(int j=0;j<3;j++)
            {
                if(Math.Abs(matrix[i,j]/scale-matrix[j,i]/scale)>1e-10) throw new ArgumentException("Inertia tensor must be symmetric.");
                a[i,j]=(matrix[i,j]/scale+matrix[j,i]/scale)/2;
            }
            for(int iteration=0;iteration<64;iteration++)
            {
                int p=0,q=1;
                if(Math.Abs(a[0,2])>Math.Abs(a[p,q])) { p=0; q=2; }
                if(Math.Abs(a[1,2])>Math.Abs(a[p,q])) { p=1; q=2; }
                if(Math.Abs(a[p,q])<1e-14) break;
                double phi=.5*Math.Atan2(2*a[p,q],a[q,q]-a[p,p]),c=Math.Cos(phi),s=Math.Sin(phi);
                double pp=a[p,p],qq=a[q,q],pq=a[p,q];
                a[p,p]=c*c*pp-2*s*c*pq+s*s*qq; a[q,q]=s*s*pp+2*s*c*pq+c*c*qq; a[p,q]=a[q,p]=0;
                for(int k=0;k<3;k++)
                {
                    if(k!=p && k!=q)
                    {
                        double kp=a[k,p],kq=a[k,q]; a[k,p]=a[p,k]=c*kp-s*kq; a[k,q]=a[q,k]=s*kp+c*kq;
                    }
                    double vp=v[k,p],vq=v[k,q]; v[k,p]=c*vp-s*vq; v[k,q]=s*vp+c*vq;
                }
            }
            var order=Enumerable.Range(0,3).OrderBy(i=>a[i,i]).ToArray();
            var values=order.Select(i=>a[i,i]*scale).ToArray();
            foreach(double value in values) { Finite(value); if(value<=0) throw new ArgumentException("Inertia must be positive definite."); }
            // Physical mass distributions obey Imax <= Iother1 + Iother2, not merely SPD.
            if(values[2]/scale>(values[0]/scale+values[1]/scale)*(1+1e-10)) throw new ArgumentException("Principal moments must satisfy triangle inequalities.");
            var axes=new double[3,3]; for(int i=0;i<3;i++) for(int j=0;j<3;j++) axes[i,j]=v[i,order[j]];
            double determinant=axes[0,0]*(axes[1,1]*axes[2,2]-axes[1,2]*axes[2,1])-axes[0,1]*(axes[1,0]*axes[2,2]-axes[1,2]*axes[2,0])+axes[0,2]*(axes[1,0]*axes[2,1]-axes[1,1]*axes[2,0]);
            if(determinant<0) for(int i=0;i<3;i++) axes[i,2]*=-1;
            return new PrincipalInertia(DVector3.From(values),Quaternion(axes));
        }
        internal static void Finite(double x) { if(double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentException("Finite inertia data required."); }
        private static double[] Quaternion(double[,] m)
        {
            double x,y,z,w,t=m[0,0]+m[1,1]+m[2,2];
            if(t>0)
            { double s=2*Math.Sqrt(t+1); w=s/4; x=(m[2,1]-m[1,2])/s; y=(m[0,2]-m[2,0])/s; z=(m[1,0]-m[0,1])/s; }
            else if(m[0,0]>m[1,1] && m[0,0]>m[2,2])
            { double s=2*Math.Sqrt(1+m[0,0]-m[1,1]-m[2,2]); w=(m[2,1]-m[1,2])/s; x=s/4; y=(m[0,1]+m[1,0])/s; z=(m[0,2]+m[2,0])/s; }
            else if(m[1,1]>m[2,2])
            { double s=2*Math.Sqrt(1+m[1,1]-m[0,0]-m[2,2]); w=(m[0,2]-m[2,0])/s; x=(m[0,1]+m[1,0])/s; y=s/4; z=(m[1,2]+m[2,1])/s; }
            else
            { double s=2*Math.Sqrt(1+m[2,2]-m[0,0]-m[1,1]); w=(m[1,0]-m[0,1])/s; x=(m[0,2]+m[2,0])/s; y=(m[1,2]+m[2,1])/s; z=s/4; }
            double n=Math.Sqrt(x*x+y*y+z*z+w*w)*(w<0 ? -1 : 1); return new[]{x/n,y/n,z/n,w/n};
        }
    }

    public static class CadInertiaImport
    {
        // Separate import document, not a new drone-profile mode. CAD frame/unit conversion is explicit.
        public static string Apply(string droneJson,string importJson)
        {
            var options=new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error };
            var source=JObject.Parse(importJson,options); var drone=JObject.Parse(droneJson,options);
            string[] keys={"schemaVersion","coordinateConvention","lengthUnit","inertiaUnit","tensorAbout","massKg","centerOfMassLocalM","matrixKgM2","source"};
            if(source.Properties().Any(p=>!keys.Contains(p.Name)) || keys.Any(k=>source[k]==null)) throw new ArgumentException("Invalid CAD inertia import fields.");
            void Require(string key,string expected) { if(source[key].Type!=JTokenType.String || (string)source[key]!=expected) throw new ArgumentException("CAD inertia requires "+key+"="+expected); }
            Require("schemaVersion","1.0.0"); Require("coordinateConvention","UnityLeftHandedYUpZForward"); Require("lengthUnit","m"); Require("inertiaUnit","kg*m^2"); Require("tensorAbout","CenterOfMass");
            double Number(JToken token)
            {
                if(token==null || (token.Type!=JTokenType.Float && token.Type!=JTokenType.Integer)) throw new ArgumentException("Numeric CAD inertia data required.");
                double value=(double)token; InertiaTensor.Finite(value); return value;
            }
            double mass=Number(source["massKg"]); if(mass<=0) throw new ArgumentException("Positive mass required.");
            var com=source["centerOfMassLocalM"] as JArray; if(com==null || com.Count!=3) throw new ArgumentException("Three COM coordinates required.");
            foreach(var value in com) Number(value);
            var rows=source["matrixKgM2"] as JArray; if(rows==null || rows.Count!=3) throw new ArgumentException("A 3x3 tensor is required.");
            var tensor=new double[3,3]; for(int i=0;i<3;i++)
            {
                var row=rows[i] as JArray; if(row==null || row.Count!=3) throw new ArgumentException("A 3x3 tensor is required.");
                for(int j=0;j<3;j++) tensor[i,j]=Number(row[j]);
            }
            if(source["source"].Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)source["source"])) throw new ArgumentException("A CAD/measurement source is required.");
            var principal=InertiaTensor.Diagonalize(tensor);
            var properties=drone["massProperties"] as JObject ?? throw new ArgumentException("Drone massProperties required.");
            properties["massKg"]=mass; properties["centerOfMassLocalM"]=com.DeepClone(); properties["inertia"]=JObject.FromObject(principal.ToProfile());
            var provenance=drone["parameterProvenance"] as JArray ?? throw new ArgumentException("Drone provenance required.");
            provenance.Add(new JObject { ["path"]="massProperties",["sourceType"]="User",["source"]="Explicit COM tensor import: "+(string)source["source"],["confidence"]=0.5 });
            drone.Remove("derived"); return drone.ToString()+"\n";
        }
    }
}
