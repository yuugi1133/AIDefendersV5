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
# Chassis and axles.
block('Chassis',(0,.62,0),(1.45,.20,3.50),'Dark','Body')
for zz in [-1.20,1.20]:beam('Axle'+str(zz),(-1,.51,zz),(1,.51,zz),.13,'Steel')
# Four independent wheel assemblies. Cylindrical mesh axis is X.
for side,sign in [('L',-1),('R',1)]:
 for end,zz in [('F',1.20),('R',-1.20)]:
  group='Wheel'+end+side;x=sign*.98;pivots[group]=[x,.51,zz]
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
# Body panels: the lower side sections avoid the wheel openings.
block('UpperBody',(0,1.285,-.05),(1.81,.33,3.49),'Sand','Body',.045)
block('CabinFloor',(0,.93,-.23),(1.62,.18,2.70),'ShadeSand','Body')
block('Hood',(0,1.465,1.12),(1.75,.09,1.29),'LightSand','Body',.035)
block('Cowl',(0,1.50,.42),(1.78,.14,.18),'Sand','Body')
for side,sign in [('L',-1),('R',1)]:
 for name,z,d in [('Center',0,1.16),('Front',1.77,.18),('Rear',-1.77,.18)]:block('LowerSide'+side+name,(sign*.87,.99,z),(.11,.30,d),'Sand','Body',.02)
 block('RunningBoard'+side,(sign*.98,.83,-.03),(.21,.075,1.28),'ShadeSand','Body')
 # Upper wheel arches form a continuous half-ring in the YZ plane.
 for end,zz in [('F',1.20),('R',-1.20)]:
  v=[];N=13
  for xx in [sign*.90,sign*1.15]:
   for r in [.59,.53]:
    for a in np.linspace(0,math.pi,N):v.append([xx,.51+r*math.sin(a),zz+r*math.cos(a)])
  f=[]
  for i in range(N-1):
   j=i+1
   for a,b,c,d in [(i,j,j+2*N,i+2*N),(i+N,i+3*N,j+3*N,j+N),(i+2*N,j+2*N,j+3*N,i+3*N),(i,i+N,j+N,j)]:f.extend([[a,b,c],[a,c,d]])
  for i in [0,N-1]:
   a,b,c,d=i,i+2*N,i+3*N,i+N;f.extend([[a,b,c],[a,c,d]] if i==0 else [[a,c,b],[a,d,c]])
  if sign>0:f=[q[::-1] for q in f]
  mesh('Fender'+side+end,v,f,'Dark','Body')
# Cabin with a sloped windshield and flat side windows.
# Glass is opaque blue-black for simple, dependable Unity materials.
block('CabinCore',(0,1.76,-.65),(1.61,.66,1.93),'Glass','Body',.035)
block('SoftRoof',(0,2.145,-.56),(1.85,.16,2.50),'Dark','Body',.065)
# Front windshield trapezoid, thickness represented by a small closed prism.
v=[[-.82,1.55,.48],[.82,1.55,.48],[.82,2.07,.21],[-.82,2.07,.21],[-.82,1.55,.45],[.82,1.55,.45],[.82,2.07,.18],[-.82,2.07,.18]]
f=[[0,1,2],[0,2,3],[4,6,5],[4,7,6],[0,4,5],[0,5,1],[3,2,6],[3,6,7],[0,3,7],[0,7,4],[1,5,6],[1,6,2]]
mesh('Windshield',v,f,'Glass','Body')
for sign in [-1,1]:beam('APillar'+str(sign),(sign*.86,1.48,.52),(sign*.86,2.095,.20),.09,'Sand')
beam('WindscreenTop',(-.87,2.09,.20),(.87,2.09,.20),.09,'Sand')
beam('WindscreenBottom',(-.87,1.54,.49),(.87,1.54,.49),.08,'Sand')
beam('WindshieldDivider',(0,1.56,.49),(0,2.06,.23),.035,'Dark')
for side,sign in [('L',-1),('R',1)]:
 # Doors and side glazing outlines.
 for j,(z,d) in enumerate([(-.03,.73),(-.84,.75)]):
  block('Door'+side+str(j),(sign*.914,1.19,z),(.025,.47,d),'LightSand','Body',.02)
  block('SideWindow'+side+str(j),(sign*.844,1.79,z),(.024,.47,d-.06),'Glass','Body',.015)
  block('WindowSill'+side+str(j),(sign*.872,1.535,z),(.08,.055,d),'Sand','Body')
  block('Handle'+side+str(j),(sign*.94,1.44,z-.23),(.04,.045,.14),'Dark','Body',.008)
  for y in [1.04,1.35]:block('Hinge'+side+str(j)+str(y),(sign*.94,y,z+d/2-.045),(.04,.065,.05),'ShadeSand','Body',.008)
 for j,z in enumerate([-.435,-1.26]):block('Pillar'+side+str(j),(sign*.86,1.81,z),(.09,.59,.075),'Sand','Body')
 block('RearCanvasSide'+side,(sign*.86,1.83,-1.52),(.09,.56,.47),'Dark','Body')
 block('RearQuarterWindow'+side,(sign*.91,1.84,-1.52),(.015,.37,.30),'Glass','Body',.025)
 beam('CanvasBrace'+side,(sign*.927,1.67,-1.69),(sign*.927,2.01,-1.35),.045,'Dark')
 beam('MirrorArm'+side,(sign*.89,1.53,.43),(sign*1.06,1.72,.40),.045,'Dark')
 block('Mirror'+side,(sign*1.10,1.77,.40),(.13,.29,.17),'Dark','Body')
 # Small pale five-point star badge on the front door.
 center=np.array([sign*.931,1.20,-.04]);v=[center.tolist()]
 for j in range(10):
  a=math.pi/2+j*math.pi/5;r=.15 if j%2==0 else .066;v.append((center+[0,r*math.sin(a),r*math.cos(a)]).tolist())
 f=[[0,j+1,(j+1)%10+1] for j in range(10)]
 if sign>0:f=[q[::-1] for q in f]
 mesh('StarBadge'+side,v,f,'Mark','Body')
