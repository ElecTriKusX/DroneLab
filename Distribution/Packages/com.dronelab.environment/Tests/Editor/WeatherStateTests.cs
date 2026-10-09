using System;
using DroneLab.Physics;
using NUnit.Framework;

namespace DroneLab.Weather.Tests
{
    public sealed class WeatherStateTests
    {
        private static WeatherSnapshot Snapshot(double t=293.15,double p=101325,double wind=5)
            =>new WeatherSnapshot("Rain",new DVector3(wind,0,0),t,.5,0,.2,1,
                new AtmosphereColumn(t,p,2000,100),new WeatherSample("Rain",10));
        [Test] public void BeforeFirstSnapshotProviderExplicitlyUsesFallback()
        {
            var state=new WeatherState();
            Assert.That(state.TrySampleAir(default,0,out _),Is.False);
            Assert.That(state.TrySampleWeather(out _),Is.False); Assert.That(state.Sample(default,0).Length,Is.Zero);
        }
        [Test] public void WindAirAndPrecipitationUseOnePublishedSnapshot()
        {
            var state=new WeatherState(); var snapshot=Snapshot(); state.Publish(snapshot);
            state.TrySampleAir(new DVector3(0,100,0),4,out var air); state.TrySampleWeather(out var weather);
            Assert.That(state.Current,Is.SameAs(snapshot)); Assert.That(air.TemperatureK,Is.EqualTo(snapshot.TemperatureK));
            Assert.That(air.PressurePa,Is.EqualTo(101325)); Assert.That(air.AltitudeM,Is.EqualTo(2000));
            Assert.That(air.Density,Is.EqualTo(101325/(Atmosphere.GasConstant*293.15)).Within(1e-12));
            Assert.That(state.Sample(default,4).X,Is.EqualTo(5)); Assert.That(weather.Precipitation,Is.EqualTo("Rain"));
            Assert.That(weather.IntensityMmPerHour,Is.EqualTo(10));
        }
        [Test] public void QueriesAreReadOnlyAndNewPublicationDoesNotMutatePreviousSnapshot()
        {
            var state=new WeatherState(); var old=Snapshot(); state.Publish(old);
            var point=new DVector3(1,500,2); state.TrySampleAir(point,10,out var before);
            for(int i=0;i<100;i++) { state.Sample(new DVector3(i,i,0),i); state.TrySampleAir(new DVector3(i,i,0),i,out _); }
            state.TrySampleAir(point,10,out var after); Assert.That(after.Density,Is.EqualTo(before.Density));
            Assert.That(state.Current,Is.SameAs(old));
            state.Publish(Snapshot(313.15,80000,8)); state.TrySampleAir(new DVector3(0,100,0),10,out var changed);
            Assert.That(changed.TemperatureK,Is.EqualTo(313.15)); Assert.That(changed.PressurePa,Is.EqualTo(80000));
            Assert.That(state.Sample(default,10).X,Is.EqualTo(8)); Assert.That(old.TemperatureK,Is.EqualTo(293.15));
        }
        [Test] public void InvalidPublicationCannotReplaceLastValidState()
        {
            var state=new WeatherState(); var valid=Snapshot(); state.Publish(valid);
            Assert.Throws<ArgumentNullException>(()=>state.Publish(null));
            Assert.Throws<ArgumentException>(()=>state.Publish(new WeatherSnapshot("Diagnostic",default,293,.1,0,0,1)));
            Assert.Throws<ArgumentException>(()=>new WeatherSnapshot("Mismatch",default,293,.1,0,0,1,new AtmosphereColumn(280,101325)));
            Assert.That(state.Current,Is.SameAs(valid));
        }
    }
}
