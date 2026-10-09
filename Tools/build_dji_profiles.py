#!/usr/bin/env python3
"""Build approximate physics profiles and portable GLB documents from the four supplied models.
Standard library only. Source geometry/materials are preserved; Avata gets a separate visual derivative.
Usage: python3 Tools/build_dji_profiles.py INPUT_DIRECTORY OUTPUT_DIRECTORY
"""
import argparse, copy, hashlib, itertools, json, math, pathlib, shutil, struct, uuid
ROOT = pathlib.Path(__file__).resolve().parents[1]
SPECS = {
 'dji_mavic_3':dict(name='DJI Mavic 3',mass=.895,dims=[.283,.1077,.3475],diameter=.2388,pitch=.1346,blades=2,capacity=5,voltage=15.4,full=17.6,cells=4,scale=.01,yaw=180,twr=2,source='https://www.dji.com/support/product/mavic-3', hubs=[121,179,83,147],parts=[[115,118,121],[170,176,179],[83,86,89],[147,150,153]]),
 'dji_mini_3':dict(name='DJI Mini 3',mass=.248,dims=[.362,.072,.251],diameter=.1524,pitch=.0762,blades=2,capacity=2.453,voltage=7.38,full=8.5,cells=2,scale=1,yaw=180,twr=2,source='https://www.dji.com/mini-3/specs',hubs=[103,210,164,59],parts=[[90,93],[193,198],[151,154],[46,49]]),
 'dji_avata2':dict(name='DJI Avata 2',mass=.377,dims=[.212,.064,.185],diameter=.0762,pitch=.0813,blades=3,capacity=2.15,voltage=14.76,full=17,cells=4,scale=1,yaw=0,twr=3,source='https://www.dji.com/avata-2/specs',hubs=[],parts=[]),
 'dji_m600_ul_ver':dict(name='DJI Matrice 600 · baseline',mass=9.1,dims=[1.518,.759,1.668],diameter=.5334,pitch=.1778,blades=2,capacity=27,voltage=22.2,full=25.2,cells=6,scale=1.133/559.307665721,yaw=180,twr=2.2,source='https://www.dji.com/support/product/matrice600',hubs=[119,113,104,116,110,107],parts=[[119],[113],[104],[116],[110],[107]])
}
PROP_SOURCE='https://support.dji.com/help/content?customId=01700006559&documentType=&lang=en&paperDocType=ARTICLE&re=US&spaceId=17'

def read_glb(path):
 data=path.read_bytes();magic,version,length=struct.unpack_from('<III',data)
 assert magic==0x46546c67 and version==2 and length==len(data),path
 chunks=[];offset=12
 while offset<len(data):
  size,kind=struct.unpack_from('<II',data,offset);offset+=8;chunks.append((kind,data[offset:offset+size]));offset+=size
 document=json.loads(next(data for kind,data in chunks if kind==0x4e4f534a))
 return document,next(data for kind,data in chunks if kind==0x004e4942)

