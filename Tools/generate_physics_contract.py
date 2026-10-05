"""Generate DTOs, JSON Schemas and field reference from one versioned contract.
Run from any directory: python Tools/generate_physics_contract.py
No third-party dependencies. Semantic/capability checks live in ProfileLoader.cs.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / 'Assets/DronePhysics'
D = {}
def num(lo=None, hi=None, positive=False):
    s = {'type': 'number'}
    if lo is not None: s['minimum'] = lo
    if hi is not None: s['maximum'] = hi
    if positive: s['exclusiveMinimum'] = 0
    return s
def integer(lo=0): return {'type': 'integer', 'minimum': lo}
def enum(*values): return {'type': 'string', 'enum': list(values)}
def ref(name): return {'$ref': '#/$defs/' + name}
def arr(item, minimum=0): return {'type': 'array', 'items': item, 'minItems': minimum}
def vec(n=3): return dict(arr(num()), minItems=n, maxItems=n)
def obj(name, fields, optional=()):
    D[name] = {'type': 'object', 'additionalProperties': False,
               'properties': fields, 'required': [k for k in fields if k not in optional]}
    return ref(name)
S = {'type': 'string', 'minLength': 1}
B = {'type': 'boolean'}
P = num(positive=True)
N = num(0)
F = num(0, 1)
obj('Metadata', {'name': S, 'manufacturer': S, 'model': S, 'description': S}, ('manufacturer','model','description'))
obj('CoordinateSystem', {'convention': enum('UnityLeftHandedYUpZForward'), 'lengthUnit': enum('m'),
    'modelScaleMetersPerUnit': P})
obj('PhysicsModulesProfile', {k:B for k in ['motorResponse','bodyDrag','windInteraction','groundEffect',
    'rotorAerodynamics','bladeFlapping','inducedDrag','batteryDischarge','batteryVoltageSag',
    'motorElectrical','gyroscopicRotorEffects']})
obj('PhysicsConfiguration', {'fidelity': enum('Basic','Advanced'), 'modules':ref('PhysicsModulesProfile')})
obj('InertiaProfile', {'mode':enum('AutoBox','ManualPrincipal'), 'principalMomentsKgM2':vec(),
    'principalAxesRotationXyzw':vec(4)}, ('principalMomentsKgM2','principalAxesRotationXyzw'))
obj('MassProperties', {'massKg':P,'centerOfMassLocalM':vec(),'dimensionsM':vec(),'inertia':ref('InertiaProfile')})
obj('EfficiencyPoint', {'loadFraction':F,'efficiency':num(0.000001,1)})
obj('MotorElectricalProfile', {'motorKvRpmPerVolt':P,'motorResistanceOhm':P,'noLoadCurrentA':N,
    'maxCurrentA':P,'maxPowerW':P,'motorEfficiency':num(0.000001,1),
    'efficiencyCurve':arr(ref('EfficiencyPoint')),'escEfficiency':num(0.000001,1),'escMaxCurrentA':P}, ('efficiencyCurve','maxPowerW'))
obj('MotorProfile', {'minRpm':N,'idleRpm':N,'maxRpm':P,'responseTimeUpS':N,'responseTimeDownS':N,
    'rotatingInertiaKgM2':N,'electrical':ref('MotorElectricalProfile')}, ('rotatingInertiaKgM2','electrical'))
obj('PropellerProfile', {'diameterM':P,'pitchM':N,'bladeCount':integer(1)})
obj('RpmPerformancePoint', {'rpm':N,'thrustN':N,'torqueNm':N,'currentA':N}, ('currentA',))
obj('PerformanceMapPoint', {'rpm':P,'advanceRatio':num(),'ct':num(),'cq':num(),'reynolds':P}, ('reynolds',))
obj('RotorPerformanceProfile', {'model':enum('OmegaSquared','RpmTable','CtCq','PerformanceMap'),
    'kThrustNPerRadPerSecSquared':P,'kTorqueNmPerRadPerSecSquared':N,'referenceAirDensityKgM3':P,
    'ct':P,'cq':N,'rpmTable':arr(ref('RpmPerformancePoint'),2),
    'performanceMap':arr(ref('PerformanceMapPoint'),2),'outOfRangePolicy':enum('Reject','Clamp')},
    ('kThrustNPerRadPerSecSquared','kTorqueNmPerRadPerSecSquared','referenceAirDensityKgM3','ct','cq','rpmTable','performanceMap','outOfRangePolicy'))
obj('RotorGeometry', {'positionLocalM':vec(),'thrustAxisLocal':vec(),'spinDirection':enum('CW','CCW')})
obj('RotorAerodynamicsProfile', {'rotorDragCoefficientKgPerRad':N,'bladeFlappingCoefficient':N,
    'inducedDragCoefficient':N}, ('bladeFlappingCoefficient','inducedDragCoefficient'))
obj('RotorProfile', {'rotorId':S,'geometry':ref('RotorGeometry'),'motor':ref('MotorProfile'),
    'propeller':ref('PropellerProfile'),'performance':ref('RotorPerformanceProfile'),
    'advancedAerodynamics':ref('RotorAerodynamicsProfile')}, ('advancedAerodynamics',))
obj('ProjectedAreaSample', {'directionLocal':vec(),'areaM2':P})
obj('ProjectedAreaProfile', {'mode':enum('MeshDirectionalLUT','ManualDirectionalLUT','AxisApproximation'),
    'samples':arr(ref('ProjectedAreaSample'),1),'referenceAreaM2':vec()}, ('samples','referenceAreaM2'))
obj('AeroSurfaceProfile', {'surfaceId':S,'positionLocalM':vec(),'normalLocal':vec(),'areaM2':P,'dragCoefficient':N})
obj('BodyAerodynamicsProfile', {'model':enum('AxisApproximation','ProjectedArea','Surfaces'),
    'dragCd':vec(),'referenceAreaM2':vec(),'dragCoefficient':N,'projectedArea':ref('ProjectedAreaProfile'),
    'dragApplicationPointLocalM':vec(),'surfaces':arr(ref('AeroSurfaceProfile'),1)},
    ('dragCd','referenceAreaM2','dragCoefficient','projectedArea','dragApplicationPointLocalM','surfaces'))
obj('GroundEffectProfile', {'coefficient':N,'minHeightRadiusRatio':P,'maxMultiplier':num(1)})
obj('OcvPoint', {'soc':F,'voltageV':P})
obj('BatteryProfile', {'mode':enum('None','Simple','Electrical'),'cellCount':integer(1),'nominalVoltageV':P,
    'capacityAh':P,'initialSoc':F,'internalResistanceOhm':N,'maxDischargeCurrentA':P,
    'ocvCurve':arr(ref('OcvPoint'),2)}, ('cellCount','nominalVoltageV','capacityAh','initialSoc','internalResistanceOhm','maxDischargeCurrentA','ocvCurve'))
obj('PowerSystemProfile', {'battery':ref('BatteryProfile')})
obj('DerivedProfile', {'projectedAreaLut':arr(ref('ProjectedAreaSample')),'hoverRpm':N,
    'maxTotalThrustN':N,'thrustToWeightRatio':N,'rotorDiskAreasM2':arr(P),'approximateInertiaKgM2':vec()},
    ('projectedAreaLut','hoverRpm','maxTotalThrustN','thrustToWeightRatio','rotorDiskAreasM2','approximateInertiaKgM2'))
obj('ParameterProvenance', {'path':S,'sourceType':enum('Manufacturer','Measured','Calculated','AutoGeometry','Preset','Estimated','User'),
    'source':S,'confidence':F})
obj('DroneProfile', {'schemaVersion':enum('1.0.0'),'metadata':ref('Metadata'),'coordinateSystem':ref('CoordinateSystem'),
    'physicsConfiguration':ref('PhysicsConfiguration'),'massProperties':ref('MassProperties'),
    'rotors':arr(ref('RotorProfile'),1),'bodyAerodynamics':ref('BodyAerodynamicsProfile'),
    'groundEffect':ref('GroundEffectProfile'),'powerSystem':ref('PowerSystemProfile'),
    'derived':ref('DerivedProfile'),'parameterProvenance':arr(ref('ParameterProvenance'))}, ('groundEffect','derived'))
obj('EnvironmentProfile', {'schemaVersion':enum('1.0.0'),'gravityMps2':P,'airDensityMode':enum('Constant','StandardAtmosphere'),
    'airDensityKgM3':P,'temperatureK':P,'pressurePa':P,'altitudeM':num(),
    'windMode':enum('None','Constant','Gust','Turbulence','CustomField'),'windVelocityWorldMps':vec(),
    'gustEnabled':B,'gustIntensityMps':N,'gustTimeScaleS':P,'turbulenceSeed':integer()},
    ('temperatureK','pressurePa','altitudeM','gustIntensityMps','gustTimeScaleS','turbulenceSeed'))

def write(path, text):
    p = ROOT / path
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text,encoding='utf-8')
def cs_type(s):
    if '$ref' in s:return s['$ref'].split('/')[-1]
    return {'number':'double','integer':'int','string':'string','boolean':'bool','array':None}.get(s.get('type')) or cs_type(s['items'])+'[]'

code = '// Generated by Tools/generate_physics_contract.py. Edit the generator.\nusing System;\n\nnamespace DroneLab.Physics\n{\n'
for name,s in D.items():
    code += '    [Serializable]\n    public sealed class '+name+'\n    {\n'
    for key,field in s['properties'].items():code += '        public '+('double?' if name == 'RpmPerformancePoint' and key == 'currentA' else cs_type(field))+' '+key+';\n'
    code += '    }\n\n'
code += '}\n'
write('Assets/DronePhysics/Core/Profiles.Generated.cs',code)
for root,file in [('DroneProfile','drone-profile'),('EnvironmentProfile','environment-profile')]:
    # Include only definitions reachable from this profile's root.
    needed = set()
    def collect(rule):
        if isinstance(rule, dict):
            if '$ref' in rule:
                name = rule['$ref'].split('/')[-1]
                if name not in needed:
                    needed.add(name)
                    collect(D[name])
            for value in rule.values(): collect(value)
        elif isinstance(rule, list):
            for value in rule: collect(value)
    collect(ref(root))
    schema={'$schema':'https://json-schema.org/draft/2020-12/schema','$id':'urn:dronelab:'+file+':1.0.0',
        '$ref':'#/$defs/'+root,'$defs':{name: rule for name,rule in D.items() if name in needed}}
    write('Assets/DronePhysics/Resources/DronePhysics/'+file+'.schema.json',json.dumps(schema,indent=2)+'\n')
doc='# Реестр параметров — контракт 1.0.0\n\nСгенерировано из `Tools/generate_physics_contract.py`. Все величины SI; RPM — об/мин; quaternion — [x,y,z,w]. `?` означает необязательное поле, а не автоматически реализованный эффект. Условные требования и доступность исполнения проверяются дополнительно в ProfileLoader.\n\n'
for name,s in D.items():
    doc+='## '+name+'\n\n| Поле | Тип / ограничения |\n|---|---|\n'
    for key,field in s['properties'].items():
        desc=field.get('enum') or field.get('$ref',field.get('type'))
        doc+='| `'+key+('' if key in s['required'] else '?')+'` | '+str(desc).replace('#/$defs/','')+' |\n'
    doc+='\n'
write('Docs/Physics/PARAMETERS.md',doc.rstrip()+'\n')
print('Generated DTOs, two schemas, parameter reference.')
