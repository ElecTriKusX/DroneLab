using UnityEngine;

namespace DroneLab.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DroneLab/UI/Карты и окружение")]
    public sealed class DroneScenarioController : MonoBehaviour
    {
        [SerializeField] private DroneScenarioCatalog catalog;
        public DroneScenarioCatalog Catalog => catalog != null ? catalog : DroneScenarioCatalog.Load();
    }
}
