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

        private GltfImport imported;
        private int revision;
        public async Task<(bool success, string error)> LoadAsync(string path, Transform parent)
        {
            path = (path ?? "").Trim().Trim('"');
            if (!File.Exists(path)) return (false, "Файл модели не найден.");
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".glb" && extension != ".gltf") return (false, "Поддерживаются GLB и glTF 2.0.");
            int request = ++revision;
            var candidate = new GltfImport(); GameObject instance = null;
            try {
                bool parsed = await candidate.Load(new Uri(Path.GetFullPath(path)), new ImportSettings { GenerateMipMaps = true, AnisotropicFilterLevel = 4 });
                if (!parsed) return (false, "Не удалось прочитать модель. Проверьте glTF 2.0 и доступность её текстур/буферов.");
                if (this == null || request != revision || parent == null) return (false, "Загрузка отменена.");
                instance = new GameObject(Path.GetFileNameWithoutExtension(path)); instance.transform.SetParent(parent, false);
                // One stable wrapper regardless of the GLB scene's root count.
                // The default adds an extra Scene object when there are multiple roots,
                // which would change every persisted sibling-index path.
                var instantiator=new GameObjectInstantiator(candidate,instance.transform,
                    settings:new InstantiationSettings{SceneObjectCreation=SceneObjectCreation.Never});
                bool instantiated = await candidate.InstantiateMainSceneAsync(instantiator);
                if (!instantiated || this == null || request != revision || parent == null) return (false, "Не удалось создать 3D-модель или загрузка отменена.");
                // Keep the old model until the replacement is completely ready.
                ReleaseCurrent(); LoadedRoot = instance.transform; instance = null;
                imported = candidate; candidate = null; SourcePath = path;
                return (true, "");
            } catch (Exception ex) { Debug.LogException(ex); return (false, ex.Message); }
            finally { if (instance != null) Destroy(instance); candidate?.Dispose(); }
        }
        private void ReleaseCurrent()
        {
            if (LoadedRoot != null) { LoadedRoot.gameObject.SetActive(false); Destroy(LoadedRoot.gameObject); }
            LoadedRoot = null; SourcePath = ""; imported?.Dispose(); imported = null;
        }
        public void Clear() { ++revision; ReleaseCurrent(); }
        private void OnDestroy() => Clear();

        public static string PathFrom(Transform root, Transform node)
        {
            if (root == null || node == null || node == root) return "";

            // Sibling indices survive duplicate names, slashes and broken source encodings.
            string path = node.GetSiblingIndex().ToString(System.Globalization.CultureInfo.InvariantCulture);
            Transform current = node.parent;
            while (current != null && current != root)
            {
                path = current.GetSiblingIndex().ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" + path;
                current = current.parent;
            }

            return current == root ? "@/" + path : "";
        }

        public static Transform FindByPath(Transform root, string path)
        {
            if (root == null) return null;
            if(string.IsNullOrWhiteSpace(path))return root;
            if(!path.StartsWith("@/",StringComparison.Ordinal))return root.Find(path); // Existing documents.
            var node=root;
            foreach(string segment in path.Substring(2).Split('/')) {
                if(!int.TryParse(segment,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out int index) || index<0 || index>=node.childCount)return null;
                node=node.GetChild(index);
            }
            return node;
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
                Bounds localBounds = renderer.localBounds;
                Vector3 min = localBounds.min;
                Vector3 max = localBounds.max;
                Vector3[] corners =
                {
                    new Vector3(min.x,min.y,min.z), new Vector3(min.x,min.y,max.z),
                    new Vector3(min.x,max.y,min.z), new Vector3(min.x,max.y,max.z),
                    new Vector3(max.x,min.y,min.z), new Vector3(max.x,min.y,max.z),
                    new Vector3(max.x,max.y,min.z), new Vector3(max.x,max.y,max.z)
                };

                foreach (Vector3 rendererCorner in corners)
                {
                    Vector3 worldCorner = renderer.transform.TransformPoint(rendererCorner);
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
