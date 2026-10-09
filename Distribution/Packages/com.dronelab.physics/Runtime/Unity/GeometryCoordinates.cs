using UnityEngine;

namespace DroneLab.Simulation
{
    public static class GeometryCoordinates
    {
        // Physics root/ancestors have unit scale. Child/import scale is applied exactly once.
        public static Vector3 MeshVertexInMeters(Transform root,Transform mesh,Vector3 vertex)
            => root.InverseTransformPoint(mesh.TransformPoint(vertex));
    }
}
