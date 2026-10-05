"""Rebuild reference profiles/benchmarks from explicitly sourced numerical values; no network.
Never changes the JSON contract or treats estimates as measured data.
"""
from pathlib import Path
import json,math,copy,uuid
root=Path(__file__).resolve().parents[1];resources=root/'Assets/DronePhysics/Resources/DronePhysics'
base=json.loads((resources/'quad_test_basic.json').read_text())
BIT='https://www.bitcraze.io/documentation/repository/crazyflie-firmware/master/functional-areas/pwm-to-thrust/'
PAPER='https://arxiv.org/pdf/2603.05944'
HUM='https://archive.air.in.tum.de/Main/Publications/Klose2011a.pdf'
PYB='https://github.com/utiasDSL/gym-pybullet-drones/blob/7ebad1ecabd28a7000add2d05f888aa2e837c2cc/gym_pybullet_drones/assets/cf2x.urdf'
JAX='https://github.com/Data-Science-in-Mechanical-Engineering/CrazyflieBrushJAX/blob/5e449f116b03218e803e728f2a9d8f68f60b05fe/environment/quadcopter.py'
UIUC='https://m-selig.ae.illinois.edu/props/volume-1/data/'
def save(p,data):
    p.write_text(json.dumps(data,indent=2,ensure_ascii=False)+'\n')
    if 'Assets' in p.parts and not Path(str(p)+'.meta').exists(): Path(str(p)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
def prov(path,kind,source,confidence): return dict(path=path,sourceType=kind,source=source,confidence=confidence)
def profile(name,model,mass,inertia,dim,pos,diameter,pitch,maxrpm,up,down):
    p=copy.deepcopy(base);p['metadata']=dict(name=name,manufacturer=model.split(' ')[0],model=model,description='Open reference profile; sources and estimates are explicit. Static/low-speed validation only, not a calibrated full aircraft digital twin.')
    p['physicsConfiguration']['fidelity']='Basic'
    for key in p['physicsConfiguration']['modules']:p['physicsConfiguration']['modules'][key]=key in ['motorResponse','windInteraction']
    p['massProperties']=dict(massKg=mass,centerOfMassLocalM=[0,0,0],dimensionsM=dim,inertia=dict(mode='ManualPrincipal',principalMomentsKgM2=inertia,principalAxesRotationXyzw=[0,0,0,1]))
    p['bodyAerodynamics']=dict(model='AxisApproximation',dragCd=[0,0,0],referenceAreaM2=[dim[1]*dim[2],dim[0]*dim[2],dim[0]*dim[1]])
    p['parameterProvenance']=[prov('bodyAerodynamics / physicsConfiguration.modules','Estimated','Unknown body Cd/advanced rotor/thermal/electrical parameters: effects disabled, not fabricated. Body geometry only a reference envelope; not hardware mass distribution.',.1)]
    for i,r in enumerate(p['rotors']):
        r['rotorId']=['FL','FR','RR','RL'][i]
        r['geometry']=dict(positionLocalM=pos[i],thrustAxisLocal=[0,1,0],spinDirection='CW' if i%2==0 else 'CCW')
        r['motor']=dict(minRpm=0,idleRpm=0,maxRpm=maxrpm,responseTimeUpS=up,responseTimeDownS=down)
        r['propeller']=dict(diameterM=diameter,pitchM=pitch,bladeCount=2)
    return p
# Numerical transcription of manufacturer bench table; 4 simultaneously driven rotors.
values=[(0,.0,.24,4.01),(4485,1.6,.37,3.98),(7570,4.8,.56,3.95),(9374,7.9,.75,3.92),(10885,10.9,.94,3.88),(12277,13.9,1.15,3.84),(13522,17.3,1.37,3.80),(14691,21.,1.59,3.76),(15924,24.4,1.83,3.71),(17174,28.6,2.11,3.67),(18179,32.8,2.39,3.65),(19397,37.3,2.71,3.62),(20539,41.7,3.06,3.56),(21692,46.,3.46,3.48),(22598,51.9,3.88,3.40),(23882,57.9,4.44,3.30)]
train=set([0,*range(1,16,2)]);rows=[dict(rpm=r,totalThrustGram=t,totalCurrentA=i,voltageV=v,role='training' if k in train else 'holdout') for k,(r,t,i,v) in enumerate(values)]
p=profile('Crazyflie 2.0 — Bitcraze bench reference','Bitcraze Crazyflie 2.0',.027,[1.4e-5,2.17e-5,1.4e-5],[.12,.025,.12],[[-.028,0,.028],[.028,0,.028],[.028,0,-.028],[-.028,0,-.028]],.0462696,.03,23882,.06,.08)
for r in p['rotors']:
    r['performance']=dict(model='RpmTable',referenceAirDensityKgM3=1.225,outOfRangePolicy='Clamp',rpmTable=[dict(rpm=row['rpm'],thrustN=row['totalThrustGram']*.00980665/4,torqueNm=row['totalThrustGram']*.00980665/4*.006) for row in rows if row['role']=='training'])
p['parameterProvenance'] += [prov('rotors.performance.rpmTable.thrustN','Measured',BIT+' ; 2015 four-rotor total thrust divided by four, g-force converted with g0=9.80665. Alternating rows withheld. Actual test density unknown; 1.225 is assumed reference only.',.8),prov('massProperties.massKg / massProperties.inertia / rotors.geometry', 'Preset',PYB+' ; source x-forward y-left z-up converted to Unity [-y,z,x]. Inertia from public simulation model, not this bench aircraft.',.5),prov('rotors.performance.rpmTable.torqueNm / rotors.motor / rotors.propeller.pitchM','Estimated','Q/T=0.006 m proxy, motor lag 0.06/0.08 s and 30 mm pitch not measured in Bitcraze table. Current/voltage stay in benchmark metadata, battery disabled.',.1)]
save(resources/'reference_crazyflie20.json',p)
# Published identified fit, not raw measured sample data. Use paper Table II for guarded variant;
# exact polynomial coefficients from accompanying code, not conflicting code-default inertias.
p=profile('Crazyflie Brushless — guarded published fit','Bitcraze Crazyflie Brushless',.044,[3.6e-5,5.9e-5,3.3e-5],[.13,.03,.13],[[-.03535,0,.03535],[.03535,0,.03535],[.03535,0,-.03535],[-.03535,0,-.03535]],.055,.035,2900*60/(2*math.pi),.05,.05)
def force(w): x=w/2900;return -.23009526*x**3+.56176458*x*x-.0433191*x
def torque(w): x=w/2900;return -.0003396*x**3+.00087032*x*x+.0002896*x
omegas=[0,*range(250,2901,50)]
for r in p['rotors']:r['performance']=dict(model='RpmTable',referenceAirDensityKgM3=1.225,outOfRangePolicy='Clamp',rpmTable=[dict(rpm=w*60/(2*math.pi),thrustN=max(0,force(w)),torqueNm=torque(w)) for w in omegas])
p['parameterProvenance'] += [prov('massProperties.massKg / massProperties.inertia / rotors.geometry / rotors.motor','Measured',PAPER+' ; Table II guarded variant: 44 g, ROS inertias [3.3,3.6,5.9]e-5, width 7.07 cm, tau 0.05 s, gain 2900 rad/s. Identified fit, no raw traces imported. Code defaults disagree: paper variant explicitly selected.',.75),prov('rotors.performance','Calculated',JAX+' ; published identified cubic sampled at 50 rad/s spacing, positive branch >=250 rad/s. Clamp at stop; artificial join 0..250 not validated. Includes friction in torque fit.',.7),prov('massProperties.dimensionsM / rotors.propeller','Estimated','Visualization bounds estimate; 55x35 mm propeller specification. No density scaling or extra airflow gains on fitted curves.',.3)]
save(resources/'reference_crazyflie_brushless.json',p)
p=profile('AscTec Hummingbird — Wang identification','AscTec Hummingbird',.68,[.007,.012,.007],[.54,.12,.54],[[0,.01,.17],[.17,.01,0],[0,.01,-.17],[-.17,.01,0]],.2,.1,math.sqrt(3.5/5.7e-8),.0125,.025)
for i,r in enumerate(p['rotors']):
    r['rotorId']=['F','R','B','L'][i]
    r['performance']=dict(model='OmegaSquared',kThrustNPerRadPerSecSquared=5.7e-8*(60/(2*math.pi))**2,kTorqueNmPerRadPerSecSquared=.016*5.7e-8*(60/(2*math.pi))**2,referenceAirDensityKgM3=1.225)
p['parameterProvenance'] += [prov('massProperties.massKg / massProperties.inertia / rotors.geometry / rotors.performance','Measured',HUM+' ; Table 1: total mass .68 kg, arm .17 m, Iroll/pitch .007, Iyaw .012, thrust 5.7e-8 N/RPM^2, Q/T .016 m. Paper vehicle referenced by RotorS Hummingbird. Unity inertia permutation; no extra rotor mass on published total.',.75),prov('rotors.motor.maxRpm','Calculated',HUM+' ; maximum 3.5 N/rotor -> sqrt(3.5/5.7e-8) RPM. Published saturation, not independently measured maximum RPM.',.6),prov('rotors.motor.responseTimeUpS / rotors.motor.responseTimeDownS','Estimated',HUM+' ; paper identifies propulsion/thrust lag 1/80 and 1/40 s; reused as RPM first-order approximation near hover, not an exact thrust startup match.',.4),prov('massProperties.dimensionsM / rotors.propeller / rotors.geometry.positionLocalM[1]','Estimated','RotorS visual geometry reference: radius 0.1 m, offset .01 m; pitch .1 m placeholder. RotorS motor_constant=8.54858e-6 differs from paper; not used.',.2)]
save(resources/'reference_hummingbird.json',p)
# Separate propeller holder, explicitly not a fourth real aircraft.
stat=[tuple(map(float,line.split())) for line in (root/'Docs/Physics/Data/apcsf_10x4.7_static_kt0835.txt').read_text().splitlines()[1:]]
uiucRows=[dict(rpm=r,ct=ct,cp=cp,role='training' if k%2==0 or k==15 else 'holdout') for k,(r,ct,cp) in enumerate(stat)]
p=copy.deepcopy(p);p['metadata']=dict(name='UIUC APC 10x4.7 SF static bench holder',manufacturer='DroneLab / APC',model='Synthetic holder, APC 10x4.7 SF',description='Measured propeller on a synthetic multirotor holder; not stock Hummingbird or a measured aircraft. Training rows only; independent holdout comparisons in benchmark file.')
p['parameterProvenance']=[prov('massProperties / rotors.geometry / rotors.motor / physicsConfiguration.modules','Estimated','Synthetic holder geometry copied from Hummingbird reference; all battery/aero extras disabled. Startup join below 2377 RPM synthetic, excluded from accuracy claims.',.1),prov('rotors.propeller / rotors.performance','Measured',UIUC+'apcsf_10x4.7_static_kt0835.txt ; UIUC Volume 1 v3 static CT/CP; D=0.254 m, pitch=.11938 m. CP/(2pi)=CQ; forces assume rho=1.225; validation uses measured coefficients, not direct measured N.',.8)]
for r in p['rotors']:
    r['motor']['maxRpm']=6528;r['propeller']=dict(diameterM=.254,pitchM=.11938,bladeCount=2)
    r['performance']=dict(model='RpmTable',referenceAirDensityKgM3=1.225,outOfRangePolicy='Clamp',rpmTable=[dict(rpm=0,thrustN=0,torqueNm=0)]+[dict(rpm=x['rpm'],thrustN=x['ct']*1.225*(x['rpm']/60)**2*.254**4,torqueNm=x['cp']/(2*math.pi)*1.225*(x['rpm']/60)**2*.254**5) for x in uiucRows if x['role']=='training'])
save(resources/'bench_apc_10x47_static.json',p)
# Measured axial sweep at 4014 RPM, alternating J rows held out, includes negative CT.
axial=[(.142,.0994,.0451),(.177,.0944,.0446),(.216,.0885,.0436),(.255,.0829,.0427),(.289,.0770,.0414),(.325,.0719,.0407),(.361,.0652,.0392),(.399,.0582,.0375),(.438,.0502,.0352),(.468,.0441,.0335),(.510,.0343,.0308),(.534,.0288,.0292),(.577,.0178,.0258),(.612,.0083,.0228),(.647,-.0011,.0199),(.685,-.0122,.0162),(.719,-.0218,.0130)]
axialRows=[dict(advanceRatio=j,ct=t,cp=q,role='training' if k%2==0 else 'holdout') for k,(j,t,q) in enumerate(axial)]
staticCt=.1143+(.1158-.1143)*(4014-3762)/(4029-3762);staticCp=.0460+(.0466-.0460)*(4014-3762)/(4029-3762)
p=copy.deepcopy(p);p['metadata']['name']='UIUC APC 10x4.7 SF axial bench holder';p['metadata']['description']='Single measured 4014 RPM axial sweep; finite J range, not a multi-RPM flight map. Synthetic J=0 anchor from static interpolation; negative-thrust branch is tested, electrical/extra airflow gains disabled.'
p['parameterProvenance'][-1]=prov('rotors.performance', 'Measured',UIUC+'apcsf_10x4.7_kt0836_4014.txt ; alternating measured J holdouts. J=0 anchor calculated from static rows 3762/4029 RPM; rho assumed 1.225. One RPM only; RPM clamp does not validate other RPM.',.8)
for r in p['rotors']:
    r['motor']['maxRpm']=4014
    r['performance']=dict(model='PerformanceMap',referenceAirDensityKgM3=1.225,outOfRangePolicy='Clamp',performanceMap=[dict(rpm=4014,advanceRatio=0,ct=staticCt,cq=staticCp/(2*math.pi))]+[dict(rpm=4014,advanceRatio=x['advanceRatio'],ct=x['ct'],cq=x['cp']/(2*math.pi)) for x in axialRows if x['role']=='training'])
save(resources/'bench_apc_10x47_axial.json',p)
bench=dict(formatVersion='1.0.0',retrievedAt='2026-10-06',scope='Static measured holdouts / published identified curve reproduction, not whole-flight calibration.',datasets=[dict(id='cf20_bitcraze_2015',kind='MeasuredStatic',profile='reference_crazyflie20',sourceUrl=BIT,rows=rows),dict(id='uiuc_apc_10x47_static',kind='MeasuredStaticCoefficients',profile='bench_apc_10x47_static',sourceUrl=UIUC+'apcsf_10x4.7_static_kt0835.txt',assumedDensityKgM3=1.225,rows=uiucRows),dict(id='uiuc_apc_10x47_axial',kind='MeasuredAxialCoefficients',profile='bench_apc_10x47_axial',sourceUrl=UIUC+'apcsf_10x4.7_kt0836_4014.txt',rpm=4014,assumedDensityKgM3=1.225,rows=axialRows),dict(id='cfbrush_published_fit',kind='PublishedFit',profile='reference_crazyflie_brushless',sourceUrl=PAPER,codeUrl=JAX,scope='Representation error against identified polynomial, not independent raw measurements.',thrustCoefficients=[-.23009526,.56176458,-.0433191,0],torqueCoefficients=[-.0003396,.00087032,.0002896,0],normalizationOmegaRadS=2900,massKg=.044,inertiaSourceXyz=[3.3e-5,3.6e-5,5.9e-5],tauS=.05),dict(id='hummingbird_published',kind='PublishedIdentification',profile='reference_hummingbird',sourceUrl=HUM,massKg=.68,armM=.17,thrustNPerRpmSquared=5.7e-8,torquePerThrustM=.016,rollMomentNm=.544,yawMomentNm=.102,rollAccelerationRadS2=77.7,yawAccelerationRadS2=8.5)])
save(resources/'Benchmarks/reference-benchmarks.json',bench)
meta=Path(str(resources/'Benchmarks')+'.meta');
if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\n')

save(root/'Docs/Physics/Data/reference-source-manifest.json',dict(retrievedAt='2026-10-06',sources=[dict(name='Bitcraze2015',url=BIT),dict(name='UIUC static',url=UIUC+'apcsf_10x4.7_static_kt0835.txt'),dict(name='UIUC axial',url=UIUC+'apcsf_10x4.7_kt0836_4014.txt'),dict(name='Brushless identified paper',url=PAPER),dict(name='Brushless code',url=JAX,commit='5e449f116b03218e803e728f2a9d8f68f60b05fe'),dict(name='Hummingbird identified paper',url=HUM),dict(name='CF2 geometry',url=PYB,commit='7ebad1ecabd28a7000add2d05f888aa2e837c2cc'),dict(name='RotorS visual reference',url='https://github.com/ethz-asl/rotors_simulator',commit='cd813b7a8c375d677352aa20ad20047feb661126')]))
