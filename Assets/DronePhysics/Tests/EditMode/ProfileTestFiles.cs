using System.IO;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    internal static class ProfileTestFiles
    {
        public static string Read(string name)
        {
#if UNITY_EDITOR
            var asset=UnityEngine.Resources.Load<UnityEngine.TextAsset>("DronePhysics/"+name);
            Assert.That(asset,Is.Not.Null,"Missing profile resource: "+name);
            return asset.text;
#else
            return File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"Profiles",name+".json"));
#endif
        }
    }
}