def identity():return [[1 if i==j else 0 for j in range(4)] for i in range(4)]
def multiply(a,b):return [[sum(a[i][k]*b[k][j] for k in range(4))for j in range(4)]for i in range(4)]
def transform(m,p):return [sum(m[i][k]*p[k] for k in range(3))+m[i][3] for i in range(3)]
def matrix(n):
 if 'matrix' in n:return [[n['matrix'][j*4+i] for j in range(4)]for i in range(4)]
 x,y,z,w=n.get('rotation',[0,0,0,1]);m=identity();rot=[[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]
 for i in range(3):
  m[i][3]=n.get('translation',[0,0,0])[i]
  for k in range(3):m[i][k]=rot[i][k]*n.get('scale',[1,1,1])[k]
 return m

def geometry(j):
 bounds={};world={};paths={}
 def visit(i,parent,path):
  n=j['nodes'][i];m=multiply(parent,matrix(n));world[i]=m;paths[i]='@/'+path;points=[]
  if 'mesh'in n:
   for primitive in j['meshes'][n['mesh']]['primitives']:
    a=j['accessors'][primitive['attributes']['POSITION']]
    assert 'min'in a and 'max'in a,'Model must have POSITION bounds'
    points.extend(transform(m,p)for p in itertools.product(*zip(a['min'],a['max'])))
  for k,c in enumerate(n.get('children',[])):
   visit(c,m,path+'/'+str(k));points.extend(bounds.get(c,[]))
  if points:bounds[i]=[[min(p[k]for p in points)for k in range(3)],[max(p[k]for p in points)for k in range(3)]]
 for k,i in enumerate(j['scenes'][j.get('scene',0)]['nodes']):visit(i,identity(),str(k))
 return bounds,world,paths

def avata_propellers(j,binary):
 """Append flat three-blade props as separate nodes, in glTF world metres; no original primitive is edited."""
 j=copy.deepcopy(j);binary=bytearray(binary[:j['buffers'][0]['byteLength']]);vertices=[];normals=[];indices=[]
 # Each blade is an extruded tapered polygon, with flat face normals.
 for blade in range(3):
  angle=blade*2*math.pi/3;c,s=math.cos(angle),math.sin(angle)
  polygon=[(.004,-.0025),(.0355,-.004),(.0374,.0015),(.008,.004)]
  v=[(c*x-s*z,y,s*x+c*z)for y in [-.0007,.0007]for x,z in polygon]
  faces=[(0,2,1),(0,3,2),(4,5,6),(4,6,7)]
  for k in range(4):a=k;b=(k+1)%4;faces.extend([(a,b,b+4),(a,b+4,a+4)])
  for face in faces:
   face=face[::-1] # Outward winding/normals for the glTF right-handed mesh.
   a,b,d=[v[i]for i in face];u=[b[k]-a[k]for k in range(3)];w=[d[k]-a[k]for k in range(3)];normal=[u[1]*w[2]-u[2]*w[1],u[2]*w[0]-u[0]*w[2],u[0]*w[1]-u[1]*w[0]];length=math.sqrt(sum(n*n for n in normal));normal=[n/length for n in normal]
   for vertex in [a,b,d]:indices.append(len(vertices));vertices.append(vertex);normals.append(normal)
 def accessor(values,component,kind,target):
  while len(binary)%4:binary.append(0)
  offset=len(binary);flat=[v for row in values for v in row]if kind=='VEC3'else values
  binary.extend(struct.pack('<'+('f'if component==5126 else'H')*len(flat),*flat));view=len(j['bufferViews']);j['bufferViews'].append(dict(buffer=0,byteOffset=offset,byteLength=len(binary)-offset,target=target));a=dict(bufferView=view,componentType=component,count=len(values),type=kind)
  if kind=='VEC3':a.update(min=[min(v[k]for v in values)for k in range(3)],max=[max(v[k]for v in values)for k in range(3)])
  index=len(j['accessors']);j['accessors'].append(a);return index
 position=accessor(vertices,5126,'VEC3',34962);normal=accessor(normals,5126,'VEC3',34962);index=accessor(indices,5123,'SCALAR',34963)
 material=len(j['materials']);j['materials'].append(dict(name='DroneLab estimated propeller',pbrMetallicRoughness=dict(baseColorFactor=[.055,.055,.055,1],metallicFactor=0,roughnessFactor=.65)))
 mesh=len(j['meshes']);j['meshes'].append(dict(name='Approximate 3032S visual',primitives=[dict(attributes={'POSITION':position,'NORMAL':normal},indices=index,material=material)]))
 nodes=[]
 # Avata's camera faces source +Z; glTFast mirrors X, therefore FL has source +X.
 for rotor,x,z in [('FL',.057,.044),('FR',-.057,.044),('RR',-.057,-.044),('RL',.057,-.044)]:
  i=len(j['nodes']);j['nodes'].append(dict(name='DroneLab_Propeller_'+rotor,mesh=mesh,translation=[-.000772663153+x,.0005,.023632409265+z]));j['scenes'][j.get('scene',0)]['nodes'].append(i);nodes.append(i)
 j['buffers'][0]['byteLength']=len(binary)
 while len(binary)%4:binary.append(0)
 encoded=json.dumps(j,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4)
 result=struct.pack('<III',0x46546c67,2,12+8+len(encoded)+8+len(binary))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(binary),0x004e4942)+binary
 return j,result,nodes

