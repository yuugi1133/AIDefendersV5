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
# Same footprint as level 1, but a broad square pedestal gives a new silhouette.
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
plinth('SquarePedestal',[(0,2.04,2.04),(.08,2.175,2.175),(.29,2.175,2.175),(.36,2.04,2.04)],'Navy')
plinth('GoldPedestalTier',[(.36,1.84,1.84),(.43,1.84,1.84),(.47,1.73,1.73)],'Gold')
plinth('UpperDeck',[(.47,1.67,1.67),(.55,1.54,1.54)],'Blue')
lathe('Turntable',[(.55,.49),(.67,.44)],'Dark','Yaw')
box('Support',.43,.42,-.21,.21,.85,'Dark','Yaw',.05)
box('WideReactorHull',1.22,.66,-.83,.24,1.53,'Navy','Pitch',.14)
box('RearArmor',1.26,.62,-.97,-.82,1.53,'Edge','Pitch',.12)
box('TopReactorDeck',.93,.10,-.68,.13,1.89,'Blue','Pitch',.045)
box('ReactorCrest',.24,.10,-.55,.01,1.99,'Gold','Pitch',.02)
for i in range(3):box('RearVent'+str(i),.69,.05,-.988,-.973,1.38+i*.13,'Dark','Pitch',.015)
# Two long separated emitter arms replace the ring-barrel body of level 1.
for sign in [-1,1]:
 box('ForkArm'+str(sign),.29,.31,.06,1.26,1.53,'Blue','Pitch',.065);parts[-1]['v'][:,0]+=sign*.64
 box('ForkArmGold'+str(sign),.075,.065,.18,1.16,1.714,'Gold','Pitch',.016);parts[-1]['v'][:,0]+=sign*.66
 box('ForkTip'+str(sign),.34,.35,1.17,1.39,1.53,'Dark','Pitch',.07);parts[-1]['v'][:,0]+=sign*.64
 box('ElectrodeGlowTip'+str(sign),.022,.18,1.20,1.35,1.53,'Glow','Pitch',.008);parts[-1]['v'][:,0]+=sign*.456
 # Large shoulder capacitors sit above the reactor sides.
 lathe('ShoulderCapacitor'+str(sign),[(-.74,.13),(-.64,.19),(-.10,.19),(.01,.14)],'Dark','Pitch','z',(sign*.57,1.90,0),n=8)
 for k,z in enumerate([-.56,-.30]):
  lathe('CapacitorBand'+str(sign)+str(k),[(z,.195),(z+.08,.195)],'Glow','Pitch','z',(sign*.57,1.90,0),n=8,caps=False)
 box('CheekGoldPlate'+str(sign),.025,.27,-.65,-.17,1.50,'Gold','Pitch',.009);parts[-1]['v'][:,0]+=sign*.628
# Four separated arc electrodes frame the orb with visible gaps, rather than continuous rings.
def arc(name,z0,z1,outer,inner,angle,mat):
 n=4;v=[]
 for z in [z0,z1]:
  for r in [outer,inner]:
   for t in np.linspace(math.radians(angle-28),math.radians(angle+28),n+1):v.append([r*math.cos(t),1.53+r*math.sin(t),z])
 count=n+1;f=[]
 for i in range(n):
  for a,b,c,d in [(i,i+1,i+1+2*count,i+2*count),(i+count,i+3*count,i+1+3*count,i+1+count),(i+2*count,i+1+2*count,i+1+3*count,i+3*count),(i,i+count,i+1+count,i+1)]:f.extend([[a,b,c],[a,c,d]])
 for i in [0,n]:
  q=[i,i+2*count,i+3*count,i+count]
  if i==n:q=q[::-1]
  f.extend([[q[0],q[1],q[2]],[q[0],q[2],q[3]]])
 mesh(name,v,f,mat,'Pitch')
for i,a in enumerate([45,135,225,315]):
 arc('SplitElectrodeArmor'+str(i),.96,1.24,.76,.47,a,'Navy')
 arc('SplitElectrodeGold'+str(i),1.245,1.29,.76,.64,a,'Gold')
 arc('InnerChargeCoil'+str(i),1.25,1.30,.51,.466,a,'Glow')
lathe('ChargedOrb',[(-.30,.001),(-.23,.19),(-.11,.28),(.11,.28),(.23,.19),(.30,.001)],'Core','ChargeOrb','y',(0,1.53,1.17),n=12)
box('IndicatorHousing',.44,.16,1.025,1.095,.23,'Dark','Base',.025)
box('Indicator',.32,.065,1.095,1.11,.24,'Glow','Base',.014)
# OBJ is world-space, named parts; Unity builder below supplies movable hierarchy.
lines=['mtllib PlasmaLevel2.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/PlasmaLevel2.obj').write_text('\n'.join(lines))
(P/'Models/PlasmaLevel2.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKa 0.1 0.1 0.1\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]; pivot=np.array([0,1.53,1.17]) if p['group']=='ChargeOrb' else np.array([0,1.53,0]) if p['group'] in ('Pitch','Recoil') else np.zeros(3)
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v-pivot],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersPlasmaLevel2/Editor/PlasmaLevel2MeshData.json').write_text(json.dumps(data))

for p in parts:
 tri=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
print('Parts:',len(parts),'Triangles:',sum(len(p['f']) for p in parts))

v=np.concatenate([q['v'] for q in parts]);assert len({q['name'] for q in parts})==len(parts)
(P/'ModelInfo.json').write_text(json.dumps({'parts':len(parts),'triangles':sum(len(q['f']) for q in parts),'bounds':[v.min(0).tolist(),v.max(0).tolist()],'pitchPivot':[0,1.53,0],'muzzle':[0,1.53,1.64],'orbPivot':[0,1.53,1.17]},indent=2))
