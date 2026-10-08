using UnityEngine;

namespace DroneLab.UI
{
    internal sealed class DroneProfileAssetLifetime : MonoBehaviour
    {
        public TextAsset asset;
        private void OnDestroy() { if (asset != null) Destroy(asset); }
    }

}
