from pathlib import Path
import numpy as np,json,math
P=Path(__file__).parent
colors={'Sand':'#b4a77b','LightSand':'#d0c39a','ShadeSand':'#8c805c','Dark':'#343737','Steel':'#55564d','Black':'#1e2527','Glass':'#406875','Lamp':'#eadcb1'}
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

# Track rings with open centers, and independently named road wheels.
for side,sign in [('L',-1),('R',1)]:
 group='Track'+side;x=sign*1.04;n=24;v=[]
 for xx in [x-.24,x+.24]:
  for r in [.46,.35]:
   for i in range(n):
    a=2*math.pi*i/n;v.append([xx,.49+r*math.cos(a),1.40*(1 if math.sin(a)>=0 else -1)+r*math.sin(a)])
 f=[]
 for i in range(n):
  j=(i+1)%n
  for a,b,c,d in [(i,j,j+2*n,i+2*n),(i+n,i+3*n,j+3*n,j+n),(i+2*n,j+2*n,j+3*n,i+3*n),(i,i+n,j+n,j)]:f.extend([[a,b,c],[a,c,d]])
 mesh('TrackBelt'+side,v,f,'Dark',group)
 for j,z in enumerate(np.linspace(-1.36,1.36,6)):
  wheel('RoadWheel'+side+str(j),x,.47,z,.335,.37,'Steel',group)
  wheel('Hub'+side+str(j),x+sign*.205,.47,z,.155,.045,'ShadeSand',group)
 # Individual cleats along both straight runs.
 for j,z in enumerate(np.linspace(-1.38,1.38,19)):
  for y,tag in [(0.025,'Lower'),(.955,'Upper')]:block('Tread'+side+tag+str(j),(x,y,z),(.50,.05,.11),'Steel',group,.008)
 # Cleats wrapping around each end.
 for end in [-1,1]:
  for j,a in enumerate(np.linspace(-math.pi/2,math.pi/2,9)):
   y=.49+.465*math.sin(a);z=end*(1.40+.465*math.cos(a))
   block('EndTread'+side+str(end)+'_'+str(j),(0,0,0),(.50,.05,.13),'Steel',group,.008)
   p=parts[-1];angle=-end*(math.pi/2-a);co=math.cos(angle);si=math.sin(angle);q=p['v'].copy();p['v']=np.column_stack([q[:,0]+x,q[:,1]*co-q[:,2]*si+y,q[:,1]*si+q[:,2]*co+z])

deck('LowerHull',[(.40,1.50,3.18,0),(.90,1.83,3.72,0)],'ShadeSand','Hull')
deck('UpperHull',[(.83,2.38,3.94,0),(1.20,2.12,3.40,-.10)],'Sand','Hull')
# Segmented side armor skirts, leaving lower running gear visible.
for side,sign in [('L',-1),('R',1)]:
 for j,z in enumerate(np.linspace(-1.48,1.48,6)):
  block('Skirt'+side+str(j),(sign*1.235,.88,z),(.14,.53,.475),'LightSand' if j%3 else 'Sand')
  for y in [.69,1.07]:
   block('SkirtFastener'+side+str(j)+str(y),(sign*1.31,y,z+.15),(.025,.04,.04),'Steel',bevel=.005)
 block('Fender'+side,(sign*1.11,1.16,-.03),(.40,.06,3.74),'LightSand')
 # Front headlight and armored backing.
 block('LightHousing'+side,(sign*.87,1.22,1.55),(.29,.18,.16),'Dark')
 block('Headlamp'+side,(sign*.87,1.22,1.638),(.20,.10,.018),'Lamp',bevel=.01)
 block('RearLight'+side,(sign*.96,1.02,-1.85),(.14,.07,.025),'ShadeSand',bevel=.01)
