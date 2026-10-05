using System;

namespace DroneLab.Physics
{
    // Four parallel +Y rotors only. Physics itself accepts arbitrary rotors.
    // A * thrusts = [collective_N, torqueX_Nm, torqueY_Nm, torqueZ_Nm].
    public sealed class QuadAllocator
    {
        private readonly double[,] inverse=new double[4,4];
        private readonly RuntimeDroneParameters p;
        public double TorqueScale { get; private set; } = 1;
        public double AchievedCollective { get; private set; }
        private readonly bool nonlinear;
        private readonly double[] solution=new double[4];
        private double collectiveCapacity=double.PositiveInfinity;
        public QuadAllocator(RuntimeDroneParameters parameters)
        {
            p=parameters;
            if(p.Rotors.Count != 4) throw new ArgumentException("Test pilot requires four rotors.");
            var a=new double[4,8];
            foreach(var rotor in p.Rotors)
            {
                nonlinear|=!rotor.Performance.IsStaticQuadratic;
                rotor.Performance.RequirePilotCurve(rotor.MaxOmega);
                if(!rotor.Performance.IsQuadratic && rotor.MinOmega>0) throw new ArgumentException("Table/map test pilot requires minRpm=0.");
            }
            for(int i=0;i<4;i++)
            {
                var r=p.Rotors[i];
                if ((r.Axis-new DVector3(0,1,0)).Length>1e-5) throw new ArgumentException("Test pilot requires +Y rotor axes.");
                if(nonlinear && r.MinOmega>0) throw new ArgumentException("Table/map test pilot requires minRpm=0 for every rotor.");
                var torque=DVector3.Cross(r.Position-p.CenterOfMass,r.Axis)+r.Axis*(r.ReactionSign*r.Performance.Evaluate(r.MaxOmega).Torque/r.Performance.Evaluate(r.MaxOmega).Thrust);
                a[0,i]=1; a[1,i]=torque.X; a[2,i]=torque.Y; a[3,i]=torque.Z; a[i,i+4]=1;
            }
            for(int col=0;col<4;col++)
            {
                int pivot=col;
                for(int row=col+1;row<4;row++) if(Math.Abs(a[row,col])>Math.Abs(a[pivot,col])) pivot=row;
                if(Math.Abs(a[pivot,col])<1e-9) throw new ArgumentException("Rotor layout cannot control all four axes.");
                for(int k=0;k<8;k++) { double temp=a[col,k]; a[col,k]=a[pivot,k]; a[pivot,k]=temp; }
                double d=a[col,col]; for(int k=0;k<8;k++) a[col,k]/=d;
                for(int row=0;row<4;row++) if(row!=col)
                { double f=a[row,col]; for(int k=0;k<8;k++) a[row,k]-=f*a[col,k]; }
            }
            for(int i=0;i<4;i++) for(int j=0;j<4;j++) inverse[i,j]=a[i,j+4];
            for(int i=0;i<4;i++)
            {
                if(inverse[i,0]<=0) throw new ArgumentException("Rotor layout cannot produce positive collective with zero torque.");
                collectiveCapacity=Math.Min(collectiveCapacity,p.Rotors[i].MaxThrust/inverse[i,0]);
                if(nonlinear && p.Rotors[i].ReactionSign*inverse[i,2]<=0)
                    throw new ArgumentException("Table/map test pilot needs a monotone yaw null-space for this rotor layout.");
            }
        }
        // Preserve collective first; scale all requested torque axes together to fit motor bounds.
        // No negative-thrust clipping that accidentally raises collective during a yaw step.
        public bool Allocate(double collective,DVector3 torque,double[] commands)
        {
            if (commands == null || commands.Length != 4) throw new ArgumentException("Four output commands required.");
            if(double.IsNaN(collective) || double.IsInfinity(collective) ||
                double.IsNaN(torque.Length) || double.IsInfinity(torque.Length)) throw new ArgumentException("Finite wrench required.");
            if(nonlinear) return AllocateStaticCurves(collective,torque,commands);
            AchievedCollective=PhysicsMath.Clamp(collective,0,collectiveCapacity);
            TorqueScale=1;
            for(int i=0;i<4;i++)
            {
                double baseline=inverse[i,0]*AchievedCollective;
                double delta=inverse[i,1]*torque.X+inverse[i,2]*torque.Y+inverse[i,3]*torque.Z;
                if(delta>0) TorqueScale=Math.Min(TorqueScale,(p.Rotors[i].MaxThrust-baseline)/delta);
                else if(delta<0) TorqueScale=Math.Min(TorqueScale,-baseline/delta);
                commands[i]=delta;
            }
            TorqueScale=PhysicsMath.Clamp(TorqueScale,0,1);
            for(int i=0;i<4;i++) commands[i]=Math.Sqrt(PhysicsMath.Clamp(
                (inverse[i,0]*AchievedCollective+TorqueScale*commands[i])/p.Rotors[i].MaxThrust,0,1));
            return TorqueScale<1-1e-9 || Math.Abs(AchievedCollective-collective)>1e-9;
        }
        // Geometry fixes collective/roll/pitch; one remaining scalar controls yaw.
        // Invert actual static thrust and solve sum(sign * Q(T)) instead of assuming Q/T constant.
        private double Yaw(double z)
        {
            double total=0;
            for(int i=0;i<4;i++)
            {
                var rotor=p.Rotors[i]; double t=PhysicsMath.Clamp(solution[i]+inverse[i,2]*z,0,rotor.MaxThrust);
                double omega=rotor.Performance.OmegaForStaticThrust(t,rotor.MaxOmega);
                total+=rotor.ReactionSign*rotor.Performance.Evaluate(omega).Torque;
            }
            return total;
        }
        private bool TryStatic(double collective,DVector3 torque,double[] commands)
        {
            double lo=double.NegativeInfinity,hi=double.PositiveInfinity;
            for(int i=0;i<4;i++)
            {
                solution[i]=inverse[i,0]*collective+inverse[i,1]*torque.X+inverse[i,3]*torque.Z;
                double a=-solution[i]/inverse[i,2],b=(p.Rotors[i].MaxThrust-solution[i])/inverse[i,2];
                lo=Math.Max(lo,Math.Min(a,b)); hi=Math.Min(hi,Math.Max(a,b));
            }
            if(lo>hi+1e-12) return false;
            if(Yaw(lo)>torque.Y+1e-10 || Yaw(hi)<torque.Y-1e-10) return false;
            for(int k=0;k<36;k++) { double mid=(lo+hi)/2; if(Yaw(mid)<torque.Y) lo=mid; else hi=mid; }
            double z=(lo+hi)/2;
            for(int i=0;i<4;i++)
            {
                var rotor=p.Rotors[i];
                double t=PhysicsMath.Clamp(solution[i]+inverse[i,2]*z,0,rotor.MaxThrust);
                commands[i]=PhysicsMath.Clamp(rotor.Performance.OmegaForStaticThrust(t,rotor.MaxOmega)/rotor.MaxOmega,0,1);
            }
            return true;
        }
        private bool AllocateStaticCurves(double collective,DVector3 torque,double[] commands)
        {
            AchievedCollective=PhysicsMath.Clamp(collective,0,collectiveCapacity);
            if(AchievedCollective==0) { Array.Clear(commands,0,4); TorqueScale=torque.Length>0 ? 0 : 1; return collective!=0 || torque.Length>0; }
            if(!TryStatic(AchievedCollective,default,commands))
            {
                double lo=0,hi=AchievedCollective;
                for(int k=0;k<30;k++) { double mid=(lo+hi)/2; if(TryStatic(mid,default,commands)) lo=mid; else hi=mid; }
                AchievedCollective=lo;
            }
            TorqueScale=1;
            if(!TryStatic(AchievedCollective,torque,commands))
            {
                double lo=0,hi=1;
                for(int k=0;k<28;k++) { double mid=(lo+hi)/2; if(TryStatic(AchievedCollective,torque*mid,commands)) lo=mid; else hi=mid; }
                TorqueScale=lo; TryStatic(AchievedCollective,torque*lo,commands);
            }
            return TorqueScale<1-1e-9 || Math.Abs(AchievedCollective-collective)>1e-9;
        }
    }
}
