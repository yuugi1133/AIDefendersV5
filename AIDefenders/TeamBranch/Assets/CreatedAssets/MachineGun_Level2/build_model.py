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
def plinth(name,rings,mat):
 v=[]
 for y,w,d in rings:
  x=w/2;z=d/2;b=.16
  v.extend([[a,y,c] for a,c in [(-x+b,-z),(x-b,-z),(x,-z+b),(x,z-b),(x-b,z),(-x+b,z),(-x,z-b),(-x,-z+b)]])
 f=[]
 for j in range(len(rings)-1):
  for i in range(8):
   a=j*8+i;b=j*8+(i+1)%8;f.extend([[a,b+8,b],[a,a+8,b+8]])
 for i in range(1,7):f.extend([[0,i,i+1],[(len(rings)-1)*8,(len(rings)-1)*8+i+1,(len(rings)-1)*8+i]])
 mesh(name,v,f,mat,'Base')
plinth('SquareFoundation',[(0,2.06,2.06),(.06,2.175,2.175),(.28,2.175,2.175),(.34,2.04,2.04)],'Navy')
plinth('SquareTop',[(.32,2.06,2.06),(.38,1.96,1.96)],'Blue')
plinth('RaisedGoldFrame',[(.38,1.57,1.57),(.43,1.57,1.57),(.50,1.44,1.44)],'Gold')
plinth('RaisedInnerDeck',[(.49,1.25,1.25),(.53,1.22,1.22)],'Blue')
lathe('Turntable',[(.55,.49),(.64,.45)],'Dark','Yaw')
box('Support',.36,.46,-.18,.18,.84,'Dark','Yaw',.045)

box('Housing',1.04,1.10,-.76,.40,1.55,'Navy',bevel=.17)
p=parts.pop();tri=p['v'][p['f']];normal=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);normal/=np.linalg.norm(normal,axis=1)[:,None];mask=normal[:,1]>.5
mesh('UpperArmor',p['v'],p['f'][mask],'Blue','Pitch');mesh('Housing',p['v'],p['f'][~mask],'Navy','Pitch')
box('FrontRim',1.055,1.095,.40,.445,1.55,'Edge',bevel=.17)
box('GoldMantlet',.88,.91,.445,.55,1.55,'Gold',bevel=.14)
box('Breech',.64,.62,.55,.65,1.55,'Dark',bevel=.075)
box('BarrelCollar',.51,.49,.65,.71,1.55,'Steel','Recoil',bevel=.045)
# A hollow, square-profile barrel with a small bevel on the corners.
def tube(name,w,h,z0,z1,t,mat):
 def profile(w,h,b):
  x=w/2;y=h/2;return [(-x+b,-y),(x-b,-y),(x,-y+b),(x,y-b),(x-b,y),(-x+b,y),(-x,y-b),(-x,-y+b)]
 outer=profile(w,h,.028);inner=profile(w-2*t,h-2*t,.015)
 v=[[a,1.55+b,z] for z in (z0,z1) for ring in (outer,inner) for a,b in ring];f=[]
 for i in range(8):
  j=(i+1)%8
  for a,b,c,d in [(i,j,j+16,i+16),(i+8,i+24,j+24,j+8),(i+16,j+16,j+24,i+24),(i,i+8,j+8,j)]:f.extend([[a,b,c],[a,c,d]])
 mesh(name,v,f,mat,'Recoil')
# Three open square barrels in a triangular cluster, matching the new MG1 reference.
muzzles=[]
for i,(x,y) in enumerate([(-.15,1.66),(.15,1.66),(0,1.40)]):
 tube('Barrel'+str(i),.19,.19,.69,1.53,.034,'Steel')
 parts[-1]['v']+=np.array([x,y-1.55,0])
 tube('MuzzleLip'+str(i),.215,.215,1.48,1.59,.041,'Dark')
 parts[-1]['v']+=np.array([x,y-1.55,0])
 box('BoreShadow'+str(i),.116,.116,.94,.95,y,'Black','Recoil',bevel=.013)
 parts[-1]['v'][:,0]+=x
 muzzles.append([x,y,1.61])
box('IndicatorHousing',.35,.16,.925,1.01,.38,'Dark','Base',.025)
box('Indicator',.25,.065,1.01,1.025,.40,'Glow','Base',.014)
for i in range(3):box('RearVent'+str(i),.58,.045,-.781,-.761,1.36+i*.13,'Dark',bevel=.014)
# Duplicate the three-barrel pod into left/right housings.
pods=[q for q in parts if q['group'] in ('Pitch','Recoil')]
parts=[q for q in parts if q['group'] not in ('Pitch','Recoil')]
for side,x in [('Left',-.50),('Right',.50)]:
 for q in pods:
  v=q['v'].copy();v[:,0]=v[:,0]*.80+x
  mesh(q['name']+side,v,q['f'].copy(),q['mat'],'Recoil'+side if q['group']=='Recoil' else 'Pitch')
muzzles=[[x+dx*.80,y,1.61] for x in [-.50,.50] for dx,y in [(-.15,1.66),(.15,1.66),(0,1.40)]]
# Indicator sits on the front face of the new square pedestal.
for q in parts:
 if q['name'] in ('Indicator','IndicatorHousing'):q['v'][:,1]-=.15;q['v'][:,2]+=.075
# OBJ is world-space, named parts; Unity builder below supplies movable hierarchy.
lines=['mtllib MachineGunLevel2.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/MachineGunLevel2.obj').write_text('\n'.join(lines))
(P/'Models/MachineGunLevel2.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKa 0.1 0.1 0.1\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]; pivot=np.array([0,1.55,0]) if p['group'] in ('Pitch','RecoilLeft','RecoilRight') else np.zeros(3)
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v-pivot],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersMachineGunLevel2/Editor/MachineGunLevel2MeshData.json').write_text(json.dumps(data))

for p in parts:
 tri=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
print('Parts:',len(parts),'Triangles:',sum(len(p['f']) for p in parts))

v=np.concatenate([p['v'] for p in parts]);assert len({p['name'] for p in parts})==len(parts)
(P/'ModelInfo.json').write_text(json.dumps({'triangles':sum(len(p['f']) for p in parts),'parts':len(parts),'height':float(v[:,1].max()),'bounds':[v.min(0).tolist(),v.max(0).tolist()],'muzzles':muzzles,'pitchPivot':[0,1.55,0]},indent=2))
