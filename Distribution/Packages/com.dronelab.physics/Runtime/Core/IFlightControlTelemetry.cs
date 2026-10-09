namespace DroneLab.Physics
{
    // Optional observer only. A physics body can run with direct motor commands and no controller.
    // Controllers implement this interface to add their state/settings to flight recording.
    public interface IFlightControlTelemetry
    {
        string ControlMode { get; }
        bool AltitudeHold { get; }
        bool Saturated { get; }
        FlightInput CurrentInput { get; }
        DVector3 DesiredRateLocal { get; }
        string ExportSettingsJson();
    }
}
