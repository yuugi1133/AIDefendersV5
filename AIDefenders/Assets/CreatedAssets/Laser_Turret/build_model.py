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

box('Housing',1.10,1.10,-.65,.42,1.55,'Navy',bevel=.18)
# Give the upper faces blue armor without overlapping surfaces.
p=parts.pop();tri=p['v'][p['f']];normal=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);normal/=np.linalg.norm(normal,axis=1)[:,None]
mask=normal[:,1]>.5
mesh('UpperArmor',p['v'],p['f'][mask],'Blue','Pitch')
mesh('Housing',p['v'],p['f'][~mask],'Navy','Pitch')
box('FrontRim',1.115,1.095,.42,.47,1.55,'Edge',bevel=.18)
box('GoldFacePlate',.985,.98,.47,.53,1.55,'Gold',bevel=.17)
lathe('EmitterSocket',[(.53,.29),(.64,.29),(.70,.225)],'Dark','Pitch','z',(0,1.55,0),8)
lathe('EmitterCollar',[(.70,.215),(.75,.215),(.78,.17)],'Steel','Pitch','z',(0,1.55,0),8)
lathe('EnergyRod',[(.76,.14),(1.34,.14)],'Glow','Pitch','z',(0,1.55,0),8)
lathe('MuzzleCage',[(1.27,.21),(1.36,.235),(1.51,.235),(1.57,.18),(1.57,.12),(1.27,.12),(1.27,.21)],'Navy','Pitch','z',(0,1.55,0),8,False)
lathe('MuzzleRim',[(1.512,.238),(1.575,.183),(1.575,.121),(1.54,.1195)],'Edge','Pitch','z',(0,1.55,0),8,False)
lathe('LaserLens',[(1.505,.119),(1.515,.119)],'Glow','Pitch','z',(0,1.55,0),8)
# Symmetrical side equipment with cyan power strips and raised rails.
for sign in [-1,1]:
 suffix='R' if sign>0 else 'L'
 box('SideHousing'+suffix,.14,.40,-.51,.30,1.40,'Navy',bevel=.04);parts[-1]['v'][:,0]+=sign*.575
 box('SideRail'+suffix,.19,.085,-.56,.34,1.64,'Edge',bevel=.025);parts[-1]['v'][:,0]+=sign*.59
 box('PowerStrip'+suffix,.025,.30,-.18,.03,1.40,'Glow',bevel=.009);parts[-1]['v'][:,0]+=sign*.66
 for j,z in enumerate([-.36,.18]):
  lathe('RailBolt'+suffix+str(j),[(1.683,.045),(1.715,.045)],'Dark','Pitch',center=(sign*.59,0,z),n=8)
box('IndicatorHousing',.35,.16,.925,1.01,.38,'Dark','Base',.025)
box('Indicator',.25,.065,1.01,1.025,.40,'Glow','Base',.014)
for i in range(3):box('RearVent'+str(i),.60,.045,-.671,-.651,1.36+i*.13,'Dark',bevel=.014)
# OBJ is world-space, named parts; Unity builder below supplies movable hierarchy.
lines=['mtllib LaserTurret.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/LaserTurret.obj').write_text('\n'.join(lines))
(P/'Models/LaserTurret.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKa 0.1 0.1 0.1\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]; pivot=np.array([0,1.55,0]) if p['group'] in ('Pitch','Recoil') else np.zeros(3)
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v-pivot],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersLaserTurret/Editor/LaserTurretMeshData.json').write_text(json.dumps(data))

for p in parts:
 tri=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
print('Parts:',len(parts),'Triangles:',sum(len(p['f']) for p in parts))