# Front grille, lights, bumper, towing eyes and wipers.
block('GrilleSurround',(0,1.235,1.73),(1.72,.53,.09),'Sand','Body')
block('Radiator',(0,1.245,1.784),(1.04,.35,.025),'Black','Body',.015)
for j in range(4):block('GrilleBar'+str(j),(0,1.105+j*.095,1.805),(1.03,.035,.035),'LightSand','Body',.005)
for sign in [-1,1]:
 lathe('HeadlightBezel'+str(sign),[(1.775,.172),(1.82,.172)],'Steel','Body','z',(sign*.68,1.29,0),16)
 lathe('Headlight'+str(sign),[(1.821,.135),(1.831,.135)],'Lamp','Body','z',(sign*.68,1.29,0),16)
 block('Indicator'+str(sign),(sign*.68,1.04,1.802),(.21,.10,.035),'Amber','Body',.012)
 block('HoodLatch'+str(sign),(sign*.86,1.43,1.36),(.035,.085,.09),'Dark','Body',.005)
 beam('Wiper'+str(sign),(sign*.46,1.57,.51),(sign*.29,1.83,.39),.024,'Dark')
block('FrontBumper',(0,.83,1.91),(2.13,.24,.22),'Dark','Body')
block('RearBumper',(0,.80,-1.91),(2.02,.18,.20),'Dark','Body')
for sign in [-1,1]:
 block('TowMount'+str(sign),(sign*.64,.79,2.04),(.12,.12,.055),'Steel','Body',.02)
 block('TailLamp'+str(sign),(sign*.74,1.23,-1.815),(.17,.21,.035),'Red','Body',.02)
block('Tailgate',(0,1.25,-1.79),(1.30,.44,.06),'Sand','Body')
block('RearWindow',(0,1.84,-1.79),(1.36,.43,.035),'Glass','Body')
# Rear spare tire, fixed to body.
lathe('SpareTyre',[(-2.12,.34),(-2.08,.47),(-1.86,.47),(-1.82,.34)],'Black','Body','z',(0,1.27,0),20)
lathe('SpareRim',[(-2.13,.275),(-2.115,.275)],'ShadeSand','Body','z',(0,1.27,0),16)
lathe('SpareHub',[(-2.15,.10),(-2.13,.10)],'Steel','Body','z',(0,1.27,0),12)
# Export named OBJ objects and per-group local vertices.
lines=['mtllib Jeep.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/Jeep.obj').write_text('\n'.join(lines))
(P/'Models/Jeep.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]-np.array(pivots[p['group']])
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersJeep/Editor/JeepMeshData.json').write_text(json.dumps(data))
for p in parts:
 tri=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
v=np.concatenate([p['v'] for p in parts]);print('Parts:',len(parts),'Triangles:',sum(len(p['f']) for p in parts),'Bounds:',v.min(0),v.max(0))
