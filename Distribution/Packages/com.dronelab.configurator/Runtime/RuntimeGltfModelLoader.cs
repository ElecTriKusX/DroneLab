using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace DroneLab.Configurator
{
    public sealed class RuntimeGltfModelLoader : MonoBehaviour
    {
        public Transform LoadedRoot { get; private set; }
        public string SourcePath { get; private set; }

        public async Task<(bool success, string error)> LoadAsync(string path, Transform parent)
        {
            path = (path ?? "").Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return (false, "Model file not found.");

            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".glb" && extension != ".gltf")
                return (false, "Only .glb/.gltf are supported by the runtime importer.");

            Clear();

            try
            {
                var gltf = new GltfImport();
                bool parsed = await gltf.Load(new Uri(Path.GetFullPath(path)));
                if (!parsed) return (false, "glTFast could not parse the file.");

                var instance = new GameObject(Path.GetFileNameWithoutExtension(path));
                instance.transform.SetParent(parent, false);

                bool instantiated = await gltf.InstantiateMainSceneAsync(instance.transform);
                if (!instantiated)
                {
                    Destroy(instance);
                    return (false, "glTF was parsed, but its main scene could not be instantiated.");
                }

                LoadedRoot = instance.transform;
                SourcePath = path;
                return (true, "");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Clear();
                return (false, ex.Message);
            }
        }

        public void Clear()
        {
            if (LoadedRoot != null)
                Destroy(LoadedRoot.gameObject);

            LoadedRoot = null;
            SourcePath = "";
        }

        public static string PathFrom(Transform root, Transform node)
        {
            if (root == null || node == null || node == root) return "";

            string path = node.name;
            Transform current = node.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return current == root ? path : "";
        }

        public static Transform FindByPath(Transform root, string path)
        {
            if (root == null) return null;
            return string.IsNullOrWhiteSpace(path) ? root : root.Find(path);
        }

        public static bool TryGetWorldBounds(Transform root, out Bounds bounds)
        {
            bounds = new Bounds();
            if (root == null) return false;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool initialized = false;

            foreach (Renderer renderer in renderers)
            {
                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return initialized;
        }

        public static bool TryGetBoundsInFrame(Transform root, Transform frame, out Bounds bounds)
        {
            bounds = new Bounds();
            if (root == null || frame == null) return false;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool initialized = false;

            foreach (Renderer renderer in renderers)
            {
                Bounds world = renderer.bounds;
                Vector3 min = world.min;
                Vector3 max = world.max;
                Vector3[] corners =
                {
                    new Vector3(min.x,min.y,min.z), new Vector3(min.x,min.y,max.z),
                    new Vector3(min.x,max.y,min.z), new Vector3(min.x,max.y,max.z),
                    new Vector3(max.x,min.y,min.z), new Vector3(max.x,min.y,max.z),
                    new Vector3(max.x,max.y,min.z), new Vector3(max.x,max.y,max.z)
                };

                foreach (Vector3 worldCorner in corners)
                {
                    Vector3 local = frame.InverseTransformPoint(worldCorner);
                    if (!initialized)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        initialized = true;
                    }
                    else bounds.Encapsulate(local);
                }
            }

            return initialized;
        }
    }
}
