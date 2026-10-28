"""Rebuild the MG2 upgrade game mesh with numpy (Y up, +Z forward)."""
from pathlib import Path
import json, math
import numpy as np
P=Path(__file__).resolve().parent
colors={'Navy':'#264A63','Roof':'#5594B2','Edge':'#78ADC1','Steel':'#405969','Dark':'#243541','Black':'#10242F','Gold':'#F1AC3D','GoldLight':'#FFD36C','Glow':'#15E5DC'}
parts=[]
PIVOT=np.array([0.,1.50,0.])
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
loft('TwinReceiverBridge',profile(1.36,.28,.055),[(-.36,1),(.15,1)],'Dark','Pitch',(0,1.17,0))
# Closed square hollow tube with beveled corners.
def square_tube(name,w,h,z0,z1,wall,mat,group,x):
 outer=profile(w,h,min(.025,w/8));inner=profile(w-2*wall,h-2*wall,.012)
 v=[[a+x,b+1.5,z] for z in [z0,z1] for ring in [outer,inner] for a,b in ring];f=[]
 for i in range(8):
  j=(i+1)%8
  for a,b,c,d in [(i,j,j+16,i+16),(i+8,i+24,j+24,j+8),(i+16,j+16,j+24,i+24),(i,i+8,j+8,j)]:f.extend([[a,b,c],[a,c,d]])
 add(name,v,f,mat,group)
for side,sign in [('L',-1),('R',1)]:
 x=sign*.40;group='Recoil_'+side
 loft('FrontHousing'+side,profile(.76,.85,.105),[(-.43,.96),(-.37,1),(.40,1),(.46,.94)],'Navy','Pitch',(x,1.515,0))
 split_surfaces('FrontHousing'+side,'Roof','Roof')
 loft('RearHousing'+side,profile(.76,.92,.115),[(-.80,.93),(-.735,1),(-.21,1),(-.16,.95)],'Navy','Pitch',(x,1.64,0))
 split_surfaces('RearHousing'+side,'Roof','Roof')
 loft('ArmorBand'+side,profile(.77,.855,.105),[(-.193,1),(-.163,1)],'Edge','Pitch',(x,1.515,0))
 loft('FaceRim'+side,profile(.745,.82,.10),[(.455,.97),(.482,1),(.502,.97)],'Edge','Pitch',(x,1.515,0))
 loft('GoldMantlet'+side,profile(.66,.73,.10),[(.500,.95),(.53,1),(.589,1),(.612,.92)],'Gold','Pitch',(x,1.50,0))
 split_surfaces('GoldMantlet'+side,'GoldLight','GoldLight')
 loft('Breech'+side,profile(.44,.47,.055),[(.61,1),(.66,1)],'Dark','Pitch',(x,1.5,0))
 loft('BarrelCollar'+side,profile(.35,.36,.035),[(.657,1),(.709,1)],'Steel',group,(x,1.5,0))
 square_tube('SquareBarrel'+side,.24,.25,.70,1.46,.043,'Steel',group,x)
 square_tube('MuzzleBlock'+side,.315,.30,1.35,1.615,.055,'Dark',group,x)
 square_tube('MuzzleLip'+side,.302,.287,1.615,1.655,.051,'Steel',group,x)
 loft('BoreShadow'+side,profile(.20,.18,.012),[(1.36,1),(1.37,1)],'Black',group,(x,1.5,0))
 round_mesh('GoldTopBolt'+side,[(1.941,.064),(1.978,.064),(2.000,.038)],'Gold','Pitch','y',(x,0,.03),6)
 loft('SideMagazine'+side,profile(.18,.25,.025),[(-.50,.9),(-.47,1),(.04,1),(.08,.8)],'Dark','Pitch',(sign*.815,1.42,0))
 loft('MagazineLowerLip'+side,profile(.12,.09,.015),[(-.34,1),(-.13,1)],'Steel','Pitch',(sign*.835,1.26,0))
 for j in range(2):
  loft('RearVent'+side+str(j),profile(.42,.055,.015),[(-.808,1),(-.796,1)],'Dark','Pitch',(x,1.51+j*.14,0))
loft('IndicatorHousing',profile(.32,.115,.026),[(1.087,1),(1.108,.92)],'Dark','Base',(0,.16,0))
loft('Indicator',profile(.23,.061,.018),[(1.109,1),(1.118,.94)],'Glow','Base',(0,.16,0),True)
# Portable OBJ and Unity local mesh data.
obj=['mtllib MG2Upgrade.mtl'];offset=1
for p in parts:
 obj+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 obj+=['v %.7f %.7f %.7f'%tuple(q) for q in p['v']]
 obj+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/MG2Upgrade.obj').write_text('\n'.join(obj))
mtl=[]
for name,h in colors.items():
 rgb=[int(h[i:i+2],16)/255 for i in (1,3,5)]
 mtl+=['newmtl '+name,'Kd '+' '.join(map(str,rgb)),'Ks 0.12 0.12 0.12','Ns 24']
 if name=='Glow':mtl+=['Ke '+' '.join(map(str,rgb))]
(P/'Models/MG2Upgrade.mtl').write_text('\n'.join(mtl))
pivots={'Base':np.zeros(3),'Yaw':np.zeros(3),'Pitch':PIVOT,'Recoil_L':np.array([-.4,1.5,0]),'Recoil_R':np.array([.4,1.5,0])}
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]-pivots[p['group']]
 data['parts'].append(dict(name=p['name'],group=p['group'],material=p['mat'],color=colors[p['mat']],emissive=p['emissive'],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersMG2Upgrade/Editor/MG2UpgradeMeshData.json').write_text(json.dumps(data,separators=(',',':')))
verts=np.concatenate([p['v'][np.unique(p['f'])] for p in parts])
stats={'parts':len(parts),'triangles':sum(len(p['f']) for p in parts),'bounds':[verts.min(0).tolist(),verts.max(0).tolist()],'baseWidth':BASE_WIDTH,'height':float(verts[:,1].max()),'muzzles':[[-.4,1.5,1.68],[.4,1.5,1.68]]}
(P/'ModelInfo.json').write_text(json.dumps(stats,indent=2))
for p in parts:
 t=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1)>1e-9),p['name']
assert abs(stats['height']-2.1)<1e-8
print(json.dumps(stats))
