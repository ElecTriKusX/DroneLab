using UnityEngine;

namespace DroneLab.Physics.Tests
{
    // Third-party stand-in: recorder must not require DroneTestPilot or a particular input device.
    public sealed class IndependentControlTelemetry : MonoBehaviour, IFlightControlTelemetry
    {
        public string ControlMode=>"External";
        public bool AltitudeHold=>true;
        public bool Saturated=>false;
        public FlightInput CurrentInput=>new FlightInput(.1,0,0,0,.25);
        public DVector3 DesiredRateLocal=>new DVector3(.2,0,0);
        public string ExportSettingsJson()=>"{\"source\":\"independent controller\"}";
    }
}
