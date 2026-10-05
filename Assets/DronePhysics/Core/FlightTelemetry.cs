using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DroneLab.Physics
{
    public readonly struct RotorTelemetry
    {
        public readonly double Command,DriveAuthority,Rpm,Thrust,ReactionTorque,AdvanceRatio,GroundGain,InducedHoverSpeed;
        public readonly double? MeasuredCurrent,BusCurrent,GroundHeight;
        public readonly DVector3 WindWorld,DragWorld;
        public readonly bool Clamped;
        public readonly double ThrustCorrection;
        public readonly DVector3 FlappingMomentWorld;
        public readonly bool FlowClamped;
        public readonly double? PropellerTorque,AccelerationTorque,SpinEnergy,MotorCurrent,MotorLoss,EscLoss;
        public readonly double? EvaluationRpm;
        public RotorTelemetry(double command,double drive,double rpm,double thrust,double torque,double advance,
            double? measuredCurrent,double? busCurrent,double? groundHeight,double groundGain,double induced,
            DVector3 wind,DVector3 drag,bool clamped,double thrustCorrection=0,DVector3 flappingMoment=default,bool flowClamped=false,
            double? propellerTorque=null,double? accelerationTorque=null,double? spinEnergy=null,double? motorCurrent=null,double? motorLoss=null,double? escLoss=null,double? evaluationRpm=null)
        { Command=command; DriveAuthority=drive; Rpm=rpm; Thrust=thrust; ReactionTorque=torque; AdvanceRatio=advance;
          MeasuredCurrent=measuredCurrent; BusCurrent=busCurrent; GroundHeight=groundHeight; GroundGain=groundGain;
          InducedHoverSpeed=induced; WindWorld=wind; DragWorld=drag; Clamped=clamped;
          ThrustCorrection=thrustCorrection; FlappingMomentWorld=flappingMoment; FlowClamped=flowClamped;
          PropellerTorque=propellerTorque; AccelerationTorque=accelerationTorque; SpinEnergy=spinEnergy;
          MotorCurrent=motorCurrent; MotorLoss=motorLoss; EscLoss=escLoss; EvaluationRpm=evaluationRpm; }
    }
    public sealed class FlightTelemetryFrame
    {
        // State is sampled BEFORE Rigidbody integration; forces are prepared for [TimeS, TimeS+Dt].
        public double TimeS,Dt,Density,AltitudeM,Mass,Gravity;
        public int Segment;
        public bool Armed,Saturated,PowerLimited,HasDriveFault;
        public string ControlMode="None";
        public bool AltitudeHold;
        public FlightInput Input;
        public DVector3 PositionWorld,VelocityWorld,AngularVelocityLocal,WindWorld,BodyDragWorld,BodyTorqueWorld,DesiredRateLocal;
        public double RotationX,RotationY,RotationZ,RotationW;
        public double? Soc,Voltage,Current,ElectricalPower,MechanicalPower,ConsumedAh,EnergyJ;
        public double? PropellerPower,SpinEnergyRate,SpinBalanceError,MotorLoss,EscLoss;
        public DVector3 GyroscopicMomentWorld;
        public RotorTelemetry[] Rotors;
    }
    public sealed class FlightCsvWriter
    {
        public const string FormatVersion="1.2.0";
        private readonly TextWriter writer;
        private readonly int count;
        private readonly StringBuilder row=new StringBuilder(2048);
        private static readonly char[] SpecialCsv={',','"','\r','\n'};
        public long Rows { get; private set; }
        public FlightCsvWriter(TextWriter writer,IReadOnlyList<string> rotorIds)
        {
            this.writer=writer ?? throw new ArgumentNullException(nameof(writer));
            if(rotorIds==null || rotorIds.Count==0) throw new ArgumentException("Rotor IDs required.");
            count=rotorIds.Count;
            string header="segment,time_s,dt_s,armed,control_mode,altitude_hold,saturated,power_limited,drive_fault,mass_kg,gravity_mps2,density_kgm3,altitude_m,position_x_m,position_y_m,position_z_m,rotation_x,rotation_y,rotation_z,rotation_w,velocity_x_mps,velocity_y_mps,velocity_z_mps,rate_x_radps,rate_y_radps,rate_z_radps,desired_rate_x_radps,desired_rate_y_radps,desired_rate_z_radps,input_roll,input_pitch,input_yaw,input_climb,input_throttle,wind_x_mps,wind_y_mps,wind_z_mps,body_drag_x_n,body_drag_y_n,body_drag_z_n,body_torque_x_nm,body_torque_y_nm,body_torque_z_nm,soc_end,voltage_v,current_a,bus_power_w,shaft_power_w,consumed_ah_end,bus_energy_j_end";
            row.Append(header);
            for(int i=0;i<count;i++)
                foreach(string suffix in new[]{"command","drive","rpm","thrust_n","reaction_nm","advance_j","measured_current_a","bus_current_a","ground_height_m","ground_gain","induced_hover_mps","wind_x_mps","wind_y_mps","wind_z_mps","drag_x_n","drag_y_n","drag_z_n","clamped","thrust_correction_n","flap_x_nm","flap_y_nm","flap_z_nm","flow_clamped","propeller_q_nm","acceleration_q_nm","spin_energy_j","motor_current_a","motor_loss_w","esc_loss_w","force_rpm"})
                    Cell("rotor_"+i+"_"+rotorIds[i]+"_"+suffix);
            foreach(var name in new[]{"propeller_power_w","spin_energy_rate_w","spin_balance_error_w","motor_loss_w","esc_loss_w","gyro_x_nm","gyro_y_nm","gyro_z_nm"}) Cell(name);
            writer.WriteLine(row.ToString());
        }
        private void Cell(string value)
        {
            row.Append(',');
            if(value.IndexOfAny(SpecialCsv)>=0) row.Append('"').Append(value.Replace("\"","\"\"")).Append('"');
            else row.Append(value);
        }
        private void Number(double x)
        {
            if(double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentException("Telemetry must be finite; unknown values use empty cells.");
            Cell(x.ToString("R",CultureInfo.InvariantCulture));
        }
        private void Optional(double? x) { if(x.HasValue) Number(x.Value); else Cell(""); }
        private void Flag(bool x)=>Cell(x ? "1" : "0");
        private void Vector(DVector3 v) { Number(v.X); Number(v.Y); Number(v.Z); }
        public void Write(FlightTelemetryFrame f)
        {
            if(f==null || f.Rotors==null || f.Rotors.Length!=count) throw new ArgumentException("Rotor count changed; start a new recording.");
            row.Clear(); row.Append(f.Segment.ToString(CultureInfo.InvariantCulture));
            Number(f.TimeS); Number(f.Dt); Flag(f.Armed); Cell(f.ControlMode); Flag(f.AltitudeHold);
            Flag(f.Saturated); Flag(f.PowerLimited); Flag(f.HasDriveFault); Number(f.Mass); Number(f.Gravity); Number(f.Density); Number(f.AltitudeM);
            Vector(f.PositionWorld); Number(f.RotationX); Number(f.RotationY); Number(f.RotationZ); Number(f.RotationW);
            Vector(f.VelocityWorld); Vector(f.AngularVelocityLocal); Vector(f.DesiredRateLocal);
            Number(f.Input.Roll); Number(f.Input.Pitch); Number(f.Input.Yaw); Number(f.Input.Climb); Number(f.Input.Throttle);
            Vector(f.WindWorld); Vector(f.BodyDragWorld); Vector(f.BodyTorqueWorld);
            Optional(f.Soc); Optional(f.Voltage); Optional(f.Current); Optional(f.ElectricalPower); Optional(f.MechanicalPower); Optional(f.ConsumedAh); Optional(f.EnergyJ);
            foreach(var r in f.Rotors)
            {
                Number(r.Command); Number(r.DriveAuthority); Number(r.Rpm); Number(r.Thrust); Number(r.ReactionTorque); Number(r.AdvanceRatio);
                Optional(r.MeasuredCurrent); Optional(r.BusCurrent); Optional(r.GroundHeight); Number(r.GroundGain); Number(r.InducedHoverSpeed);
                Vector(r.WindWorld); Vector(r.DragWorld); Flag(r.Clamped);
                Number(r.ThrustCorrection); Vector(r.FlappingMomentWorld); Flag(r.FlowClamped);
                Optional(r.PropellerTorque); Optional(r.AccelerationTorque); Optional(r.SpinEnergy); Optional(r.MotorCurrent); Optional(r.MotorLoss); Optional(r.EscLoss);
                Optional(r.EvaluationRpm);
            }
            Optional(f.PropellerPower); Optional(f.SpinEnergyRate); Optional(f.SpinBalanceError); Optional(f.MotorLoss); Optional(f.EscLoss); Vector(f.GyroscopicMomentWorld);
            writer.WriteLine(row.ToString()); Rows++;
        }
        public void Flush()=>writer.Flush();
    }
}
