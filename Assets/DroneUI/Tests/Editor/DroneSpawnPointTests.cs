using System;
using DroneLab.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneLab.UI.Tests
{
    public sealed class DroneSpawnPointTests
    {
        private Scene scene;
        [SetUp] public void SetUp() => scene=SceneManager.CreateScene("SpawnPointTests-"+Guid.NewGuid().ToString("N"));
        [TearDown] public void TearDown() => EditorSceneManager.CloseScene(scene,true);
        private DroneSpawnPoint Create(string id)
        {
            var go=new GameObject("Spawn "+id);SceneManager.MoveGameObjectToScene(go,scene);var point=go.AddComponent<DroneSpawnPoint>();point.id=id;return point;
        }
        [Test] public void PhysicalUndersideStaysAbovePadRegardlessOfDroneHeight()
        {
            var point=Create("main");point.transform.position=new Vector3(4,2,-3);point.transform.rotation=Quaternion.Euler(20,70,-10);
            foreach(float height in new[]{.05f,.1f,.6f}) {
                var position=point.Position(new Vector3(1,height,1));Assert.That(position.y-height*.5f,Is.EqualTo(2+point.clearanceM).Within(1e-5));
                Assert.That(position.x,Is.EqualTo(4));Assert.That(position.z,Is.EqualTo(-3));
            }
            Assert.That(Vector3.Dot(point.Heading*Vector3.up,Vector3.up),Is.EqualTo(1).Within(1e-5));
        }
        [Test] public void AsymmetricEnvelopeClearsThePadWithoutMovingTheHorizontalOrigin()
        {
            var point=Create("main");point.transform.position=new Vector3(4,2,-3);
            var bounds=new Bounds(new Vector3(-.2f,-.3f,.1f),new Vector3(1,.8f,1));
            var position=point.Position(bounds);
            Assert.That(position.y+bounds.min.y,Is.EqualTo(2+point.clearanceM).Within(1e-5));
            Assert.That(position.x,Is.EqualTo(4));Assert.That(position.z,Is.EqualTo(-3));
        }
        [Test] public void MissingOrAmbiguousStartIsRejectedAndIdSelectsTheCorrectPad()
        {
            Assert.Throws<InvalidOperationException>(()=>DroneSpawnPoint.Resolve(scene,null));
            var first=Create("main");Assert.That(DroneSpawnPoint.Resolve(scene,null),Is.SameAs(first));
            var second=Create("alternate");Assert.Throws<InvalidOperationException>(()=>DroneSpawnPoint.Resolve(scene,null));
            Assert.That(DroneSpawnPoint.Resolve(scene,"alternate"),Is.SameAs(second));
            second.id="main";Assert.Throws<InvalidOperationException>(()=>DroneSpawnPoint.Resolve(scene,"main"));
            second.gameObject.SetActive(false);Assert.That(DroneSpawnPoint.Resolve(scene,"main"),Is.SameAs(first));
        }
        [Test] public void NegativeAndNonfiniteClearanceAreRejected()
        {
            var point=Create("main");foreach(float value in new[]{-1,float.NaN,float.PositiveInfinity}) {
                point.clearanceM=value;Assert.Throws<InvalidOperationException>(()=>DroneSpawnPoint.Resolve(scene,"main"));
            }
        }
    }
}
