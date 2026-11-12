from pathlib import Path
import numpy as np,json,math
P=Path(__file__).parent
colors={'Sand':'#c5b98d','LightSand':'#ded3ab','ShadeSand':'#9c906b','Dark':'#303333','Steel':'#67695e','Black':'#1e2426','Glass':'#263d48','Lamp':'#ece3c4','Amber':'#d18c36','Red':'#a04734','Mark':'#e6dfc2'}
parts=[]
def mesh(name,v,f,mat,group):
 v=np.array(v,float); f=np.array(f,int)
 # Each triangle has independent vertices for deliberately flat normals.
 parts.append(dict(name=name,v=v,f=f,mat=mat,group=group))
def lathe(name,rings,mat,group='Base',axis='y',center=(0,0,0),n=12,caps=True):
 v=[]
 for h,r in rings:
  for i in range(n):
   a=2*math.pi*i/n+math.pi/n
   q=[r*math.cos(a),h,r*math.sin(a)] if axis=='y' else [r*math.cos(a),r*math.sin(a),h]
   v.append(np.array(q)+center)
 f=[]
 for j in range(len(rings)-1):
  for i in range(n):
   a=j*n+i;b=j*n+(i+1)%n;c=b+n;d=a+n
   f.extend([[a,b,c],[a,c,d]] if axis=='z' else [[a,c,b],[a,d,c]])
 if caps:
  for j,rev in [(0,False),(len(rings)-1,True)]:
   k=len(v);q=[0,rings[j][0],0] if axis=='y' else [0,0,rings[j][0]];v.append(np.array(q)+center)
   for i in range(n):
    a=j*n+i;b=j*n+(i+1)%n
    f.append([k,b,a] if rev==(axis=='y') else [k,a,b])
 mesh(name,v,f,mat,group)
def box(name,w,h,z0,z1,y,mat,group='Pitch',bevel=.12):
 x=w/2;t=h/2;b=bevel
 poly=[(-x+b,-t),(x-b,-t),(x,-t+b),(x,t-b),(x-b,t),(-x+b,t),(-x,t-b),(-x,-t+b)]
 v=[[a,y+c,z] for z in (z0,z1) for a,c in poly];f=[]
 for i in range(8):
  j=(i+1)%8;f.extend([[i,j,j+8],[i,j+8,i+8]])
 for i in range(1,7):f.extend([[0,i+1,i],[8,8+i,8+i+1]])
 mesh(name,v,f,mat,group)
# Coordinates: X right, Y up, +Z forward. Compact game scale, not engineering dimensions.
def block(name,pos,size,mat,group='Hull',bevel=.03):
 x,y,z=pos;w,h,d=size
 box(name,w,h,z-d/2,z+d/2,y,mat,group,min(bevel,w*.2,h*.2));parts[-1]['v'][:,0]+=x

def deck(name,rings,mat,group):
 v=[]
 for y,w,d,z in rings:
  b=.17; x=w/2;t=d/2
  poly=[(-x+b,-t),(x-b,-t),(x,-t+b),(x,t-b),(x-b,t),(-x+b,t),(-x,t-b),(-x,-t+b)]
  v.extend([[a,y,c+z] for a,c in poly])
 f=[]
 for j in range(len(rings)-1):
  for i in range(8):
   a=j*8+i;b=j*8+(i+1)%8;c=b+8;d=a+8;f.extend([[a,c,b],[a,d,c]])
 for i in range(1,7):f.extend([[0,i,i+1],[8*(len(rings)-1),8*(len(rings)-1)+i+1,8*(len(rings)-1)+i]])
 mesh(name,v,f,mat,group)

def wheel(name,x,y,z,r,width,mat,group,n=12):
 lathe(name,[(-width/2,r),(width/2,r)],mat,group,'z',n=n)
 p=parts[-1];q=p['v'].copy();p['v']=np.column_stack([q[:,2]+x,q[:,0]+y,q[:,1]+z])

