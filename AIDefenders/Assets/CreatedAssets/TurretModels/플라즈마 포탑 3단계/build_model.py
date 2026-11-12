from pathlib import Path
import numpy as np,json,math
P=Path(__file__).parent
colors={'Navy':'#24465e','Blue':'#396780','Edge':'#7396a3','Gold':'#f3b642','Dark':'#26333c','Steel':'#4e626c','Black':'#111f28','Glow':'#ab74f4','Core':'#ead2ff'}
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
# Final upgrade: low hub and four diagonal stabilizer feet.
for i,angle in enumerate([45,135,225,315]):
 for name,width,h,z0,z1,y,mat in [('Stabilizer',.33,.21,.22,1.36,.12,'Navy'),('FootArmor',.27,.045,.30,1.28,.25,'Blue'),('GoldToe',.335,.215,1.10,1.19,.12,'Gold'),('FootPowerBand',.338,.218,.72,.81,.12,'Glow')]:
  box(name+str(i),width,h,z0,z1,y,mat,'Base',.025)
  a=math.radians(angle);c=math.cos(a);ss=math.sin(a);parts[-1]['v']=parts[-1]['v']@np.array([[c,0,-ss],[0,1,0],[ss,0,c]])
lathe('FoundationHub',[(0,.48),(.08,.62),(.36,.62),(.45,.53)],'Navy',n=12)
lathe('GoldHubBelt',[(.27,.625),(.34,.625)],'Gold',caps=False)
lathe('UpperHub',[(.45,.52),(.55,.60),(.63,.53)],'Blue',n=12)
lathe('Turntable',[(.63,.42),(.72,.38)],'Dark','Yaw')
box('HeavySupport',.46,.54,-.30,.22,.94,'Dark','Yaw',.07)
# Octagonal axial reaction chamber; gold band and a rear luminous core.
lathe('MainReactor',[(-1.05,.44),(-.90,.56),(-.23,.56),(.11,.43)],'Navy','Pitch','z',(0,1.65,0),n=8)
lathe('ReactorFrontBand',[(.10,.44),(.20,.44)],'Gold','Pitch','z',(0,1.65,0),n=8)
lathe('ReactorRearArmor',[(-1.15,.46),(-1.05,.46)],'Steel','Pitch','z',(0,1.65,0),n=8)
lathe('RearEnergyDisc',[(-1.158,.25),(-1.152,.25)],'Glow','Pitch','z',(0,1.65,0),n=12)
# Three radial focusing arms form an open triangular cage around a much larger orb.
def beam(name,a,b,width,height,mat):
 a=np.array(a,float);b=np.array(b,float);z=b-a;length=np.linalg.norm(z);z/=length
 helper=np.array([0,1,0]) if abs(z[1])<.9 else np.array([1,0,0]);x=np.cross(helper,z);x/=np.linalg.norm(x);y=np.cross(z,x)
 box(name,width,height,-length/2,length/2,0,mat,'Pitch',min(.035,width*.2,height*.2))
 parts[-1]['v']=parts[-1]['v']@np.array([x,y,z])+(a+b)/2
for i,angle in enumerate([90,210,330]):
 a=math.radians(angle);rad=np.array([math.cos(a),math.sin(a),0]);center=np.array([0,1.65,0])
 points=[center+rad*.43+[0,0,-.15],center+rad*.87+[0,0,.45],center+rad*.91+[0,0,1.12],center+rad*.72+[0,0,1.60]]
 for j,(aa,bb) in enumerate(zip(points,points[1:])):
  beam('FocusingArm'+str(i)+'_'+str(j),aa,bb,.24,.24,'Blue' if j<2 else 'Navy')
  beam('GoldArmSpine'+str(i)+'_'+str(j),aa+rad*.135,bb+rad*.135,.075,.045,'Gold')
 # Dense emitter head aimed back towards the core.
 tip=points[-1]
 lathe('ElectrodeHead'+str(i),[(1.48,.15),(1.69,.15)],'Steel','Pitch','z',(tip[0],tip[1],0),n=8)
 lathe('ElectrodeGlowTip'+str(i),[(1.692,.10),(1.705,.10)],'Glow','Pitch','z',(tip[0],tip[1],0),n=8)
 # One rear capacitor per arm, visually integrated with the triangular layout.
 cap=center+rad*.58
 lathe('RadialCapacitor'+str(i),[(-.96,.12),(-.84,.18),(-.39,.18),(-.25,.12)],'Dark','Pitch','z',(cap[0],cap[1],0),n=8)
 for k,z in enumerate([-.76,-.53]):
  lathe('CapacitorBand'+str(i)+str(k),[(z,.187),(z+.08,.187)],'Glow','Pitch','z',(cap[0],cap[1],0),n=8,caps=False)
lathe('ChargedOrb',[(-.38,.001),(-.30,.23),(-.14,.35),(.14,.35),(.30,.23),(.38,.001)],'Core','ChargeOrb','y',(0,1.65,1.25),n=16)
box('IndicatorHousing',.40,.16,.56,.63,.20,'Dark','Base',.025)
box('Indicator',.28,.065,.63,.65,.22,'Glow','Base',.014)
# OBJ is world-space, named parts; Unity builder below supplies movable hierarchy.
lines=['mtllib PlasmaLevel3.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/PlasmaLevel3.obj').write_text('\n'.join(lines))
(P/'Models/PlasmaLevel3.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKa 0.1 0.1 0.1\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]; pivot=np.array([0,1.65,1.25]) if p['group']=='ChargeOrb' else np.array([0,1.65,0]) if p['group'] in ('Pitch','Recoil') else np.zeros(3)
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v-pivot],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersPlasmaLevel3/Editor/PlasmaLevel3MeshData.json').write_text(json.dumps(data))

for p in parts:
 tri=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
print('Parts:',len(parts),'Triangles:',sum(len(p['f']) for p in parts))

v=np.concatenate([q['v'] for q in parts]);assert len({q['name'] for q in parts})==len(parts)
(P/'ModelInfo.json').write_text(json.dumps({'parts':len(parts),'triangles':sum(len(q['f']) for q in parts),'bounds':[v.min(0).tolist(),v.max(0).tolist()],'pitchPivot':[0,1.65,0],'muzzle':[0,1.65,1.89],'orbPivot':[0,1.65,1.25]},indent=2))
