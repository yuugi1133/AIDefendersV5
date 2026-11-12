from pathlib import Path
import numpy as np,json,math
P=Path(__file__).parent
colors={'Navy':'#24465e','Blue':'#396780','Edge':'#7396a3','Gold':'#f3b642','Dark':'#26333c','Steel':'#4e626c','Black':'#111f28','Glow':'#24eee0'}
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
# Four diagonal support feet, with gold tips and cyan bands.
def footblock(name,z0,z1,width,h,y,mat,angle):
 box(name,width,h,z0,z1,y,mat,'Base',.025)
 a=math.radians(angle);c=math.cos(a);ss=math.sin(a);r=np.array([[c,0,-ss],[0,1,0],[ss,0,c]])
 parts[-1]['v']=parts[-1]['v']@r
for i,angle in enumerate([45,135,225,315]):
 footblock('Outrigger'+str(i),.25,1.38,.29,.19,.115,'Navy',angle)
 footblock('FootTop'+str(i),.30,1.30,.24,.04,.23,'Blue',angle)
 footblock('CyanBand'+str(i),.78,.85,.295,.20,.12,'Glow',angle)
 footblock('GoldToe'+str(i),1.13,1.19,.295,.195,.12,'Gold',angle)
lathe('CentralBase',[(0,.40),(.12,.47),(.32,.47),(.43,.39)],'Navy')
lathe('HubGoldBand',[(.24,.475),(.30,.475)],'Gold',caps=False)
lathe('RaisedHub',[(.43,.39),(.54,.50),(.58,.45)],'Blue')
lathe('Turntable',[(.55,.49),(.64,.45)],'Dark','Yaw')
box('Support',.36,.46,-.18,.18,.84,'Dark','Yaw',.045)

box('Housing',1.04,1.10,-.76,.40,1.55,'Navy',bevel=.17)
p=parts.pop();tri=p['v'][p['f']];normal=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);normal/=np.linalg.norm(normal,axis=1)[:,None];mask=normal[:,1]>.5
mesh('UpperArmor',p['v'],p['f'][mask],'Blue','Pitch');mesh('Housing',p['v'],p['f'][~mask],'Navy','Pitch')
box('FrontRim',1.055,1.095,.40,.445,1.55,'Edge',bevel=.17)
box('GoldMantlet',.88,.91,.445,.55,1.55,'Gold',bevel=.14)
box('Breech',.64,.62,.55,.65,1.55,'Dark',bevel=.075)
box('BarrelCollar',.51,.49,.65,.71,1.55,'Steel','Recoil',bevel=.045)
# Six round tubes per pod with a polygonal front retaining collar.
def roundtube(name,x,y,z0,z1,outer,inner,mat):
 n=8;v=[]
 for z in [z0,z1]:
  for r in [outer,inner]:
   for i in range(n):
    a=2*math.pi*i/n;v.append([x+r*math.cos(a),y+r*math.sin(a),z])
 f=[]
 for i in range(n):
  j=(i+1)%n
  for aa,bb,cc,dd in [(i,j,j+16,i+16),(i+8,i+24,j+24,j+8),(i+16,j+16,j+24,i+24),(i,i+8,j+8,j)]:f.extend([[aa,bb,cc],[aa,cc,dd]])
 mesh(name,v,f,mat,'Recoil')
roundtube('MuzzleBrace',0,1.55,1.39,1.48,.29,.215,'Dark')
for i in range(6):
 a=2*math.pi*i/6;x=.162*math.cos(a);y=1.55+.162*math.sin(a)
 roundtube('GatlingTube'+str(i),x,y,.69,1.60,.058,.035,'Steel')
 roundtube('MuzzleRing'+str(i),x,y,1.58,1.65,.063,.035,'Edge')
 lathe('BoreShadow'+str(i),[(.90,.034),(.91,.034)],'Black','Recoil','z',(x,y,0),n=8)
# Large, simple upgrade markings on the top and side armor.
box('GoldTopFin',.14,.075,-.40,.17,2.115,'Gold','Pitch',.015)
# Keep the common height at 2.1 by seating armor slightly below the top edge.
parts[-1]['v'][:,1]-=.053
box('IndicatorHousing',.35,.16,.925,1.01,.38,'Dark','Base',.025)
box('Indicator',.25,.065,1.01,1.025,.40,'Glow','Base',.014)
for i in range(3):box('RearVent'+str(i),.58,.045,-.781,-.761,1.36+i*.13,'Dark',bevel=.014)
# Three pods share aiming, with independent recoil groups.
pods=[q for q in parts if q['group'] in ('Pitch','Recoil')]
parts=[q for q in parts if q['group'] not in ('Pitch','Recoil')]
muzzles=[]
for side,x in [('Left',-.72),('Center',0),('Right',.72)]:
 for q in pods:
  v=q['v'].copy();v[:,0]=v[:,0]*.62+x
  mesh(q['name']+side,v,q['f'].copy(),q['mat'],'Recoil'+side if q['group']=='Recoil' else 'Pitch')
 for i in range(6):
  a=2*math.pi*i/6;muzzles.append([x+.162*math.cos(a)*.62,1.55+.162*math.sin(a),1.67])
# Outer side armor plates, gold with blue edging.
for sign in [-1,1]:
 box('GoldSideArmor'+str(sign),.035,.43,-.65,.23,1.43,'Gold','Pitch',.012);parts[-1]['v'][:,0]+=sign*1.055
# Old pedestal indicator is omitted; the foot bands are the stage-3 lights.
parts=[q for q in parts if q['name'] not in ('Indicator','IndicatorHousing')]
# Seat pod housings lower so the three gold top fins stand above their roofs.
for q in parts:
 if q['group'] in ('Pitch','RecoilLeft','RecoilCenter','RecoilRight') and not q['name'].startswith('GoldTopFin'):
  q['v'][:,1]-=.08
for point in muzzles:point[1]-=.08
# OBJ is world-space, named parts; Unity builder below supplies movable hierarchy.
lines=['mtllib MachineGunLevel3.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/MachineGunLevel3.obj').write_text('\n'.join(lines))
(P/'Models/MachineGunLevel3.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKa 0.1 0.1 0.1\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]; pivot=np.array([0,1.55,0]) if p['group'] in ('Pitch','RecoilLeft','RecoilCenter','RecoilRight') else np.zeros(3)
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v-pivot],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersMachineGunLevel3/Editor/MachineGunLevel3MeshData.json').write_text(json.dumps(data))

for p in parts:
 tri=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
print('Parts:',len(parts),'Triangles:',sum(len(p['f']) for p in parts))

v=np.concatenate([p['v'] for p in parts]);assert len({p['name'] for p in parts})==len(parts)
(P/'ModelInfo.json').write_text(json.dumps({'triangles':sum(len(p['f']) for p in parts),'parts':len(parts),'height':float(v[:,1].max()),'bounds':[v.min(0).tolist(),v.max(0).tolist()],'muzzles':muzzles,'pitchPivot':[0,1.55,0]},indent=2))
