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

box('Housing',1.04,.88,-.435,.40,1.55,'Navy',bevel=.17)
p=parts.pop();tri=p['v'][p['f']];normal=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);normal/=np.linalg.norm(normal,axis=1)[:,None];mask=normal[:,1]>.5
mesh('UpperArmor',p['v'],p['f'][mask],'Blue','Pitch');mesh('Housing',p['v'],p['f'][~mask],'Navy','Pitch')
box('FrontRim',1.055,.875,.40,.445,1.55,'Edge',bevel=.17)
box('GoldMantlet',.88,.75,.445,.55,1.55,'Gold',bevel=.14)
box('Breech',.50,.46,.55,.65,1.55,'Dark',bevel=.075)
box('BarrelCollar',.35,.32,.65,.71,1.55,'Steel','Recoil',bevel=.045)
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
tube('SquareBarrel',.32,.30,.69,3.06,.055,'Steel')
tube('BarrelShroud',.43,.39,.69,1.72,.045,'Navy')
tube('ShroudEndBand',.46,.42,1.64,1.75,.05,'Edge')
box('BarrelGoldInlay',.075,.020,.82,1.60,1.755,'Gold','Recoil',bevel=.005)
tube('MuzzleBrake',.43,.40,2.94,3.16,.105,'Dark')
tube('MuzzleLip',.44,.41,3.16,3.22,.11,'Gold')
box('BoreShadow',.18,.16,.89,.90,1.55,'Black','Recoil',bevel=.015)
box('IndicatorHousing',.35,.16,.925,1.01,.38,'Dark','Base',.025)
box('Indicator',.25,.065,1.01,1.025,.40,'Glow','Base',.014)
for i in range(3):box('RearVent'+str(i),.58,.045,-.781,-.761,1.36+i*.13,'Dark',bevel=.014)
# Rear reinforcement band from 1.jpg; preserve the single precision barrel from 2.jpg.
box('RearReinforcement',1.075,.915,-.51,-.435,1.55,'Edge','Pitch',.14)
box('RearCasing',1.04,.88,-.95,-.51,1.55,'Navy','Pitch',.14)
# Small stage-one optic on two mounting feet.
box('ScopeRail',.29,.08,-.65,.38,2.015,'Dark','Pitch',.018)
for i,z in enumerate([-.40,.24]):box('ScopeMount'+str(i),.18,.20,z-.06,z+.06,2.10,'Steel','Pitch',.018)
# Final-stage long-range optic: broad objective hood, larger eyepiece and controls.
lathe('ScopeBody',[(-.79,.15),(-.63,.185),(.21,.185),(.49,.275),(.72,.275)],'Steel','Pitch','z',(0,2.33,0),n=12)
lathe('ScopeObjectiveArmor',[(.48,.286),(.75,.286)],'Navy','Pitch','z',(0,2.33,0),n=12)
lathe('ScopeFrontRim',[(.75,.292),(.83,.292)],'Gold','Pitch','z',(0,2.33,0),n=12)
lathe('ScopeLensSeat',[(.832,.25),(.85,.25)],'Dark','Pitch','z',(0,2.33,0),n=12)
lathe('ScopeFrontLens',[(.851,.20),(.855,.20)],'Glow','Pitch','z',(0,2.33,0),n=12)
lathe('ScopeRearCap',[(-.94,.18),(-.79,.18)],'Dark','Pitch','z',(0,2.33,0),n=12)
lathe('ScopeRearGoldBand',[(-.81,.183),(-.77,.183)],'Gold','Pitch','z',(0,2.33,0),n=12,caps=False)
lathe('ElevationKnob',[(2.48,.11),(2.62,.11),(2.65,.09)],'Dark','Pitch','y',(0,0,-.18),n=8)
lathe('KnobGoldMark',[(2.61,.113),(2.64,.113)],'Gold','Pitch','y',(0,0,-.18),n=8,caps=False)
box('OpticLowerCradle',.25,.12,-.63,.42,2.14,'Blue','Pitch',.02)
# Broader angular cheek plates with clear gold insets.
for sign in [-1,1]:
 box('SideArmor'+str(sign),.13,.62,-.49,.31,1.52,'Blue','Pitch',.04)
 parts[-1]['v'][:,0]+=sign*.56
 box('SideArmorGold'+str(sign),.02,.39,-.34,.20,1.52,'Gold','Pitch',.007)
 parts[-1]['v'][:,0]+=sign*.635
 for i in range(3):
  box('ArmorVent'+str(sign)+str(i),.024,.045,-.28+i*.14,-.19+i*.14,1.36,'Dark','Pitch',.007)
  parts[-1]['v'][:,0]+=sign*.648
 # Narrow accent above the long barrel guard.
 box('BarrelSideRail'+str(sign),.035,.10,.83,1.60,1.55,'Edge','Recoil',.008)
 parts[-1]['v'][:,0]+=sign*.225
# Gold side fasteners as in the body reference.
for sign in [-1,1]:
 lathe('SideFastener'+str(sign),[(0,.065),(.025,.065)],'Gold','Pitch','z',n=6)
 q=parts[-1]['v'].copy();parts[-1]['v']=np.column_stack([sign*(q[:,2]+.525),q[:,0]+1.55,q[:,1]-.08])
# Seat housing and optic to retain the common turret height, including the scope.
for q in parts:
 if q['group'] in ('Pitch','Recoil'):q['v'][:,1]-=.15
# The new rear casing covers the old rear vents.
parts=[q for q in parts if not q['name'].startswith('RearVent')]
# OBJ is world-space, named parts; Unity builder below supplies movable hierarchy.
lines=['mtllib PrecisionLevel3.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/PrecisionLevel3.obj').write_text('\n'.join(lines))
(P/'Models/PrecisionLevel3.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKa 0.1 0.1 0.1\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]; pivot=np.array([0,1.40,0]) if p['group'] in ('Pitch','Recoil') else np.zeros(3)
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v-pivot],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersPrecisionLevel3/Editor/PrecisionLevel3MeshData.json').write_text(json.dumps(data))

for p in parts:
 tri=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
print('Parts:',len(parts),'Triangles:',sum(len(p['f']) for p in parts))

v=np.concatenate([q['v'] for q in parts]);assert len({q['name'] for q in parts})==len(parts)
(P/'ModelInfo.json').write_text(json.dumps({'parts':len(parts),'triangles':sum(len(q['f']) for q in parts),'bounds':[v.min(0).tolist(),v.max(0).tolist()],'pitchPivot':[0,1.4,0],'muzzle':[0,1.4,3.24],'barrelCount':1},indent=2))
