"""Rebuild the MG2 upgrade game mesh with numpy (Y up, +Z forward)."""
from pathlib import Path
import json, math
import numpy as np
P=Path(__file__).resolve().parent
colors={'Navy':'#264A63','Roof':'#5594B2','Edge':'#78ADC1','Steel':'#405969','Dark':'#243541','Black':'#10242F','Gold':'#F1AC3D','GoldLight':'#FFD36C','Glow':'#15E5DC'}
parts=[]
PIVOT=np.array([0.,1.55,0.])
def add(name,vertices,faces,material,group='Pitch',emissive=False):
 v=np.asarray(vertices,dtype=float);f=np.asarray(faces,dtype=int)
 if len(f)==0:return
 parts.append(dict(name=name,v=v,f=f,mat=material,group=group,emissive=emissive))
def profile(w,h,b):
 x=w/2;y=h/2
 return np.array([[-x+b,-y],[x-b,-y],[x,-y+b],[x,y-b],[x-b,y],[-x+b,y],[-x,y-b],[-x,-y+b]])
def loft(name,outline,rings,mat,group='Pitch',center=(0,1.55,0),emissive=False):
 v=[];n=len(outline)
 for z,s in rings:
  v.extend([[x*s+center[0],y*s+center[1],z+center[2]] for x,y in outline])
 f=[]
 for k in range(len(rings)-1):
  for i in range(n):
   a=k*n+i;b=k*n+(i+1)%n;c=b+n;d=a+n;f.extend([[a,b,c],[a,c,d]])
 for k,reverse in [(0,True),(len(rings)-1,False)]:
  idx=len(v);v.append([center[0],center[1],rings[k][0]+center[2]])
  for i in range(n):
   a=k*n+i;b=k*n+(i+1)%n;f.append([idx,b,a] if reverse else [idx,a,b])
 add(name,v,f,mat,group,emissive)
def round_mesh(name,rings,mat,group='Pitch',axis='z',center=(0,1.55,0),n=12,caps=True,emissive=False,skip_front=False):
 v=[]
 for t,r in rings:
  for i in range(n):
   a=2*math.pi*(i+.5)/n
   q=[r*math.cos(a),r*math.sin(a),t] if axis=='z' else [r*math.cos(a),t,r*math.sin(a)]
   v.append(np.array(q)+center)
 f=[]
 for k in range(len(rings)-1):
  for i in range(n):
   a=k*n+i;b=k*n+(i+1)%n;c=b+n;d=a+n
   if skip_front and (v[a][2]+v[b][2])/2>.80:continue
   f.extend([[a,b,c],[a,c,d]] if axis=='z' else [[a,c,b],[a,d,c]])
 if caps:
  for k,top in [(0,False),(len(rings)-1,True)]:
   idx=len(v);v.append(np.array([0,0,rings[k][0]] if axis=='z' else [0,rings[k][0],0])+center)
   for i in range(n):
    a=k*n+i;b=k*n+(i+1)%n;f.append([idx,a,b] if (axis=='z')==top else [idx,b,a])
 add(name,v,f,mat,group,emissive)
def split_surfaces(name,topmat,bevelmat):
 p=parts.pop();tri=p['v'][p['f']];norm=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);norm/=np.linalg.norm(norm,axis=1)[:,None]
 top=norm[:,1]>.95;bevel=(norm[:,1]>.2)&~top
 for key,mask,mat in [('Top',top,topmat),('Bevel',bevel,bevelmat),('Body',~(top|bevel),p['mat'])]:
  add(name+key,p['v'],p['f'][mask],mat,p['group'])

BASE_WIDTH=2.1752649608029814
# Four individual feet; narrow gaps and a cross-shaped support connect them.
def pad(name,w,d,rings,mat,x,z,emissive=False):
 loft(name,profile(w,d,min(.12,w/5,d/5)),rings,mat,'Base',(0,0,0),emissive)
 q=parts[-1]['v'].copy();parts[-1]['v']=np.column_stack([q[:,0]+x,q[:,2],-q[:,1]+z])
for side,x in [('L',-.58),('R',.58)]:
 for end,z in [('F',.58),('B',-.58)]:
  n=side+end;w=BASE_WIDTH-1.16
  pad('Foot'+n,w,w,[(0,.92),(.045,1),(.145,1)],'Navy',x,z)
  pad('EnergyBand'+n,w*.985,w*.985,[(.145,1),(.18,1)],'Glow',x,z,True)
  pad('UpperArmor'+n,w,w,[(.18,1),(.275,1),(.30,.96)],'Navy',x,z)
  pad('GoldBorder'+n,w*.97,w*.97,[(.30,1),(.36,1),(.385,.94)],'Gold',x,z)
  pad('InsetDeck'+n,w*.80,w*.80,[(.384,1),(.39,1)],'Roof',x,z)
