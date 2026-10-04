using System;

namespace DroneLab.Physics
{
    // Test controller allocator: four parallel +Y rotors only. Physics itself accepts arbitrary rotors.
    // A * thrusts = [collective_N, torqueX_Nm, torqueY_Nm, torqueZ_Nm].
    public sealed class QuadAllocator
    {
        private readonly double[,] inverse=new double[4,4];
        private readonly RuntimeDroneParameters p;
        public QuadAllocator(RuntimeDroneParameters parameters)
        {
            p=parameters;
            if(p.Rotors.Count != 4) throw new ArgumentException("Test pilot requires four rotors.");
            var a=new double[4,8];
            for(int i=0;i<4;i++)
            {
                var r=p.Rotors[i];
                if ((r.Axis-new DVector3(0,1,0)).Length>1e-5) throw new ArgumentException("Test pilot requires +Y rotor axes.");
                var torque=DVector3.Cross(r.Position-p.CenterOfMass,r.Axis)+r.Axis*(r.ReactionSign*r.KQ/r.KT);
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
        }
        // Scaling the entire requested wrench preserves its proportions at upper saturation.
        // Negative thrust clipping loses authority; surfaced to telemetry, never hidden.
        public bool Allocate(double collective,DVector3 torque,double[] commands)
        {
            if (commands == null || commands.Length != 4) throw new ArgumentException("Four output commands required.");
            bool saturated=false; double scale=1;
            for(int i=0;i<4;i++)
            {
                double thrust=inverse[i,0]*collective+inverse[i,1]*torque.X+inverse[i,2]*torque.Y+inverse[i,3]*torque.Z;
                if(thrust<0) saturated=true;
                commands[i]=Math.Max(0,thrust)/p.Rotors[i].MaxThrust;
                scale=Math.Max(scale,commands[i]);
            }
            for(int i=0;i<4;i++) commands[i]=Math.Sqrt(commands[i]/scale);
            return saturated || scale>1;
        }
    }
}
