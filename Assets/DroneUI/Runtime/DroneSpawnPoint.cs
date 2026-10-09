using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneLab.UI
{
    /// <summary>Surface position and horizontal heading; independent of drone size and model scale.</summary>
    [DisallowMultipleComponent]
    public sealed class DroneSpawnPoint : MonoBehaviour
    {
        [Tooltip("Match the map's Spawn Point Id. With one point the map may leave it empty.")]
        public string id = "main";
        [Min(0), Tooltip("Gap between the physical envelope's underside and this surface, in metres.")]
        public float clearanceM = .03f;
        public Quaternion Heading => Quaternion.Euler(0, transform.eulerAngles.y, 0);
        public Vector3 Position(Vector3 dimensions) => transform.position + Vector3.up * (dimensions.y * .5f + clearanceM);
        public static DroneSpawnPoint Resolve(Scene scene, string requestedId)
        {
            var points = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<DroneSpawnPoint>(true))
                .Where(point => point.isActiveAndEnabled && (string.IsNullOrWhiteSpace(requestedId) || point.id == requestedId)).ToList();
            if (points.Count == 0) throw new InvalidOperationException("На карте не найдена точка старта" +
                (string.IsNullOrWhiteSpace(requestedId) ? "." : " «" + requestedId + "».") + " Добавьте DroneSpawnPoint на площадку и сохраните сцену.");
            if (points.Count != 1) throw new InvalidOperationException("На карте несколько подходящих точек старта. Укажите уникальный идентификатор в каталоге карты.");
            var point = points[0];
            if (float.IsNaN(point.clearanceM) || float.IsInfinity(point.clearanceM) || point.clearanceM < 0)
                throw new InvalidOperationException("Зазор точки старта должен быть конечным и неотрицательным.");
            return point;
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(.85f, .93f, .9f); var center = transform.position;
            Gizmos.DrawWireCube(center + Vector3.up * .02f, new Vector3(.6f, .04f, .6f));
            var forward = Heading * Vector3.forward; Gizmos.DrawLine(center, center + forward);
            Gizmos.DrawLine(center + forward, center + forward * .75f + Heading * Vector3.right * .15f);
            Gizmos.DrawLine(center + forward, center + forward * .75f - Heading * Vector3.right * .15f);
        }
    }
}
