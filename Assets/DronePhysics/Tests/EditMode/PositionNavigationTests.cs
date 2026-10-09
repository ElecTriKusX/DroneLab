using System;
using DroneLab.Physics;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class PositionNavigationTests
    {
        [TestCase(.01,0)] [TestCase(.02,0)] [TestCase(.04,0)]
        [TestCase(.01,.5)] [TestCase(.02,.5)] [TestCase(.04,.5)]
        public void PositionLoopSettlesAtRequestedXZWithDragAndConstantDisturbance(double dt,double disturbance)
        {
            var controller=new PositionController(); var target=new DVector3(4,10,-3);
            var position=new DVector3(-8,10,9); DVector3 velocity=default;
            for(double t=0;t<60;t+=dt) {
                var a=controller.Acceleration(target,position,velocity,dt,3,2.5,true);
                Assert.That(a.Length,Is.LessThanOrEqualTo(2.500001));
                velocity+=(a + new DVector3(disturbance,0,-disturbance*.7)-velocity*.2)*dt;
                position+=velocity*dt;
            }
            Assert.That((position-target).Length,Is.LessThan(.08));
            Assert.That(velocity.Length,Is.LessThan(.03));
        }
        [TestCase(123)] [TestCase(12345)] [TestCase(73)] public void NavigationWithDelayedNoisyGpsAndActuatorLagSettlesNearGoal(int seed)
        {
            var controller=new PositionController(); var filter=new GpsNavigationFilter();
            var random=new Random(seed); var position=new DVector3(-8,0,7); var target=new DVector3(5,0,-3);
            DVector3 velocity=default,actualAcceleration=default;
            var delayed=new System.Collections.Generic.Queue<(double time,DVector3 value)>();
            double sum=0; int n=0;
            for(int step=0;step<6000;step++) {
                double time=step*.01;
                if(step%10==0) delayed.Enqueue((time,position+new DVector3((random.NextDouble()-.5)*2.08,0,(random.NextDouble()-.5)*2.08)));
                while(delayed.Count>0 && delayed.Peek().time<=time-.1+1e-9) { var sample=delayed.Dequeue(); filter.Feed(sample.value,sample.time,.6); }
                var request=filter.Ready ? controller.Acceleration(target,filter.Predict(time),filter.Velocity,.01,3,2.5,true) : default;
                actualAcceleration+=(request-actualAcceleration)*(1-Math.Exp(-.01/.15));
                velocity+=(actualAcceleration+new DVector3(.2,0,-.1)-velocity*.2)*.01; position+=velocity*.01;
                if(step>5000) { sum+=(position-target).Length; n++; }
            }
            Assert.That(sum/n,Is.LessThan(.6));
            Assert.That((position-target).Length,Is.LessThan(1));
        }
        [Test] public void NoisyGpsMissionArrivesAtAllWaypointsWithoutSkipping()
        {
            var c=new PositionController(); var filter=new GpsNavigationFilter(); var mission=new WaypointMission { ArrivalRadiusM=1.5,ArrivalSpeedMps=1.2 };
            mission.Start(new[]{new DVector3(5,0,0),new DVector3(5,0,5),default(DVector3)});
            var random=new Random(12345); DVector3 position=default,velocity=default,acc=default;
            var samples=new System.Collections.Generic.Queue<(double t,DVector3 p)>();
            for(int i=0;i<18000 && !mission.Completed;i++) {
                double t=i*.01;
                if(i%10==0) samples.Enqueue((t,position+new DVector3((random.NextDouble()-.5)*2.08,0,(random.NextDouble()-.5)*2.08)));
                while(samples.Count>0 && samples.Peek().t<=t-.1+1e-9) { var sample=samples.Dequeue(); filter.Feed(sample.p,sample.t,.6); }
                if(!filter.Ready) continue;
                var estimate=filter.Predict(t);
                var desired=c.Acceleration(mission.Target,estimate,filter.Velocity,.01,3,2.5,true);
                acc+=(desired-acc)*(1-Math.Exp(-.01/.15)); velocity+=(acc-velocity*.2)*.01; position+=velocity*.01;
                mission.Step(estimate,filter.Velocity.Length,.01,true);
            }
            Assert.That(mission.Completed,Is.True,"Mission must advance with the default GPS noise, not wait forever for an exact coordinate.");
            Assert.That(mission.Index,Is.EqualTo(2)); Assert.That(position.Length,Is.LessThan(1.5));
        }
        [Test] public void IdealGpsFilterTracksConstantVelocityAndResetRemovesIt()
        {
            var filter=new GpsNavigationFilter(); filter.Feed(default,0,0); filter.Feed(new DVector3(.3,0,0),.1,0);
            Assert.That(filter.Velocity.X,Is.EqualTo(3).Within(1e-9));
            Assert.That(filter.Predict(.2).X,Is.EqualTo(.6).Within(1e-9));
            filter.Reset(); Assert.That(filter.Ready,Is.False); Assert.That(filter.Velocity.Length,Is.Zero);
        }
        [Test] public void VerticalErrorDoesNotProduceHorizontalAcceleration()
        {
            var c=new PositionController();
            Assert.That(c.Acceleration(new DVector3(0,100,0),default,default,.02,3,2.5,true).Length,Is.EqualTo(0));
        }
        [Test] public void ResetRemovesDisturbanceTrim()
        {
            var c=new PositionController();
            for(int i=0;i<100;i++) c.Acceleration(new DVector3(.2,0,0),default,default,.02,3,2.5,true);
            Assert.That(c.Acceleration(default,default,default,.02,3,2.5,false).Length,Is.GreaterThan(0));
            c.Reset(); Assert.That(c.Acceleration(default,default,default,.02,3,2.5,false).Length,Is.EqualTo(0));
        }
        [Test] public void SaturationDoesNotAccumulateTrim()
        {
            var c=new PositionController();
            for(int i=0;i<1000;i++) c.Acceleration(new DVector3(100,0,100),default,default,.02,3,.4,true);
            Assert.That(c.Acceleration(default,default,default,.02,3,.4,false).Length,Is.EqualTo(0));
        }
        [Test] public void MissionRequiresSlowArrivalAndDwellAndFinishesInOrder()
        {
            var m=new WaypointMission(); var a=new DVector3(0,10,0); var b=new DVector3(5,10,5);
            m.Start(new[]{a,b});
            for(int i=0;i<100;i++) m.Step(a,2,.02,true);
            Assert.That(m.Index,Is.EqualTo(0));
            for(int i=0;i<38;i++) m.Step(a,.1,.02,true);
            Assert.That(m.Index,Is.EqualTo(1));
            Assert.That(m.Active,Is.True);
            for(int i=0;i<38;i++) m.Step(b,.1,.02,true);
            Assert.That(m.Completed,Is.True); Assert.That(m.Active,Is.False);
        }
        [Test] public void MissionDoesNotSkipPointsOrContinueAfterGpsLoss()
        {
            var m=new WaypointMission(); var b=new DVector3(5,0,5); m.Start(new[]{default(DVector3),b});
            for(int i=0;i<100;i++) m.Step(b,0,.02,true);
            Assert.That(m.Index,Is.EqualTo(0));
            m.Step(default,0,.02,false); Assert.That(m.Active,Is.False); Assert.That(m.Completed,Is.False);
            m.Step(default,0,1,true); Assert.That(m.Active,Is.False);
        }
        [Test] public void MissionValidatesPointsBeforeReplacingActiveRoute()
        {
            var m=new WaypointMission(); m.Start(new[]{default(DVector3)});
            Assert.Throws<ArgumentException>(()=>m.Start(new[]{new DVector3(double.NaN,0,0)}));
            Assert.That(m.Active,Is.True); Assert.That(m.Target.Length,Is.EqualTo(0));
        }
        [Test] public void CsvCadenceRecordsFirstSampleAndHonorsIntervalAndReset()
        {
            var c=new RecordingCadence(); int count=0;
            for(int i=0;i<=100;i++) if(c.Due(i*.01,.1)) count++;
            Assert.That(count,Is.EqualTo(11));
            Assert.That(c.Due(0,.1),Is.True); Assert.That(c.Due(.02,.1),Is.False); Assert.That(c.Due(.1,.1),Is.True);
        }
        [Test] public void CsvCadenceDoesNotFabricateRowsAcrossSkippedTime()
        {
            var c=new RecordingCadence(); Assert.That(c.Due(0,.1),Is.True);
            Assert.That(c.Due(3,.1),Is.True); Assert.That(c.Due(3.01,.1),Is.False);
        }
    }
}