block('FrontArmor',(0,.99,1.71),(1.35,.23,.23),'LightSand')
block('DriverHatch',(-.46,1.225,1.08),(.57,.06,.48),'LightSand')
block('DriverScope',(-.46,1.29,.96),(.28,.08,.10),'Glass',bevel=.01)
for j in range(9):block('EngineGrille'+str(j),(0,1.21,-1.20+j*.07),(1.12,.025,.035),'Dark',bevel=.005)
lathe('TurretRing',[(1.19,.76),(1.29,.76)],'Dark','Yaw',n=16)
deck('TurretArmor',[(1.29,1.75,1.70,-.22),(1.72,1.88,1.72,-.24),(1.87,1.49,1.42,-.24)],'Sand','Yaw')
block('TurretRoof',(0,1.88,-.24),(1.28,.035,1.10),'LightSand','Yaw')
block('Mantlet',(0,1.57,.66),(.66,.48,.24),'ShadeSand','Pitch',.07)
lathe('GunRoot',[(.74,.18),(.93,.18)],'Steel','Recoil','z',(0,1.57,0),12)
lathe('GunBarrel',[(.88,.13),(2.90,.105),(3.23,.105),(3.23,.067),(2.99,.067)],'LightSand','Recoil','z',(0,1.57,0),12,False)
lathe('BoreShadow',[(2.98,.066),(2.99,.066)],'Black','Recoil','z',(0,1.57,0),12)
lathe('FumeExtractor',[(1.22,.13),(1.30,.205),(1.63,.205),(1.72,.13)],'Sand','Recoil','z',(0,1.57,0),12)
for j,z in enumerate([.98,2.72,3.13]):lathe('BarrelBand'+str(j),[(z,.118 if z>2 else .145),(z+.055,.118 if z>2 else .145)],'ShadeSand','Recoil','z',(0,1.57,0),12)
lathe('CommanderHatch',[(1.90,.28),(1.98,.28)],'ShadeSand','Yaw',center=(-.39,0,-.28),n=12)
lathe('HatchLid',[(1.98,.25),(2.015,.25)],'LightSand','Yaw',center=(-.39,0,-.28),n=12)
block('OpticsHousing',(.41,1.99,.06),(.30,.22,.34),'ShadeSand','Yaw')
block('OpticsGlass',(.41,1.99,.237),(.22,.11,.018),'Glass','Yaw',.01)
# Rear storage, railings and communications antenna.
block('RearStorage',(0,1.64,-1.23),(1.37,.36,.36),'ShadeSand','Yaw')
for side,sign in [('L',-1),('R',1)]:
 for j,z in enumerate([-.96,-1.40]):block('RackPost'+side+str(j),(sign*.78,1.71,z),(.035,.42,.035),'Steel','Yaw',.005)
 block('RackRail'+side,(sign*.78,1.89,-1.18),(.035,.035,.48),'Steel','Yaw',.005)
block('RearRackRail',(0,1.89,-1.40),(1.59,.035,.035),'Steel','Yaw',.005)
lathe('AntennaBase',[(1.89,.065),(1.98,.065)],'Dark','Yaw',center=(.53,0,-.65),n=8)
lathe('Antenna',[(1.97,.014),(2.43,.009)],'Steel','Yaw',center=(.53,0,-.65),n=6)
# Decorative short smoke-launcher clusters, no behavior attached.
for side,sign in [('L',-1),('R',1)]:
 for j in range(3):
  lathe('SmokeTube'+side+str(j),[(0,.058),(.20,.058)],'Dark','Yaw','z',n=8)
  p=parts[-1];q=p['v'].copy();angle=-.52;co=math.cos(angle);si=math.sin(angle)
  p['v']=np.column_stack([q[:,0]+sign*(.64+j*.095),q[:,1]*co-q[:,2]*si+1.65,q[:,1]*si+q[:,2]*co+.54])
# Export static OBJ, plus local vertices for the Unity pivot hierarchy.
lines=['mtllib Tank.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/Tank.obj').write_text('\n'.join(lines))
(P/'Models/Tank.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 pivot=np.array([0,1.57,.66]) if p['group'] in ('Pitch','Recoil') else np.array([0,1.25,-.22]) if p['group']=='Yaw' else np.zeros(3)
 v=p['v'][p['f'].flatten()]-pivot
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersTank/Editor/TankMeshData.json').write_text(json.dumps(data))
for p in parts:
 tri=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
v=np.concatenate([p['v'] for p in parts]);print('Parts:',len(parts),'Triangles:',sum(len(p['f']) for p in parts),'Bounds:',v.min(0),v.max(0))
