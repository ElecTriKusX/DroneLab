using System;
using System.Linq;

namespace DroneLab.Physics
{
    public sealed class RuntimeMotorPower
    {
        public readonly double Kv, Resistance, NoLoadCurrent, MaxCurrent, MaxPower, Efficiency, EscEfficiency, EscMaxCurrent;
        internal RuntimeMotorPower(MotorElectricalProfile p)
        {
            Kv=p.motorKvRpmPerVolt; Resistance=p.motorResistanceOhm; NoLoadCurrent=p.noLoadCurrentA;
            MaxCurrent=p.maxCurrentA; MaxPower=p.maxPowerW>0 ? p.maxPowerW : double.PositiveInfinity;
            Efficiency=p.motorEfficiency; EscEfficiency=p.escEfficiency; EscMaxCurrent=p.escMaxCurrentA;
        }
    }
    public sealed class RuntimeBattery
    {
        public readonly string Mode;
        public readonly double NominalVoltage, CapacityC, InitialSoc, Resistance, MaxCurrent;
        public readonly bool Discharge;
        private readonly double[] soc,voltage;
        internal RuntimeBattery(BatteryProfile p,PhysicsModulesProfile modules)
        {
            Mode=p.mode; NominalVoltage=p.nominalVoltageV; CapacityC=p.capacityAh*3600; InitialSoc=p.initialSoc;
            Resistance=modules.batteryVoltageSag ? p.internalResistanceOhm : 0;
            Discharge=modules.batteryDischarge; MaxCurrent=p.maxDischargeCurrentA;
            soc=p.ocvCurve.Select(x=>x.soc).ToArray(); voltage=p.ocvCurve.Select(x=>x.voltageV).ToArray();
        }
        public double OpenCircuitVoltage(double charge)
        {
            charge=PhysicsMath.Clamp(charge,0,1);
            for(int i=1;i<soc.Length;i++) if(charge<=soc[i])
                return voltage[i-1]+(voltage[i]-voltage[i-1])*(charge-soc[i-1])/(soc[i]-soc[i-1]);
            return voltage[voltage.Length-1];
        }
    }
    // Quasi-steady power governor. Mechanical motor lag is separate; no inductance or regeneration.
    public sealed class PowerSystem
    {
        private readonly RuntimeDroneParameters parameters;
        private readonly RuntimeBattery battery;
        private readonly double[] motorPower,motorCurrent,requiredVoltage;
        private double queryDensity;
        public double Soc { get; private set; }
        public double OpenVoltage { get; private set; }
        public double TerminalVoltage { get; private set; }
        public double Current { get; private set; }
        public double ElectricalPower { get; private set; }
        public double MechanicalPower { get; private set; }
        public double BatteryLossPower { get; private set; }
        public double ConsumedAh { get; private set; }
        public double TerminalEnergyJ { get; private set; }
        public double ChemicalEnergyJ { get; private set; }
        public double RpmScale { get; private set; }
        public bool Limited => RpmScale<1-1e-7;
        // Battery-side current, distinct from optional current measured in propeller CSV tables.
        public double[] RotorCurrentA { get; }
        public PowerSystem(RuntimeDroneParameters p)
        {
            parameters=p; battery=p.Battery ?? throw new ArgumentException("Battery is disabled.");
            int count=p.Rotors.Count; RotorCurrentA=new double[count]; motorPower=new double[count];
            motorCurrent=new double[count]; requiredVoltage=new double[count]; Reset();
        }
        public void Reset()
        {
            Soc=battery.InitialSoc; OpenVoltage=battery.OpenCircuitVoltage(Soc); TerminalVoltage=OpenVoltage;
            Current=ElectricalPower=MechanicalPower=BatteryLossPower=ConsumedAh=TerminalEnergyJ=ChemicalEnergyJ=0;
            RpmScale=1; Array.Clear(RotorCurrentA,0,RotorCurrentA.Length);
        }
        public static double CurrentForPower(double power,double openVoltage,double resistance)
        {
            if(power<0 || !Finite(power) || openVoltage<=0 || !Finite(openVoltage) || resistance<0 || !Finite(resistance))
                throw new ArgumentOutOfRangeException(nameof(power));
            if(power==0) return 0;
            double discriminant=openVoltage*openVoltage-4*resistance*power;
            if(discriminant<0) return double.PositiveInfinity;
            // Stable low-current root; tends to P / Voc as R approaches zero.
            return 2*power/(openVoltage+Math.Sqrt(discriminant));
        }
        private static bool Finite(double x)=>!double.IsNaN(x) && !double.IsInfinity(x);
        private bool Evaluate(double scale,double[] requested,double currentLimit,out double current,out double terminal,out double mechanical)
        {
            double total=0; mechanical=0;
            for(int i=0;i<requested.Length;i++)
            {
                double omega=requested[i]*scale; var r=parameters.Rotors[i]; var m=r.Power;
                double q=r.Performance.Evaluate(omega,density:queryDensity).Torque;
                double shaft=q*omega; mechanical+=shaft;
                if(omega==0) { motorPower[i]=motorCurrent[i]=requiredVoltage[i]=0; continue; }
                if(battery.Mode=="Electrical")
                {
                    double kt=60/(2*Math.PI*m.Kv);
                    motorCurrent[i]=m.NoLoadCurrent+q/kt;
                    requiredVoltage[i]=omega*kt+motorCurrent[i]*m.Resistance;
                    motorPower[i]=requiredVoltage[i]*motorCurrent[i];
                }
                else
                {
                    // Fixed nominal-voltage no-load loss; this is an estimated power model.
                    motorPower[i]=shaft/m.Efficiency+battery.NominalVoltage*m.NoLoadCurrent;
                    requiredVoltage[i]=0; motorCurrent[i]=0;
                }
                total+=motorPower[i]/m.EscEfficiency;
            }
            current=CurrentForPower(total,OpenVoltage,battery.Resistance);
            terminal=OpenVoltage-current*battery.Resistance;
            if(!Finite(current) || !Finite(terminal) || current>currentLimit || terminal<OpenVoltage/2) return false;
            for(int i=0;i<requested.Length;i++)
            {
                var r=parameters.Rotors[i]; var m=r.Power;
                if(motorPower[i]>m.MaxPower) return false;
                if(battery.Mode=="Electrical")
                {
                    if(requiredVoltage[i]>terminal || motorCurrent[i]>Math.Min(m.MaxCurrent,m.EscMaxCurrent)) return false;
                }
                else if(requested[i]*scale>r.MaxOmega*Math.Min(1,terminal/battery.NominalVoltage) ||
                    motorPower[i]/(m.EscEfficiency*terminal)>Math.Min(m.MaxCurrent,m.EscMaxCurrent)) return false;
            }
            return true;
        }
        // requested/output may be the same array. Commit SOC only after the caller's force queries succeed.
        public void Resolve(double[] requested,double[] output,double dt,bool powered,double? density=null)
        {
            queryDensity=density ?? parameters.Density;
            if(!Finite(queryDensity) || queryDensity<=0) throw new ArgumentOutOfRangeException(nameof(density));
            if(!Finite(dt) || dt<=0) throw new ArgumentOutOfRangeException(nameof(dt));
            if(requested==null || output==null || requested.Length!=parameters.Rotors.Count || output.Length!=requested.Length)
                throw new ArgumentException("One speed per rotor is required.");
            for(int i=0;i<requested.Length;i++)
                if(!Finite(requested[i]) || requested[i]<0 || requested[i]>parameters.Rotors[i].MaxOmega+1e-9)
                    throw new ArgumentOutOfRangeException(nameof(requested));
            OpenVoltage=battery.OpenCircuitVoltage(Soc); Current=ElectricalPower=MechanicalPower=BatteryLossPower=0;
            TerminalVoltage=OpenVoltage; RpmScale=1; Array.Clear(RotorCurrentA,0,RotorCurrentA.Length);
            if(!powered) { Array.Copy(requested,output,requested.Length); return; }
            double limit=battery.MaxCurrent;
            if(battery.Discharge) limit=Math.Min(limit,Soc*battery.CapacityC/dt);
            double scale=1;
            if(Soc<=0) scale=0;
            else if(!Evaluate(1,requested,limit,out _,out _,out _))
            {
                double lo=0,hi=1;
                for(int k=0;k<48;k++)
                {
                    double mid=(lo+hi)/2;
                    if(Evaluate(mid,requested,limit,out _,out _,out _)) lo=mid; else hi=mid;
                }
                scale=lo;
            }
            Evaluate(scale,requested,limit,out double current,out double terminal,out double mechanical);
            Current=current; TerminalVoltage=terminal; MechanicalPower=mechanical; ElectricalPower=current*terminal;
            BatteryLossPower=current*current*battery.Resistance; RpmScale=scale;
            for(int i=0;i<output.Length;i++)
            { RotorCurrentA[i]=motorPower[i]/(parameters.Rotors[i].Power.EscEfficiency*terminal); output[i]=requested[i]*scale; }
        }
        public void Commit(double dt)
        {
            if(!Finite(dt) || dt<=0) throw new ArgumentOutOfRangeException(nameof(dt));
            double charge=Current*dt;
            if(battery.Discharge) Soc=Math.Max(0,Soc-charge/battery.CapacityC);
            ConsumedAh+=charge/3600; TerminalEnergyJ+=ElectricalPower*dt; ChemicalEnergyJ+=OpenVoltage*charge;
        }
    }
}
