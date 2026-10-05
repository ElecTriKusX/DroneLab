using System;
using System.Linq;

namespace DroneLab.Physics
{
    public sealed class RuntimeMotorPower
    {
        public readonly double Kv, Resistance, NoLoadCurrent, MaxCurrent, MaxPower, Efficiency, EscEfficiency, EscMaxCurrent;
        public readonly double ResistanceReferenceTemperature,ResistanceTemperatureCoefficient;
        public readonly RuntimeThermalParameters Thermal,EscThermal;
        internal RuntimeMotorPower(MotorElectricalProfile p)
        {
            Kv=p.motorKvRpmPerVolt; Resistance=p.motorResistanceOhm; NoLoadCurrent=p.noLoadCurrentA;
            MaxCurrent=p.maxCurrentA; MaxPower=p.maxPowerW>0 ? p.maxPowerW : double.PositiveInfinity;
            Efficiency=p.motorEfficiency; EscEfficiency=p.escEfficiency; EscMaxCurrent=p.escMaxCurrentA;
            ResistanceReferenceTemperature=p.resistanceReferenceTemperatureK; ResistanceTemperatureCoefficient=p.resistanceTemperatureCoefficientPerK;
            Thermal=p.thermal==null ? null : new RuntimeThermalParameters(p.thermal);
            EscThermal=p.escThermal==null ? null : new RuntimeThermalParameters(p.escThermal);
        }
    }
    public sealed class RuntimeBattery
    {
        public readonly string Mode;
        public readonly double NominalVoltage, CapacityC, InitialSoc, Resistance, MaxCurrent;
        public readonly bool Discharge;
        public readonly RuntimeThermalParameters Thermal;
        private readonly double[] soc,voltage;
        internal RuntimeBattery(BatteryProfile p,PhysicsModulesProfile modules)
        {
            Mode=p.mode; NominalVoltage=p.nominalVoltageV; CapacityC=p.capacityAh*3600; InitialSoc=p.initialSoc;
            Resistance=modules.batteryVoltageSag ? p.internalResistanceOhm : 0;
            Discharge=modules.batteryDischarge; MaxCurrent=p.maxDischargeCurrentA;
            Thermal=p.thermal==null ? null : new RuntimeThermalParameters(p.thermal);
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
    // DC/BLDC equivalent supply governor; optional implicit spin dynamics. No inductance/regen.
    public sealed class PowerSystem
    {
        private readonly RuntimeDroneParameters parameters;
        private readonly RuntimeBattery battery;
        private readonly double[] motorPower,motorCurrent,requiredVoltage;
        private double queryDensity;
        private RotorDriveState queryDrive;
        private double[] queryAxial;
        private readonly double[] previous,coast,desired;
        private readonly bool[] driven;
        private readonly double[][] stepArrays;
        private double queryDt;
        private bool thermalStepPending;
        public ThermalSystem Thermal { get; }
        public bool ThermalDerated=>Thermal?.Derated ?? false;
        public double MotorResistanceOhm(int i)
        {
            var m=parameters.Rotors[i].Power;
            return Thermal==null ? m.Resistance : m.Resistance*(1+m.ResistanceTemperatureCoefficient*(Thermal.Motor(i).TemperatureK-m.ResistanceReferenceTemperature));
        }
        public bool IsThermalDriveAvailable(int i)
        {
            if(Thermal==null) return true;
            var m=parameters.Rotors[i].Power;
            // A local cap below no-load current cannot drive this motor. Let it coast rather
            // than making all other motors fail the common-scale feasibility test.
            return Thermal.Battery.Authority>0 &&
                Math.Min(m.MaxCurrent*Thermal.Motor(i).Authority,m.EscMaxCurrent*Thermal.Esc(i).Authority)>m.NoLoadCurrent;
        }
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
        public double PropellerPower { get; private set; }
        public double SpinEnergyChangePower { get; private set; }
        public double SpinBalanceErrorPower { get; private set; }
        public double MotorLossPower { get; private set; }
        public double EscLossPower { get; private set; }
        public double[] RotorTorqueNm { get; }
        public double[] RotorPropellerTorqueNm { get; }
        public double[] RotorAccelerationTorqueNm { get; }
        public double[] RotorSpinEnergyJ { get; }
        public double[] RotorMotorLossW { get; }
        public double[] RotorEscLossW { get; }
        public double[] MotorCurrentA=>motorCurrent;
        public bool Limited => RpmScale<1-1e-7;
        // Battery-side current, distinct from optional current measured in propeller CSV tables.
        public double[] RotorCurrentA { get; }
        public PowerSystem(RuntimeDroneParameters p)
        {
            parameters=p; battery=p.Battery ?? throw new ArgumentException("Battery is disabled.");
            int count=p.Rotors.Count; RotorCurrentA=new double[count]; motorPower=new double[count];
            motorCurrent=new double[count]; requiredVoltage=new double[count];
            previous=new double[count]; coast=new double[count]; desired=new double[count]; driven=new bool[count];
            RotorTorqueNm=new double[count]; RotorAccelerationTorqueNm=new double[count]; RotorSpinEnergyJ=new double[count];
            RotorPropellerTorqueNm=new double[count];
            RotorMotorLossW=new double[count]; RotorEscLossW=new double[count];
            stepArrays=new[]{RotorTorqueNm,RotorPropellerTorqueNm,RotorAccelerationTorqueNm,RotorSpinEnergyJ,RotorMotorLossW,RotorEscLossW,motorCurrent};
            Thermal=p.ThermalEnabled ? new ThermalSystem(p) : null; Reset();
        }
        public void Reset()
        {
            Soc=battery.InitialSoc; OpenVoltage=battery.OpenCircuitVoltage(Soc); TerminalVoltage=OpenVoltage;
            Current=ElectricalPower=MechanicalPower=BatteryLossPower=ConsumedAh=TerminalEnergyJ=ChemicalEnergyJ=0;
            RpmScale=1; Array.Clear(RotorCurrentA,0,RotorCurrentA.Length);
            ClearStep();
            Thermal?.Reset(); thermalStepPending=false;
        }
        private void ClearStep()
        {
            PropellerPower=SpinEnergyChangePower=SpinBalanceErrorPower=MotorLossPower=EscLossPower=0;
            foreach(var a in stepArrays) Array.Clear(a,0,a.Length);
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
            double total=0; mechanical=0; ClearStep();
            for(int i=0;i<requested.Length;i++)
            {
                double omega=parameters.InertialRotors ? coast[i]+(desired[i]-coast[i])*scale : requested[i]*scale;
                var r=parameters.Rotors[i]; var m=r.Power;
                double evaluationOmega=parameters.InertialRotors ? (omega+previous[i])/2 : omega;
                double q=r.Performance.Evaluate(evaluationOmega,queryAxial==null ? 0:queryAxial[i],queryDensity).Torque;
                // Positivity guard: a stop occurring inside the step uses its effective average drag torque.
                if(parameters.InertialRotors && omega==0 && coast[i]==0) q=r.RotatingInertia*previous[i]/queryDt;
                RotorPropellerTorqueNm[i]=q;
                double shaft=q*evaluationOmega; double motorTorque=q;
                if(parameters.InertialRotors)
                {
                    double change=omega-previous[i];
                    RotorAccelerationTorqueNm[i]=r.RotatingInertia*change/queryDt;
                    motorTorque=driven[i] && scale>0 ? Math.Max(0,q+RotorAccelerationTorqueNm[i]) : 0;
                    RotorSpinEnergyJ[i]=RotorDynamics.SpinEnergy(r,omega);
                    SpinEnergyChangePower+=(RotorSpinEnergyJ[i]-RotorDynamics.SpinEnergy(r,previous[i]))/queryDt;
                    PropellerPower+=shaft;
                }
                RotorTorqueNm[i]=motorTorque;
                if((parameters.InertialRotors && (!driven[i] || scale==0)) || (!parameters.InertialRotors && ((queryDrive!=null && queryDrive.Get(i)==0) || !IsThermalDriveAvailable(i))))
                { motorPower[i]=motorCurrent[i]=requiredVoltage[i]=0; continue; }
                if(!parameters.InertialRotors) PropellerPower+=shaft;
                shaft=motorTorque*evaluationOmega; mechanical+=shaft;
                if(omega==0) { motorPower[i]=motorCurrent[i]=requiredVoltage[i]=0; continue; }
                if(battery.Mode=="Electrical")
                {
                    double kt=60/(2*Math.PI*m.Kv);
                    double resistance=MotorResistanceOhm(i);
                    motorCurrent[i]=m.NoLoadCurrent+motorTorque/kt;
                    requiredVoltage[i]=(parameters.InertialRotors ? Math.Max(omega,previous[i]) : omega)*kt+motorCurrent[i]*resistance;
                    motorPower[i]=(evaluationOmega*kt+motorCurrent[i]*resistance)*motorCurrent[i];
                }
                else
                {
                    // Fixed nominal-voltage no-load loss; this is an estimated power model.
                    motorPower[i]=shaft/m.Efficiency+battery.NominalVoltage*m.NoLoadCurrent;
                    requiredVoltage[i]=0; motorCurrent[i]=0;
                }
                RotorMotorLossW[i]=Math.Max(0,motorPower[i]-shaft);
                RotorEscLossW[i]=motorPower[i]*(1/m.EscEfficiency-1);
                MotorLossPower+=RotorMotorLossW[i]; EscLossPower+=RotorEscLossW[i];
                total+=motorPower[i]/m.EscEfficiency;
            }
            SpinBalanceErrorPower=parameters.InertialRotors ? mechanical-PropellerPower-SpinEnergyChangePower : 0;
            current=CurrentForPower(total,OpenVoltage,battery.Resistance);
            terminal=OpenVoltage-current*battery.Resistance;
            if(!Finite(current) || !Finite(terminal) || current>currentLimit || terminal<OpenVoltage/2) return false;
            for(int i=0;i<requested.Length;i++)
            {
                if((parameters.InertialRotors && (!driven[i] || scale==0)) || (!parameters.InertialRotors && ((queryDrive!=null && queryDrive.Get(i)==0) || !IsThermalDriveAvailable(i)))) continue;
                var r=parameters.Rotors[i]; var m=r.Power;
                double motorAuthority=Thermal?.Motor(i).Authority ?? 1,escAuthority=Thermal?.Esc(i).Authority ?? 1;
                if(motorPower[i]>m.MaxPower*motorAuthority) return false;
                if(battery.Mode=="Electrical")
                {
                    if(requiredVoltage[i]>terminal || motorCurrent[i]>Math.Min(m.MaxCurrent*motorAuthority,m.EscMaxCurrent*escAuthority)) return false;
                }
                else if(requested[i]*scale>r.MaxOmega*Math.Min(1,terminal/battery.NominalVoltage) ||
                    motorPower[i]/(m.EscEfficiency*terminal)>Math.Min(m.MaxCurrent,m.EscMaxCurrent)) return false;
            }
            return true;
        }
        // requested/output may be the same array. Commit SOC only after the caller's force queries succeed.
        public void Resolve(double[] requested,double[] output,double dt,bool powered,double? density=null,RotorDriveState drive=null,
            double[] axialVelocity=null,double[] previousOmega=null,double? ambientTemperatureK=null,double[] rotorAirSpeed=null,double bodyAirSpeed=0)
        {
            thermalStepPending=false; Thermal?.Cancel();
            if(drive!=null && drive.Count!=parameters.Rotors.Count) throw new ArgumentException("Drive state must match rotor count.");
            queryDrive=drive;
            queryDensity=density ?? parameters.Density;
            if(!Finite(queryDensity) || queryDensity<=0) throw new ArgumentOutOfRangeException(nameof(density));
            if(!Finite(dt) || dt<=0) throw new ArgumentOutOfRangeException(nameof(dt));
            if(requested==null || output==null || requested.Length!=parameters.Rotors.Count || output.Length!=requested.Length)
                throw new ArgumentException("One speed per rotor is required.");
            if(axialVelocity!=null && (axialVelocity.Length!=requested.Length || axialVelocity.Any(x=>!Finite(x))))
                throw new ArgumentException("Finite axial velocity required for each rotor.");
            queryAxial=axialVelocity; queryDt=dt;
            if(Thermal!=null)
            {
                if(!ambientTemperatureK.HasValue || rotorAirSpeed==null || rotorAirSpeed.Length!=requested.Length)
                    throw new ArgumentException("Thermal mode needs ambient temperature and one actual air speed per rotor.");
                ThermalMath.Ambient(ambientTemperatureK.Value,bodyAirSpeed);
                foreach(double speed in rotorAirSpeed) ThermalMath.Ambient(ambientTemperatureK.Value,speed);
            }
            if(parameters.InertialRotors && (previousOmega==null || previousOmega.Length!=requested.Length))
                throw new ArgumentException("RotorInertia needs previous speeds for every rotor.");
            for(int i=0;i<requested.Length;i++)
                if(!Finite(requested[i]) || requested[i]<0 || requested[i]>parameters.Rotors[i].MaxOmega+1e-9)
                    throw new ArgumentOutOfRangeException(nameof(requested));
            if(parameters.InertialRotors) for(int i=0;i<requested.Length;i++)
            {
                var r=parameters.Rotors[i];
                if(!Finite(previousOmega[i]) || previousOmega[i]<0 || previousOmega[i]>r.MaxOmega+1e-9) throw new ArgumentOutOfRangeException(nameof(previousOmega));
                previous[i]=previousOmega[i]; coast[i]=RotorDynamics.Coast(r,previous[i],dt,axialVelocity==null ? 0:axialVelocity[i],queryDensity);
                driven[i]=powered && (drive==null || drive.Get(i)>0) && Soc>0 && IsThermalDriveAvailable(i);
                desired[i]=driven[i] ? Math.Max(coast[i],requested[i]) : coast[i];
                driven[i]&=desired[i]>coast[i];
            }
            OpenVoltage=battery.OpenCircuitVoltage(Soc); Current=ElectricalPower=MechanicalPower=BatteryLossPower=0;
            TerminalVoltage=OpenVoltage; RpmScale=1; Array.Clear(RotorCurrentA,0,RotorCurrentA.Length);
            ClearStep();
            if(!powered && !parameters.InertialRotors)
            {
                Array.Copy(requested,output,requested.Length);
                if(Thermal!=null) { Thermal.Prepare(this,ambientTemperatureK.Value,rotorAirSpeed,bodyAirSpeed,dt); thermalStepPending=true; }
                return;
            }
            double limit=battery.MaxCurrent*(Thermal?.Battery.Authority ?? 1);
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
            { RotorCurrentA[i]=motorPower[i]/(parameters.Rotors[i].Power.EscEfficiency*terminal);
              output[i]=parameters.InertialRotors ? coast[i]+(desired[i]-coast[i])*scale : (queryDrive!=null && queryDrive.Get(i)==0) || !IsThermalDriveAvailable(i) ? requested[i] : requested[i]*scale; }
            if(Thermal!=null) { Thermal.Prepare(this,ambientTemperatureK.Value,rotorAirSpeed,bodyAirSpeed,dt); thermalStepPending=true; }
        }
        public void Commit(double dt)
        {
            if(!Finite(dt) || dt<=0) throw new ArgumentOutOfRangeException(nameof(dt));
            if(Thermal!=null && (!thermalStepPending || System.Math.Abs(dt-queryDt)>1e-12*System.Math.Max(1,dt)))
                throw new InvalidOperationException("Thermal commit needs one successful Resolve with the same timestep.");
            double charge=Current*dt;
            if(battery.Discharge) Soc=Math.Max(0,Soc-charge/battery.CapacityC);
            ConsumedAh+=charge/3600; TerminalEnergyJ+=ElectricalPower*dt; ChemicalEnergyJ+=OpenVoltage*charge;
            Thermal?.Commit(); thermalStepPending=false;
        }
    }
}
