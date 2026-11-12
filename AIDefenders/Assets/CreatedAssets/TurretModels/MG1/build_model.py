from pathlib import Path
import numpy as np,json,math
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
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
box('Housing',1.10,1.12,-.65,.42,1.53,'Navy',bevel=.20)
box('TopArmor',1.115,.30,-.60,.39,1.95,'Blue',bevel=.1)
# Gold octagonal frame: concentric profile perimeter extruded around barrel mount.
def frame():
 outer=[(-.35,-.56),(.35,-.56),(.55,-.36),(.55,.36),(.35,.56),(-.35,.56),(-.55,.36),(-.55,-.36)]
 inner=[(a*.80,b*.81) for a,b in outer]
 v=[[a,1.53+b,z] for z in (.43,.49) for ring in (outer,inner) for a,b in ring];f=[]
 for i in range(8):
  j=(i+1)%8
  for a,b,c,d in [(i,j,j+16,i+16),(i+8,i+24,j+24,j+8),(i+16,j+16,j+24,i+24),(i,i+8,j+8,j)]:
   if len({a,b,c,d})==4:f.extend([[a,b,c],[a,c,d]])
 mesh('GoldFaceFrame',v,f,'Gold','Pitch')
frame()
lathe('BarrelSocket',[(.43,.30),(.58,.30),(.66,.23)],'Steel','Pitch','z',(0,1.53,0),8)
lathe('Barrel',[(.59,.15),(1.28,.15)],'Steel','Recoil','z',(0,1.53,0),8)
lathe('Muzzle',[(1.20,.21),(1.30,.24),(1.49,.24),(1.56,.20),(1.56,.12),(1.20,.12),(1.20,.21)],'Dark','Recoil','z',(0,1.53,0),8,False)
lathe('BoreShadow',[(1.355,.119),(1.36,.119)],'Black','Recoil','z',(0,1.53,0),8)
box('IndicatorHousing',.35,.16,.925,1.01,.38,'Dark','Base',.025)
box('Indicator',.25,.065,1.01,1.025,.40,'Glow','Base',.014)
# Small rear ventilation plates.
for i in range(3):box('RearVent'+str(i),.60,.05,-.671,-.651,1.37+i*.13,'Dark',bevel=.015)
# OBJ is world-space, named parts; Unity builder below supplies movable hierarchy.
lines=['mtllib Turret.mtl'];offset=1
for p in parts:
 lines+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 lines+=['v %.6f %.6f %.6f'%tuple(v) for v in p['v']]
 lines+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/Turret.obj').write_text('\n'.join(lines))
(P/'Models/Turret.mtl').write_text('\n'.join('newmtl '+k+'\nKd '+' '.join(str(int(c[i:i+2],16)/255) for i in (1,3,5))+'\nKa 0.1 0.1 0.1\nKs 0.1 0.1 0.1\nNs 24\n' for k,c in colors.items()))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]; pivot=np.array([0,1.53,0]) if p['group'] in ('Pitch','Recoil') else np.zeros(3)
 data['parts'].append(dict(name=p['name'],group=p['group'],color=colors[p['mat']],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v-pivot],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersTurret/Editor/TurretMeshData.json').write_text(json.dumps(data))
fig=plt.figure(figsize=(12,7),facecolor='#e8edf1')
for idx,(az,title) in enumerate([(55,'FRONT / THREE-QUARTER'),(235,'REAR / THREE-QUARTER')]):
 ax=fig.add_subplot(1,2,idx+1,projection='3d');ax.set_facecolor('#e8edf1')
 allpoly=[];allcolors=[]
 for p in parts:
  poly=p['v'][p['f']];poly=poly[:,:,[0,2,1]]
  norms=np.cross(poly[:,1]-poly[:,0],poly[:,2]-poly[:,0]);norms/=np.maximum(np.linalg.norm(norms,axis=1)[:,None],1e-9)
  light=np.array([-.4,.1,1]);light/=np.linalg.norm(light)
  rgb=np.array([int(p['mat'] and colors[p['mat']][i:i+2],16)/255 for i in (1,3,5)])
  shades=.57+.43*np.maximum(0,-norms@light)
  allpoly.extend(poly);allcolors.extend(np.clip(shades[:,None]*rgb,0,1))
 ax.add_collection3d(Poly3DCollection(allpoly,facecolors=allcolors,edgecolors='none',zsort='average'))
 ax.set(xlim=(-1.5,1.5),ylim=(-1.2,1.8),zlim=(0,2.5));ax.set_box_aspect((3,3,2.5));ax.view_init(elev=25,azim=az);ax.set_axis_off();ax.set_title(title,color='#526372',fontsize=10,pad=-10)
fig.suptitle('AI DEFENDERS  /  LOW-POLY TURRET',fontsize=18,color='#18364b',y=.94)
fig.text(.5,.08,'NAVY + AMBER     |     SEPARATE AIM & RECOIL PARTS',ha='center',fontsize=10,color='#526372')
fig.savefig(P/'Preview.png',dpi=160,bbox_inches='tight');plt.close(fig)
for p in parts:
 tri=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)>1e-9),p['name']
print('Parts:',len(parts),'Triangles:',sum(len(p['f']) for p in parts))
