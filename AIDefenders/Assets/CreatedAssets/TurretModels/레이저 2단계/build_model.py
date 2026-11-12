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
# Octagonal-corner square platform matching the old circular base's X/Z bounds.
BASE_WIDTH=2.1752649608029814
def platform(name,w,d,rings,mat):
 outline=profile(w,d,.19)
 loft(name,outline,rings,mat,'Base',center=(0,0,0))
 p=parts[-1];q=p['v'].copy();p['v']=np.column_stack([q[:,0],q[:,2],-q[:,1]])
platform('SquareFoundation',BASE_WIDTH,BASE_WIDTH,[(0,.93),(.055,1),(.265,1),(.32,.96)],'Navy')
platform('UpperDeck',1.98,1.98,[(.316,1),(.34,.96)],'Roof')
# Raised gold ring with an open center.
outline=profile(1.65,1.65,.17);inner=profile(1.40,1.40,.12)
v=[]
for y,s in [(.34,1),(.405,1),(.442,.96)]:
 for ring in [outline,inner]:
  v.extend([[x*s,y,z*s] for x,z in ring])
f=[]
for k in range(2):
 for i in range(8):
  j=(i+1)%8;a=k*16+i;b=k*16+j;c=b+16;d=a+16
  f.extend([[a,c,b],[a,d,c],[a+8,b+8,c+8],[a+8,c+8,d+8]])
for i in range(8):
 j=(i+1)%8;f.extend([[32+i,40+j,32+j],[32+i,40+i,40+j],[i,j,8+j],[i,8+j,8+i]])
add('GoldPlatformRing',v,f,'Gold','Base')
platform('InnerDeck',1.34,1.34,[(.34,1),(.368,1)],'Navy')
round_mesh('Bearing',[(.37,.34),(.46,.32)],'Dark','Yaw','y',(0,0,0),12)
loft('Support',profile(.38,.65,.07),[(-.19,1),(.19,1)],'Dark','Yaw',(0,.775,0))

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
# Rounded-bottom side power capsules and tapered rails.
for sign in [-1,1]:
 suffix='L' if sign<0 else 'R'
 loft('SideArmor'+suffix,profile(.13,.34,.045),[(-.53,.86),(-.49,1),(.27,1),(.32,.83)],'Navy',center=(sign*.563,1.38,0))
 loft('Rail'+suffix,profile(.19,.09,.032),[(-.57,.70),(-.52,1),(.27,1),(.34,.65)],'Edge',center=(sign*.59,1.60,0))
 # Outline on the YZ plane for the capsule; extrusion normal is +/-X.
 outline=np.array([[-.085,.16],[.085,.16],[.085,-.09],[.055,-.15],[-.035,-.16],[-.085,-.10]])[::-1]
 # Convert X,Y outline into side plane: original x -> Z, y -> Y, z -> -X (proper rotation).
 loft('PowerCapsule'+suffix,outline,[(0,.95),(.025,1),(.046,.86)],'Glow',center=(0,0,0),emissive=True)
 p=parts[-1];q=p['v'].copy();p['v']=np.column_stack([-sign*q[:,2]+sign*.682,q[:,1]+1.34,sign*q[:,0]-.045])
 round_mesh('RailBolt'+suffix,[(1.648,.045),(1.679,.040)],'Dark',axis='y',center=(sign*.59,0,-.14),n=8)
# Back details are a modest design completion of the unseen side.
loft('RearInset',profile(.68,.42,.075),[(-.714,.92),(-.704,1)],'Dark',center=(0,1.47,0))
for i in range(3):loft('CoolingSlat'+str(i),profile(.53,.042,.012),[(-.727,1),(-.715,1)],'Steel',center=(0,1.37+i*.10,0))

# Compress the upper armor slightly to accommodate the raised upgrade crest.
for p in parts[head_start:]:p['v'][:,1]=1.55+(p['v'][:,1]-1.55)*.90
# Two energy capsules and bolts per side.
extras=[]
for p in parts[head_start:]:
 if p['name'].startswith('PowerCapsule') or p['name'].startswith('RailBolt'):
  twin=dict(p);twin['v']=p['v'].copy();twin['f']=p['f'].copy()
  p['name']+='_Front';twin['name']+='_Rear'
  p['v'][:,2]+=.22;twin['v'][:,2]-=.22;extras.append(twin)
parts.extend(extras)
loft('GoldUpgradeCrest',profile(.25,.055,.015),[(-.50,.72),(-.43,1),(.17,1),(.25,.65)],'Gold','Pitch',(0,2.0725,0))
split_surfaces('GoldUpgradeCrest','GoldLight','GoldLight')
loft('StatusHousing',profile(.32,.115,.026),[(1.087,1),(1.108,.92)],'Dark','Base',(0,.16,0))
loft('StatusLight',profile(.23,.061,.018),[(1.109,1),(1.118,.94)],'Glow','Base',(0,.16,0),True)
# Generate flat-shaded mesh data and a portable OBJ.
(P/'Models').mkdir(exist_ok=True)
obj=['# Y up; +Z forward; units match the Unity model','mtllib LaserLevel2.mtl'];offset=1
for p in parts:
 obj+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 obj+=['v %.7f %.7f %.7f'%tuple(v) for v in p['v']]
 obj+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/LaserLevel2.obj').write_text('\n'.join(obj))
mtl=[]
for name,h in colors.items():
 rgb=[int(h[i:i+2],16)/255 for i in (1,3,5)]
 mtl+=['newmtl '+name,'Kd '+' '.join(map(str,rgb)),'Ks 0.12 0.12 0.12','Ns 24']
 if name=='Glow':mtl+=['Ke '+' '.join(map(str,rgb))]
(P/'Models/LaserLevel2.mtl').write_text('\n'.join(mtl))
data={'parts':[]}
for p in parts:
 vertices=p['v'][p['f'].flatten()]-(PIVOT if p['group']=='Pitch' else 0)
 data['parts'].append(dict(name=p['name'],group=p['group'],material=p['mat'],color=colors[p['mat']],emissive=p['emissive'],vertices=[dict(x=float(v[0]),y=float(v[1]),z=float(v[2])) for v in vertices],triangles=list(range(len(vertices)))))
(P/'Assets/AIDefendersLaserLevel2/Editor/LaserLevel2MeshData.json').write_text(json.dumps(data,separators=(',',':')))
vertices=np.concatenate([p['v'][np.unique(p['f'])] for p in parts]);bounds=[vertices.min(0).tolist(),vertices.max(0).tolist()]
stats={'meshParts':len(parts),'triangles':sum(len(p['f']) for p in parts),'bounds':bounds,'height':float(vertices[:,1].max()),'materials':len(colors),'forward':'+Z','up':'+Y','pitchPivot':PIVOT.tolist(),'laserOrigin':[0,1.55,1.60]}
(P/'ModelInfo.json').write_text(json.dumps(stats,indent=2))
for p in parts:
 assert np.all(np.isfinite(p['v']))
 assert p['f'].min()>=0 and p['f'].max()<len(p['v'])
 t=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1)>1e-9),p['name']
assert abs(stats['height']-2.1)<1e-8
print(json.dumps(stats))
