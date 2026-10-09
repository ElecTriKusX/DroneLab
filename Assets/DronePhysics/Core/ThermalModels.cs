using System;

namespace DroneLab.Physics
{
    // Effective single temperature per component; no winding/housing network or chemistry.
    public sealed class RuntimeThermalParameters
    {
        public readonly double Capacity,Conductance,AirflowConductance,MaxAirSpeed,InitialTemperature,DerateStart,Cutoff;
        internal RuntimeThermalParameters(ThermalProfile p)
        { Capacity=p.heatCapacityJPerK; Conductance=p.heatTransferWPerK; AirflowConductance=p.airflowHeatTransferWPerKPerMps;
          MaxAirSpeed=p.maxAirSpeedMps; InitialTemperature=p.initialTemperatureK; DerateStart=p.derateStartTemperatureK; Cutoff=p.cutoffTemperatureK; }
        public double Authority(double temperature)
        { EnvironmentMath.Finite(temperature); return PhysicsMath.Clamp((Cutoff-temperature)/(Cutoff-DerateStart),0,1); }
    }
    public readonly struct ThermalStep
    {
        public readonly double Temperature,GeneratedEnergy,RejectedEnergy,StoredEnergyChange;
        public ThermalStep(double temperature,double generated,double rejected,double stored)
        { Temperature=temperature; GeneratedEnergy=generated; RejectedEnergy=rejected; StoredEnergyChange=stored; }
    }
    public static class ThermalMath
    {
        public static void Ambient(double temperature,double speed)
        {
            EnvironmentMath.Finite(temperature); EnvironmentMath.Finite(speed);
            if(temperature<100 || temperature>500 || speed<0 || speed>1e6)
                throw new ArgumentOutOfRangeException(nameof(temperature),"Thermal queries: ambient 100..500 K, nonnegative air speed <=1e6 m/s.");
        }
        // Exact solution of C dT/dt = P - G(T-Ta) for constant inputs over this step.
        public static ThermalStep Integrate(RuntimeThermalParameters p,double temperature,double lossW,double ambientK,double airSpeed,double dt)
        {
            if(p==null) throw new ArgumentNullException(nameof(p));
            EnvironmentMath.Finite(temperature); EnvironmentMath.Finite(lossW); EnvironmentMath.Finite(dt); Ambient(ambientK,airSpeed);
            if(temperature<=0 || lossW<0 || dt<=0) throw new ArgumentOutOfRangeException(nameof(lossW));
            double g=p.Conductance+p.AirflowConductance*Math.Min(airSpeed,p.MaxAirSpeed);
            double x=g*dt/p.Capacity;
            double f=x<1e-5 ? 1-x/2+x*x/6-x*x*x/24 : (1-Math.Exp(-x))/x;
            double change=(lossW-g*(temperature-ambientK))*dt/p.Capacity*f;
            double generated=lossW*dt,stored=p.Capacity*change,next=temperature+change;
            EnvironmentMath.Finite(next); EnvironmentMath.Finite(generated); EnvironmentMath.Finite(stored);
            if(next<=0) throw new ArgumentOutOfRangeException(nameof(temperature));
            return new ThermalStep(next,generated,generated-stored,stored);
        }
    }
    public sealed class ThermalNode
    {
        public readonly RuntimeThermalParameters Parameters;
        public double TemperatureK { get; private set; }
        public double GeneratedEnergyJ { get; private set; }
        public double RejectedEnergyJ { get; private set; }
        public double StoredEnergyJ=>Parameters.Capacity*(TemperatureK-Parameters.InitialTemperature);
        public double Authority=>Parameters.Authority(TemperatureK);
        private ThermalStep prepared;
        internal ThermalNode(RuntimeThermalParameters p) { Parameters=p; Reset(); }
        internal ThermalStep Prepare(double loss,double ambient,double speed,double dt)=>ThermalMath.Integrate(Parameters,TemperatureK,loss,ambient,speed,dt);
        internal void SetPrepared(ThermalStep step)=>prepared=step;
        internal void Commit()
        { TemperatureK=prepared.Temperature; GeneratedEnergyJ+=prepared.GeneratedEnergy; RejectedEnergyJ+=prepared.RejectedEnergy; }
        internal void Reset() { TemperatureK=Parameters.InitialTemperature; GeneratedEnergyJ=RejectedEnergyJ=0; prepared=default; }
    }
    public sealed class ThermalSystem
    {
        public readonly ThermalNode Battery;
        private readonly ThermalNode[] motors,escs;
        private readonly ThermalStep[] motorSteps,escSteps;
        private bool pending;
        public bool Derated
        {
            get { if(Battery.Authority<1) return true; for(int i=0;i<motors.Length;i++) if(motors[i].Authority<1 || escs[i].Authority<1) return true; return false; }
        }
        internal ThermalSystem(RuntimeDroneParameters p)
        {
            Battery=new ThermalNode(p.Battery.Thermal); motors=new ThermalNode[p.Rotors.Count]; escs=new ThermalNode[motors.Length];
            motorSteps=new ThermalStep[motors.Length]; escSteps=new ThermalStep[motors.Length];
            for(int i=0;i<motors.Length;i++) { motors[i]=new ThermalNode(p.Rotors[i].Power.Thermal); escs[i]=new ThermalNode(p.Rotors[i].Power.EscThermal); }
        }
        public ThermalNode Motor(int i)=>motors[i];
        public ThermalNode Esc(int i)=>escs[i];
        public double GeneratedEnergyJ { get { double x=Battery.GeneratedEnergyJ; for(int i=0;i<motors.Length;i++) x+=motors[i].GeneratedEnergyJ+escs[i].GeneratedEnergyJ; return x; } }
        public double RejectedEnergyJ { get { double x=Battery.RejectedEnergyJ; for(int i=0;i<motors.Length;i++) x+=motors[i].RejectedEnergyJ+escs[i].RejectedEnergyJ; return x; } }
        public double StoredEnergyJ { get { double x=Battery.StoredEnergyJ; for(int i=0;i<motors.Length;i++) x+=motors[i].StoredEnergyJ+escs[i].StoredEnergyJ; return x; } }
        // Prepare all nodes first. No state changes during governor trials or failed force queries.
        internal void Prepare(PowerSystem power,double ambient,double[] rotorAirSpeed,double bodyAirSpeed,double dt)
        {
            pending=false;
            var b=Battery.Prepare(power.BatteryLossPower,ambient,bodyAirSpeed,dt);
            for(int i=0;i<motors.Length;i++)
            {
                motorSteps[i]=motors[i].Prepare(power.RotorMotorLossW[i],ambient,rotorAirSpeed[i],dt);
                escSteps[i]=escs[i].Prepare(power.RotorEscLossW[i],ambient,rotorAirSpeed[i],dt);
            }
            Battery.SetPrepared(b);
            for(int i=0;i<motors.Length;i++) { motors[i].SetPrepared(motorSteps[i]); escs[i].SetPrepared(escSteps[i]); }
            pending=true;
        }
        internal void Cancel()=>pending=false;
        internal void Commit()
        { if(!pending) return; Battery.Commit(); for(int i=0;i<motors.Length;i++) { motors[i].Commit(); escs[i].Commit(); } pending=false; }
        internal void Reset() { Battery.Reset(); for(int i=0;i<motors.Length;i++) { motors[i].Reset(); escs[i].Reset(); } pending=false; }
    }
}