# Sand-colored four-door utility jeep with dark soft roof.
pivots={'Body':[0,0,0]}
def beam(name,a,b,width,mat,group='Body'):
 a=np.array(a,float);b=np.array(b,float);d=b-a;L=np.linalg.norm(d);z=d/L
 helper=np.array([0,1,0]) if abs(z[1])<.9 else np.array([1,0,0]);x=np.cross(helper,z);x/=np.linalg.norm(x);y=np.cross(z,x)
 block(name,(0,0,0),(width,width,L),mat,group,.005);parts[-1]['v']=parts[-1]['v']@np.array([x,y,z])+(a+b)/2
# Four independent wheel assemblies. Cylindrical mesh axis is X.
for side,sign in [('L',-1),('R',1)]:
 for end,zz in [('F',1.35),('R',-1.35)]:
  group='Wheel'+end+side;x=sign*1.10;pivots[group]=[x,.51,zz]
  lathe('Tyre'+group,[(-.19,.37),(-.155,.51/math.cos(math.pi/20)),(.155,.51/math.cos(math.pi/20)),(.19,.37)],'Black',group,'z',n=20)
  q=parts[-1]['v'].copy();parts[-1]['v']=np.column_stack([q[:,2]+x,q[:,0]+.51,q[:,1]+zz])
  wheel('Rim'+group,x+sign*.19,.51,zz,.305,.035,'ShadeSand',group,16)
  wheel('Hub'+group,x+sign*.218,.51,zz,.115,.045,'Steel',group,12)
  for j in range(8):
   a=j*math.pi/4
   wheel('RimInset'+group+str(j),x+sign*.213,.51+.215*math.cos(a),zz+.215*math.sin(a),.045,.008,'Dark',group,8)
  for j in range(5):
   a=j*2*math.pi/5;wheel('Lug'+group+str(j),x+sign*.245,.51+.074*math.cos(a),zz+.074*math.sin(a),.019,.015,'LightSand',group,6)
  # Low-profile tire tread blocks, kept within the rolling radius at the bottom.
  for j in range(20):
   a=j*2*math.pi/20+math.pi/20
   block('Tread'+group+str(j),(0,0,0),(.28,.023,.10),'Dark',group,.005)
   q=parts[-1]['v'].copy();co=math.cos(a);si=math.sin(a)
   parts[-1]['v']=np.column_stack([q[:,0]+x,q[:,1]*co-q[:,2]*si+.51+.49*co,q[:,1]*si+q[:,2]*co+zz+.49*si])

block('Chassis',(0,.68,0),(1.6,.25,3.8),'Dark','Body')
# Boat-shaped lower armor and sharply tapered upper armor.
deck('LowerHull',[(.80,1.65,3.7,0),(1.27,2.25,4.25,0),(1.46,2.25,4.25,0)],'ShadeSand','Body')
deck('UpperHull',[(1.46,2.25,4.25,0),(2.04,1.67,3.22,-.28)],'Sand','Body')
deck('RoofPlate',[(2.04,1.67,3.22,-.28),(2.08,1.61,3.15,-.28)],'LightSand','Body')
# Front glazing follows sloped front armor: z=2.125-(y-1.46)*.795/.58.
def windshield(name,x):
 block(name,(x,0,0),(.60,.34,.024),'Glass','Body',.025)
 q=parts[-1]['v'];y=q[:,1]+1.79
 q[:,2]+=2.125-(y-1.46)*.795/.58+.018;q[:,1]=y
