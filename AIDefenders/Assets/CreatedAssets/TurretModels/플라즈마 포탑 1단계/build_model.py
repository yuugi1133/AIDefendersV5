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
lathe('Foundation',[(0,1.03),(.09,1.12),(.21,1.12),(.30,1.01)],'Navy')
lathe('GoldBelt',[(.14,1.126),(.22,1.126),(.30,1.045)],'Gold',caps=False)
lathe('UpperPlinth',[(.30,1.01),(.49,.91),(.55,.82)],'Blue')
lathe('TopRim',[(.52,.85),(.56,.81),(.57,.73)],'Edge',caps=False)
lathe('Turntable',[(.55,.49),(.64,.45)],'Dark','Yaw')
box('Support',.36,.46,-.18,.18,.84,'Dark','Yaw',.045)

# Compact plasma chamber and open launch ring instead of a conventional gun barrel.
box('Chamber',.76,.72,-.78,.16,1.53,'Navy','Pitch',.14)
box('ChamberTop',.62,.10,-.66,.07,1.89,'Blue','Pitch',.045)
box('RearCap',.79,.68,-.87,-.77,1.53,'Edge','Pitch',.12)
for i in range(3):box('RearVent'+str(i),.45,.045,-.888,-.873,1.39+i*.13,'Dark','Pitch',.012)
# True annular mesh with an unobstructed central aperture.
def ring(name,z0,z1,outer,inner,mat,n=12):
 v=[]
 for z in [z0,z1]:
  for r in [outer,inner]:
   for i in range(n):
    a=2*math.pi*i/n;v.append([r*math.cos(a),1.53+r*math.sin(a),z])
 f=[]
 for i in range(n):
  j=(i+1)%n
  for aa,bb,cc,dd in [(i,j,j+2*n,i+2*n),(i+n,i+3*n,j+3*n,j+n),(i+2*n,j+2*n,j+3*n,i+3*n),(i,i+n,j+n,j)]:f.extend([[aa,bb,cc],[aa,cc,dd]])
 mesh(name,v,f,mat,'Pitch')
ring('LaunchRingArmor',.66,.98,.55,.39,'Navy')
ring('LaunchRingGoldFace',.985,1.025,.525,.405,'Gold')
ring('InnerChargeCoil',.78,.97,.397,.355,'Glow')
# Twin arms support the ring, with side-mounted energy capacitors.
for sign in [-1,1]:
 box('EmitterArm'+str(sign),.17,.22,-.02,.75,1.53,'Blue','Pitch',.045);parts[-1]['v'][:,0]+=sign*.43
 box('ArmGoldEdge'+str(sign),.045,.08,.05,.62,1.66,'Gold','Pitch',.015);parts[-1]['v'][:,0]+=sign*.46
 lathe('Capacitor'+str(sign),[(-.55,.105),(-.47,.14),(-.08,.14),(.02,.105)],'Dark','Pitch','z',(sign*.43,1.53,0),n=8)
 for k,z in enumerate([-.41,-.20]):
  lathe('CapacitorBand'+str(sign)+str(k),[(z,.145),(z+.055,.145)],'Glow','Pitch','z',(sign*.43,1.53,0),n=8,caps=False)
# Three forward electrode clamps give the opening a recognisable plasma silhouette.
for i in range(3):
 a=math.radians(90+i*120)
 box('Electrode'+str(i),.15,.17,.87,1.14,1.53,'Steel','Pitch',.035)
 q=parts[-1]['v'];q[:,1]-=1.53
 c=math.cos(a);ss=math.sin(a);q[:,:2]=q[:,:2]@np.array([[c,ss],[-ss,c]])
 q[:,0]+=.48*math.cos(a);q[:,1]+=1.53+.48*math.sin(a)
 lathe('ElectrodeGlow'+str(i),[(1.142,.047),(1.15,.047)],'Glow','Pitch','z',(.48*math.cos(a),1.53+.48*math.sin(a),0),n=6)
# Charged orb is a separate visual part so gameplay can hide or pulse it.
lathe('ChargedOrb',[(-.19,.001),(-.145,.12),(-.07,.177),(.07,.177),(.145,.12),(.19,.001)],'Core','ChargeOrb','y',(0,1.53,.94),n=12)
box('IndicatorHousing',.35,.16,.925,1.01,.38,'Dark','Base',.025)
box('Indicator',.25,.065,1.01,1.025,.40,'Glow','Base',.014)
# OBJ is world-space, named parts; Unity builder below supplies movable hierarchy.
lines=['mtllib PlasmaLevel1.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/PlasmaLevel1.obj').write_text('\n'.join(lines))
(P/'Models/PlasmaLevel1.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKa 0.1 0.1 0.1\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]; pivot=np.array([0,1.53,.94]) if p['group']=='ChargeOrb' else np.array([0,1.53,0]) if p['group'] in ('Pitch','Recoil') else np.zeros(3)
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v-pivot],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersPlasmaLevel1/Editor/PlasmaLevel1MeshData.json').write_text(json.dumps(data))

for p in parts:
 tri=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
print('Parts:',len(parts),'Triangles:',sum(len(p['f']) for p in parts))

v=np.concatenate([q['v'] for q in parts]);assert len({q['name'] for q in parts})==len(parts)
(P/'ModelInfo.json').write_text(json.dumps({'parts':len(parts),'triangles':sum(len(q['f']) for q in parts),'bounds':[v.min(0).tolist(),v.max(0).tolist()],'pitchPivot':[0,1.53,0],'muzzle':[0,1.53,1.25],'orbPivot':[0,1.53,.94]},indent=2))