pad('CrossBraceX',1.80,.28,[(.23,1),(.36,1)],'Dark',0,0)
pad('CrossBraceZ',.28,1.80,[(.23,1),(.36,1)],'Dark',0,0)
pad('CentralPlatform',1.10,.96,[(.36,.94),(.40,1),(.55,1)],'Steel',0,0)
round_mesh('TurnBearing',[(.55,.30),(.63,.28)],'Dark','Yaw','y',(0,0,0),12)
loft('Support',profile(.38,.46,.055),[(-.18,1),(.18,1)],'Dark','Yaw',(0,.84,0))
head_start=len(parts)
# Chamfered armor front and rear, with clean top color split.
loft('Armor',profile(1.08,1.10,.18),[(-.70,.91),(-.63,1),(.41,1),(.48,.94)],'Navy')
split_surfaces('Armor','Roof','Roof')
loft('FaceGasket',profile(1.075,1.085,.18),[(.476,.96),(.50,1),(.52,.97)],'Edge')
loft('GoldFace',profile(.945,.985,.16),[(.52,.96),(.548,1),(.601,1),(.622,.94)],'Gold')
split_surfaces('GoldFace','GoldLight','GoldLight')
round_mesh('EmitterBase',[(.618,.255),(.66,.285),(.735,.285),(.77,.225)],'Dark',n=8)
round_mesh('EmitterRim',[(.77,.225),(.80,.215),(.817,.172)],'Edge',n=8)
round_mesh('RodSocket',[(.816,.165),(.846,.165)],'Steel',n=8)
# The cyan rod is actual solid geometry, with emission enabled in Unity.
round_mesh('EnergyRod',[(.84,.137),(1.39,.137)],'Glow',n=8,emissive=True)
round_mesh('Muzzle',[(1.32,.19),(1.38,.218),(1.53,.218),(1.575,.174),(1.575,.11),(1.37,.11),(1.32,.19)],'Navy',n=8,caps=False)
round_mesh('MuzzleEdge',[(1.532,.219),(1.58,.176),(1.58,.112),(1.55,.112)],'Edge',n=8,caps=False)
round_mesh('ApertureDark',[(1.44,.109),(1.447,.109)],'Black',n=8)
round_mesh('Lens',[(1.448,.066),(1.453,.066)],'Glow',n=8,emissive=True)

# Additional gold side armor replaces the Level 2 side capsules.
for sign in [-1,1]:
 loft('SideGoldArmor'+str(sign),profile(.075,.24,.025),[(-.49,.80),(-.43,1),(.23,1),(.31,.70)],'Gold','Pitch',(sign*.57,1.43,0))
# Make room for the upper crest while keeping the established 2.1 unit height.
for part in parts[head_start:]:part['v'][:,1]=1.55+(part['v'][:,1]-1.55)*.90
loft('GoldUpgradeCrest',profile(.25,.055,.015),[(-.50,.72),(-.43,1),(.17,1),(.25,.65)],'Gold','Pitch',(0,2.0725,0))
split_surfaces('GoldUpgradeCrest','GoldLight','GoldLight')
# Three tall rear energy cells, seated in dark sockets and attached by a bracket.
loft('RearCellBracket',profile(1.04,.12,.025),[(-.98,1),(-.65,1)],'Dark','Pitch',(0,1.34,0))
for j,x in enumerate([-.34,0,.34]):
 round_mesh('CellSocket'+str(j),[(1.34,.148),(1.41,.148)],'Navy','Pitch','y',(x,0,-.88),8)
 round_mesh('RearEnergyCell'+str(j),[(1.40,.116),(1.98,.116),(2.065,.10),(2.10,.045)],'Glow','Pitch','y',(x,0,-.88),8,True,True)
# Generate flat-shaded mesh data and a portable OBJ.
(P/'Models').mkdir(exist_ok=True)
obj=['# Y up; +Z forward; units match the Unity model','mtllib LaserLevel3.mtl'];offset=1
for p in parts:
 obj+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 obj+=['v %.7f %.7f %.7f'%tuple(v) for v in p['v']]
 obj+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/LaserLevel3.obj').write_text('\n'.join(obj))
mtl=[]
for name,h in colors.items():
 rgb=[int(h[i:i+2],16)/255 for i in (1,3,5)]
 mtl+=['newmtl '+name,'Kd '+' '.join(map(str,rgb)),'Ks 0.12 0.12 0.12','Ns 24']
 if name=='Glow':mtl+=['Ke '+' '.join(map(str,rgb))]
(P/'Models/LaserLevel3.mtl').write_text('\n'.join(mtl))
data={'parts':[]}
for p in parts:
 vertices=p['v'][p['f'].flatten()]-(PIVOT if p['group']=='Pitch' else 0)
 data['parts'].append(dict(name=p['name'],group=p['group'],material=p['mat'],color=colors[p['mat']],emissive=p['emissive'],vertices=[dict(x=float(v[0]),y=float(v[1]),z=float(v[2])) for v in vertices],triangles=list(range(len(vertices)))))
(P/'Assets/AIDefendersLaserLevel3/Editor/LaserLevel3MeshData.json').write_text(json.dumps(data,separators=(',',':')))
vertices=np.concatenate([p['v'][np.unique(p['f'])] for p in parts]);bounds=[vertices.min(0).tolist(),vertices.max(0).tolist()]
stats={'meshParts':len(parts),'triangles':sum(len(p['f']) for p in parts),'bounds':bounds,'height':float(vertices[:,1].max()),'materials':len(colors),'forward':'+Z','up':'+Y','pitchPivot':PIVOT.tolist(),'laserOrigin':[0,1.55,1.60]}
(P/'ModelInfo.json').write_text(json.dumps(stats,indent=2))
for p in parts:
 assert np.all(np.isfinite(p['v']))
 assert p['f'].min()>=0 and p['f'].max()<len(p['v'])
 t=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1)>1e-9),p['name']
assert abs(stats['height']-2.1)<1e-8
print(json.dumps(stats))
