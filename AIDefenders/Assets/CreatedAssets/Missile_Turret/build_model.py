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
lathe('Foundation',[(0,1.03),(.09,1.12),(.21,1.12),(.30,1.01)],'Navy')
lathe('GoldBelt',[(.14,1.126),(.22,1.126),(.30,1.045)],'Gold',caps=False)
lathe('UpperPlinth',[(.30,1.01),(.49,.91),(.55,.82)],'Blue')
lathe('TopRim',[(.52,.85),(.56,.81),(.57,.73)],'Edge',caps=False)
lathe('Turntable',[(.55,.49),(.64,.45)],'Dark','Yaw')
box('Support',.36,.46,-.18,.18,.84,'Dark','Yaw',.045)

def shell(name,w,h,depth0,depth1,thickness,mat):
 def profile(w,h,b):
  x=w/2;t=h/2
  return [(-x+b,-t),(x-b,-t),(x,-t+b),(x,t-b),(x-b,t),(-x+b,t),(-x,t-b),(-x,-t+b)]
 outer=profile(w,h,.08);inner=profile(w-2*thickness,h-2*thickness,.045)
 v=[[a,1.55+b,z] for z in (depth0,depth1) for ring in (outer,inner) for a,b in ring];f=[]
 for i in range(8):
  j=(i+1)%8
  for a,b,c,d in [(i,j,j+16,i+16),(i+8,i+24,j+24,j+8),(i+16,j+16,j+24,i+24),(i,i+8,j+8,j)]:f.extend([[a,b,c],[a,c,d]])
 if name=='LauncherHousing':
  top=[];other=[]
  for face in f:
   tri=np.array(v)[face];normal=np.cross(tri[1]-tri[0],tri[2]-tri[0]);normal/=np.linalg.norm(normal)
   (top if normal[1]>.5 else other).append(face)
  mesh('TopArmor',v,top,'Blue','Pitch');mesh(name,v,other,mat,'Pitch')
 else:mesh(name,v,f,mat,'Pitch')
shell('LauncherHousing',1.10,1.10,-.68,.43,.12,'Navy')
box('RearArmor',1.095,1.095,-.73,-.67,1.55,'Navy',bevel=.08)
box('InteriorBack',.85,.85,-.665,-.65,1.55,'Black',bevel=.045)
shell('BlueFrontRim',1.12,1.10,.43,.49,.105,'Edge')
box('LaunchRail',.12,.08,-.60,.48,1.22,'Steel',bevel=.015)
lathe('MissileBody',[(-.48,.155),(.43,.155),(.51,.17)],'Steel','Missile','z',(0,1.55,0),10)
lathe('Warhead',[(.49,.17),(.62,.175),(.78,.12),(.91,.035),(.94,.005)],'Gold','Missile','z',(0,1.55,0),10)
lathe('RocketNozzle',[(-.57,.10),(-.49,.13)],'Dark','Missile','z',(0,1.55,0),10)
# Side insignia: a raised hollow hexagonal badge on each armor side.
for sign in [-1,1]:
 v=[]
 for x in [sign*.552,sign*.585]:
  for r in [.235,.165]:
   for i in range(6):
    a=2*math.pi*i/6+math.pi/6;v.append([x,1.59+r*math.sin(a),-.10+r*math.cos(a)])
 f=[]
 for i in range(6):
  j=(i+1)%6
  for a,b,c,d in [(i,j,j+12,i+12),(i+6,i+18,j+18,j+6),(i+12,j+12,j+18,i+18),(i,i+6,j+6,j)]:f.extend([[a,b,c],[a,c,d]])
 if sign<0:f=[q[::-1] for q in f]
 mesh('SideBadge'+('R' if sign>0 else 'L'),v,f,'Gold','Pitch')
box('IndicatorHousing',.35,.16,.925,1.01,.38,'Dark','Base',.025)
box('Indicator',.25,.065,1.01,1.025,.40,'Glow','Base',.014)
for i in range(3):box('RearVent'+str(i),.55,.045,-.749,-.731,1.36+i*.13,'Dark',bevel=.014)
# OBJ is world-space, named parts; Unity builder below supplies movable hierarchy.
lines=['mtllib MissileTurret.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/MissileTurret.obj').write_text('\n'.join(lines))
(P/'Models/MissileTurret.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKa 0.1 0.1 0.1\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]; pivot=np.array([0,1.55,0]) if p['group'] in ('Pitch','Missile') else np.zeros(3)
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v-pivot],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersMissileTurret/Editor/MissileTurretMeshData.json').write_text(json.dumps(data))

for p in parts:
 tri=p['v'][p['f']]; assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
vertices=np.concatenate([p['v'] for p in parts]);print('Parts',len(parts),'Triangles',sum(len(p['f']) for p in parts),'Bounds',vertices.min(0),vertices.max(0))
