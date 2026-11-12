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

def platform(name,w,d,rings,mat):
 loft(name,profile(w,d,min(.16,w/4,d/4)),rings,mat,'Base',center=(0,0,0))
 q=parts[-1]['v'].copy();parts[-1]['v']=np.column_stack([q[:,0],q[:,2],-q[:,1]])
platform('Foundation',3.2,3.2,[(0,.94),(.06,1),(.18,1),(.23,.97)],'Dark')
platform('ArmorDeck',3.08,3.08,[(.205,1),(.25,.98)],'Navy')
platform('MountingSurface',2.52,2.52,[(.25,1),(.30,.98)],'Roof')
# Gold rail segments outside the turret footprint; central top is flat at Y=.30.
for sign in [-1,1]:
 for axis in ['x','z']:
  platform('Rail'+axis+str(sign),.10,2.4,[(.25,1),(.275,1)],'Gold')
  q=parts[-1]['v']
  if axis=='x':q[:,0]+=sign*1.40
  else:
   q[:,[0,2]]=q[:,[2,0]];q[:,2]+=sign*1.40
   parts[-1]['f']=parts[-1]['f'][:,::-1]
for x in [-1.35,1.35]:
 for z in [-1.35,1.35]:
  round_mesh('Anchor_%s_%s'%(x,z),[(.25,.09),(.28,.09)],'Steel','Base','y',(x,0,z),6)
# Four slim cyan status bars on the outer edge.
for axis in ['x','z']:
 for sign in [-1,1]:
  platform('Status'+axis+str(sign),.07,.62,[(.253,1),(.267,1)],'Glow')
  q=parts[-1]['v']
  if axis=='x':q[:,0]+=sign*1.52
  else:
   q[:,[0,2]]=q[:,[2,0]];q[:,2]+=sign*1.52
   parts[-1]['f']=parts[-1]['f'][:,::-1]
  parts[-1]['emissive']=True
# Flush installation cross, concealed by installed turret.
for axis in ['x','z']:
 platform('PlacementMark'+axis,.035,.60,[(.300,1),(.301,1)],'GoldLight')
 if axis=='z':
  q=parts[-1]['v'];q[:,[0,2]]=q[:,[2,0]];parts[-1]['f']=parts[-1]['f'][:,::-1]
# Portable OBJ and Unity local mesh data.
obj=['mtllib TurretPlatform.mtl'];offset=1
for p in parts:
 obj+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 obj+=['v %.7f %.7f %.7f'%tuple(q) for q in p['v']]
 obj+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/TurretPlatform.obj').write_text('\n'.join(obj))
mtl=[]
for name,h in colors.items():
 rgb=[int(h[i:i+2],16)/255 for i in (1,3,5)]
 mtl+=['newmtl '+name,'Kd '+' '.join(map(str,rgb)),'Ks 0.12 0.12 0.12','Ns 24']
 if name=='Glow':mtl+=['Ke '+' '.join(map(str,rgb))]
(P/'Models/TurretPlatform.mtl').write_text('\n'.join(mtl))
pivots={'Base':np.zeros(3),'Yaw':np.zeros(3),'Pitch':PIVOT,'Recoil_L':np.array([-.4,1.5,0]),'Recoil_R':np.array([.4,1.5,0])}
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]-pivots[p['group']]
 data['parts'].append(dict(name=p['name'],group=p['group'],material=p['mat'],color=colors[p['mat']],emissive=p['emissive'],vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersPlatform/Editor/TurretPlatformMeshData.json').write_text(json.dumps(data,separators=(',',':')))
verts=np.concatenate([p['v'][np.unique(p['f'])] for p in parts])
stats={'parts':len(parts),'triangles':sum(len(p['f']) for p in parts),'bounds':[verts.min(0).tolist(),verts.max(0).tolist()],'mountPoint':[0,.301,0]}
(P/'ModelInfo.json').write_text(json.dumps(stats,indent=2))
for p in parts:
 t=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1)>1e-9),p['name']
 assert np.sum(np.einsum('ij,ij->i',t[:,0],np.cross(t[:,1],t[:,2])))/6>0,p['name']
print(json.dumps(stats))
