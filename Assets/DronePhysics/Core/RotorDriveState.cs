using System;

namespace DroneLab.Physics
{
    // Test disturbance: loss of commanded drive, not a broken propeller or a jammed shaft.
    public sealed class RotorDriveState
    {
        private readonly double[] authority;
        public int Count=>authority.Length;
        public bool HasFault { get { foreach(double x in authority) if(x<1) return true; return false; } }
        public RotorDriveState(int count)
        { if(count<=0) throw new ArgumentOutOfRangeException(nameof(count)); authority=new double[count]; Reset(); }
        public double Get(int index)=>authority[index];
        public void Set(int index,double fraction)
        {
            if(double.IsNaN(fraction) || double.IsInfinity(fraction) || fraction<0 || fraction>1)
                throw new ArgumentOutOfRangeException(nameof(fraction));
            authority[index]=fraction;
        }
        public double Target(int index,double requestedOmega)=>requestedOmega*authority[index];
        public void Reset() { for(int i=0;i<Count;i++) authority[i]=1; }
    }
}