for sign in [-1,1]:
 windshield('FrontGlass'+str(sign),sign*.36)
 # Side viewports follow the inward-sloping sides.
 for j,z in enumerate([.9,-.32,-1.13]):
  block('SideViewport'+str(sign)+str(j),(0,1.80,z),(.022,.18,.42),'Glass','Body',.012)
  q=parts[-1]['v'];q[:,0]+=sign*(1.125-(q[:,1]-1.46)*.29/.58+.012)
 block('SideDoor'+str(sign),(sign*1.13,1.20,-.18),(.035,.47,.69),'Sand','Body',.04)
 beam('DoorHandle'+str(sign),(sign*1.16,1.42,-.41),(sign*1.16,1.42,-.18),.035,'Steel')
 beam('Step'+str(sign),(sign*1.14,.80,-.65),(sign*1.14,.80,.65),.065,'Steel')
 for z in [-.43,.08]:block('DoorHinge'+str(sign)+str(z),(sign*1.165,1.28,z),(.025,.11,.055),'LightSand','Body',.008)
 # Headlights with protective cages.
 lathe('LampRim'+str(sign),[(1.91,.15),(1.97,.15)],'Steel','Body','z',(sign*.82,1.57,0),12)
 lathe('LampLens'+str(sign),[(1.971,.115),(1.982,.115)],'Lamp','Body','z',(sign*.82,1.57,0),12)
 for dx in [-.18,.18]:beam('LampGuardU'+str(sign)+str(dx),(sign*.82+dx,1.40,2.00),(sign*.82+dx,1.78,2.00),.025,'Steel')
 for y in [1.40,1.78]:beam('LampGuardH'+str(sign)+str(y),(sign*.82-.18,y,2.0),(sign*.82+.18,y,2.0),.025,'Steel')
 beam('MirrorStem'+str(sign),(sign*.97,1.65,1.25),(sign*1.21,2.14,1.26),.025,'Steel')
 lathe('Mirror'+str(sign),[(1.24,.115),(1.29,.115)],'ShadeSand','Body','z',(sign*1.21,2.14,0),10)
 block('TailLight'+str(sign),(sign*.83,1.4,-2.13),(.16,.16,.03),'Red','Body',.02)
beam('FrontGuard',(-1.03,1.24,2.22),(1.03,1.24,2.22),.07,'Steel')
for sign in [-1,1]:beam('FrontGuardWing'+str(sign),(sign*1.03,1.24,2.22),(sign*.85,1.64,1.98),.07,'Steel')
# Roof hatches, engine louvers, antenna.
lathe('RoofHatch',[(2.08,.31),(2.13,.31)],'ShadeSand','Body','y',(-.42,0,.70),16)
beam('HatchHandle',(-.55,2.17,.70),(-.30,2.17,.70),.035,'Steel')
for j in range(6):block('EngineVent'+str(j),(0,2.087,-1.16+j*.09),(.90,.025,.035),'Dark','Body',.006)
beam('Antenna',( -.68,2.07,-1.45),(-.68,2.88,-1.45),.014,'Dark')
lathe('AntennaFoot',[(2.07,.05),(2.16,.05)],'Steel','Body','y',(-.68,0,-1.45),8)
# Independent turret yaw and gun pitch; no firing simulation.
pivots['Yaw']=[0,2.08,.03];pivots['Pitch']=[0,2.40,.42]
lathe('TurretBearing',[(2.08,.58),(2.15,.58)],'Steel','Yaw','y',(0,0,.03),12)
deck('TurretArmor',[(2.15,1.19,1.10,.03),(2.65,.80,.75,-.09)],'Sand','Yaw')
lathe('TurretHatch',[(2.65,.28),(2.70,.28)],'LightSand','Yaw','y',(0,0,-.12),12)
beam('TurretHatchHandle',(-.13,2.74,-.12),(.13,2.74,-.12),.03,'Steel','Yaw')
block('GunMantlet',(0,2.40,.48),(.43,.28,.19),'ShadeSand','Pitch',.045)
lathe('GunBarrel',[(.51,.085),(.66,.085),(.68,.055),(1.14,.055),(1.17,.07),(1.25,.07)],'Steel','Pitch','z',(0,2.40,0),12)
lathe('MuzzleBore',[(1.251,.046),(1.254,.046)],'Black','Pitch','z',(0,2.40,0),12)
# Export named OBJ objects and per-group local vertices.
lines=['mtllib ArmoredCar.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/ArmoredCar.obj').write_text('\n'.join(lines))
(P/'Models/ArmoredCar.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]-np.array(pivots[p['group']])
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],material=p['mat'],emissive=False,vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersArmoredCar/Editor/ArmoredCarMeshData.json').write_text(json.dumps(data))
for p in parts:
 tri=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
v=np.concatenate([p['v'] for p in parts]);print('Parts:',len(parts),'Triangles:',sum(len(p['f']) for p in parts),'Bounds:',v.min(0),v.max(0))

(P/'ModelInfo.json').write_text(json.dumps({'pivots':pivots,'parts':len(parts),'triangles':sum(len(p['f']) for p in parts),'bounds':[v.min(0).tolist(),v.max(0).tolist()]},indent=2))