def write_json(path,value):path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
def build(inputs,output):
 template=json.loads((ROOT/'Assets/DronePhysics/Resources/DronePhysics/quad_test_battery_simple.json').read_text(encoding='utf-8'))
 report=[]
 for key,s in SPECS.items():
  original=inputs/(key+'.glb');j,binary=read_glb(original);folder=output/key;model=folder/'model';model.mkdir(parents=True,exist_ok=True);derived=False
  parts=s['parts'];hubs=s['hubs'];filename=original.name
  if key=='dji_avata2':j,data,hubs=avata_propellers(j,binary);parts=[[i]for i in hubs];filename='dji_avata2_with_propellers.glb';(model/filename).write_bytes(data);derived=True
  else:shutil.copyfile(original,model/filename)
  bounds,world,paths=geometry(j);scene_roots=j['scenes'][j.get('scene',0)]['nodes'];lo=[min(bounds[i][0][k]for i in scene_roots)for k in range(3)];hi=[max(bounds[i][1][k]for i in scene_roots)for k in range(3)];center=[(a+b)/2 for a,b in zip(lo,hi)]
  def physical(point):
   # glTFast glTF->Unity mirrors X; yaw=180 restores X and flips Z.
   p=[point[k]-center[k]for k in range(3)];return [round((-p[0]if s['yaw']==0 else p[0])*s['scale'],7),round(p[1]*s['scale'],7),round((p[2]if s['yaw']==0 else-p[2])*s['scale'],7)]
  p=copy.deepcopy(template);p.pop('groundEffect',None);p['metadata']=dict(name=s['name']+' · приближённый',manufacturer='DJI',model=s['name'],description='Published mass, dimensions, propeller size and nominal battery data; estimated COM, AutoBox inertia, rotor axes/spin, Ct/Cq, motor and discharge parameters. Not a bench-calibrated flight model.')
  if key=='dji_mini_3':p['metadata']['description']+=' GLB labels include WM162; file title is Mini 3, exact variant/pose not verified.'
  if key=='dji_m600_ul_ver':p['metadata']['description']+=' Baseline M600 with six TB47S, no payload; UL modification mass unknown. Battery equivalent: six parallel 6S packs, not 36S. Hexacopter requires a six-rotor controller; built-in quad pilot is incompatible.'
  if derived:p['metadata']['description']+=' Original Avata GLB has no separate blades; this copy adds approximate three-blade visual meshes only.'
  p['coordinateSystem']['modelScaleMetersPerUnit']=s['scale'];p['physicsConfiguration']['fidelity']='Basic'
  modules=p['physicsConfiguration']['modules'];modules.update(groundEffect=False,rotorAerodynamics=False,batteryDischarge=True,batteryVoltageSag=True,motorElectrical=False)
  p['massProperties']=dict(massKg=s['mass'],centerOfMassLocalM=[0,0,0],dimensionsM=s['dims'],inertia={'mode':'AutoBox'})
  count=len(hubs);ct=.1;cq=.008 if s['blades']==2 else .012;maxrpm=60*math.sqrt(s['mass']*9.80665*s['twr']/count/(1.225*ct*s['diameter']**4));p['rotors']=[];bindings={}
  rotorids=['FL','FR','RR','RL']if count==4 else['R1','R2','R3','R4','R5','R6']
  for index,(hub,rotorid)in enumerate(zip(hubs,rotorids)):
   rotor=copy.deepcopy(template['rotors'][0]);position=transform(world[hub],[0,0,0])
   if key=='dji_m600_ul_ver':position=[(a+b)/2 for a,b in zip(*bounds[hub])]
   if key=='dji_mini_3':position[1]+=.011 # Approximate blade hub above stator, not a COM measurement.
   rotor.update(rotorId=rotorid,geometry=dict(positionLocalM=physical(position),thrustAxisLocal=[0,1,0],spinDirection='CW'if index%2==0 else'CCW'),propeller=dict(diameterM=s['diameter'],pitchM=s['pitch'],bladeCount=s['blades']),performance=dict(model='CtCq',ct=ct,cq=cq))
   electrical=rotor['motor']['electrical'];electrical.update(motorKvRpmPerVolt=round(maxrpm/(s['voltage']*.8),3),motorResistanceOhm=.025 if count==6 else .15,noLoadCurrentA=.2,maxCurrentA=35 if count==6 else 15,maxPowerW=700 if count==6 else 150,motorEfficiency=.85,escEfficiency=.95,escMaxCurrentA=40 if count==6 else 18)
   rotor['motor'].update(maxRpm=round(maxrpm,3),responseTimeUpS=.08,responseTimeDownS=.12);p['rotors'].append(rotor);bindings[rotorid]=[paths[i]for i in parts[index]]
  x,y,z=s['dims'];p['bodyAerodynamics']=dict(model='AxisApproximation',dragCd=[1,1.1,1],referenceAreaM2=[round(y*z*.35,6),round(x*z*.35,6),round(x*y*.35,6)])
  battery=p['powerSystem']['battery'];battery.update(mode='Simple',cellCount=s['cells'],nominalVoltageV=s['voltage'],capacityAh=s['capacity'],initialSoc=1,internalResistanceOhm=.018 if count==6 else .08,maxDischargeCurrentA=150 if count==6 else 45)
  battery['ocvCurve']=[dict(soc=0,voltageV=3*s['cells']),dict(soc=.2,voltageV=3.5*s['cells']),dict(soc=.8,voltageV=s['voltage']*1.03),dict(soc=1,voltageV=s['full'])]
  def provenance(path,kind,source,confidence):return dict(path=path,sourceType=kind,source=source,confidence=confidence)
  p['parameterProvenance']=[provenance('massProperties.massKg / massProperties.dimensionsM / powerSystem.battery.nominalVoltageV','Manufacturer',s['source']+' ; baseline configuration, published envelope in Unity [width,height,length].',.9),provenance('rotors.propeller','Manufacturer',PROP_SOURCE+' ; propeller diameter and pitch. Blade count follows supplied mesh or approximate visual.',.8),provenance('powerSystem.battery.capacityAh','Calculated'if count==6 else'Manufacturer',s['source']+(' ; 6 x 4.5 Ah = 27 Ah equivalent at 22.2 V; ideal common-bus approximation, no individual-pack redundancy.'if count==6 else' ; standard battery.'),.7 if count==6 else.9),provenance('rotors.geometry.positionLocalM / visual.rotorNodes / coordinateSystem.modelScaleMetersPerUnit','AutoGeometry','Supplied '+original.name+'; hierarchy/motor hub reference, centered AABB; Mavic scale 0.01 inferred from body dimensions, Mini/Avata already metres, M600 scale inferred from 1133 mm motor span. Check imported pose and physical pivots in Unity.',.5),provenance('massProperties.inertia / massProperties.centerOfMassLocalM','Estimated','AutoBox uses entire published envelope, not the actual mass distribution. COM at envelope origin. Requires weighing/inertia identification for accurate flight.',.2),provenance('rotors.motor / rotors.performance / rotors.geometry.thrustAxisLocal / rotors.geometry.spinDirection / bodyAerodynamics / powerSystem.battery.internalResistanceOhm / powerSystem.battery.maxDischargeCurrentA / powerSystem.battery.ocvCurve','Estimated','Ct=0.10; Cq=0.008 (two blades), 0.012 (three). RPM ceiling from estimated thrust-to-weight '+str(s['twr'])+' at reference density 1.225 kg/m3, not environment data. Motor/ESC efficiencies .85/.95; lag .08/.12 s, common +Y axes and alternating spin are assumptions. OCV interpolant/resistance/current limits not measured. No flight time or manufacturer controller calibration.',.15)]
  if derived:p['parameterProvenance'].append(provenance('visual.rotorNodes','Estimated','Separate Avata derivative adds simple 3032S-sized flat three-blade meshes; visual only, not manufacturer CAD.',.2))
  visual=dict(modelFile='model/'+filename,scale=s['scale'],rotationEulerDeg=[0,s['yaw'],0],centerModel=True,rotorNodes=bindings,previewYaw=35,previewPitch=25,previewDistance=max(s['dims'])*2.6,previewTarget=[0,0,0],previewOrthographic=False)
  doc=dict(id=uuid.uuid5(uuid.NAMESPACE_URL,'DroneLab approximate '+key).hex,category='Промышленные'if count==6 else'FPV'if derived else'Камерные',draft=True,profile=p,visual=visual,unfinishedInputs={})
  write_json(folder/'document.json',doc);write_json(folder/'profile.json',p);write_json(folder/'asset-bindings.json',visual)
  write_json(ROOT/'Assets/DronePhysics/Resources/DronePhysics'/(key+'_approx.json'),p)
  report.append(dict(model=key,massKg=s['mass'],rotors=count,sourceSha256=hashlib.sha256(original.read_bytes()).hexdigest(),modelSha256=hashlib.sha256((model/filename).read_bytes()).hexdigest(),visualDerivative=derived,bindings=bindings,visualSizeM=[round((b-a)*s['scale'],6)for a,b in zip(lo,hi)],publishedDimensionsM=s['dims'],sources=[s['source'],PROP_SOURCE]))
 write_json(output/'model-profile-audit.json',report)
 print(json.dumps(report,ensure_ascii=False,indent=2))

if __name__=='__main__':
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('input',type=pathlib.Path);parser.add_argument('output',type=pathlib.Path);a=parser.parse_args();build(a.input,a.output)
