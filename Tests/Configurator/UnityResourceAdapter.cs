// Test-only filesystem adapter. These tests run real profile/CSV/storage code, never a simulated UI or Rigidbody.
using System;
using System.IO;
namespace UnityEngine
{
    public sealed class TextAsset { public string text; public TextAsset(string text) { this.text=text; } }
    public static class Resources
    {
        public static T Load<T>(string path) where T:class
        {
            string file=Path.Combine(AppContext.BaseDirectory,"Resources",path+".json");
            return File.Exists(file)?new TextAsset(File.ReadAllText(file)) as T:null;
        }
    }
    public static class Application { public static string persistentDataPath=Path.Combine(Path.GetTempPath(),"DroneLab-configurator-tests-"+Guid.NewGuid().ToString("N")); }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3(0,0,0);
    }
    public static class Mathf
    {
        public const float PI=(float)Math.PI;
        public static int Clamp(int v,int lo,int hi)=>Math.Clamp(v,lo,hi);
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Sin(float a)=>(float)Math.Sin(a);
        public static float Cos(float a)=>(float)Math.Cos(a);
    }
}
