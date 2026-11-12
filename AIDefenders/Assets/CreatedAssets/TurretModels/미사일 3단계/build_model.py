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

colors.update({'Blue':'#5594B2'})
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
def shell(name,w,h,depth0,depth1,thickness,mat):
 def profile(w,h,b):
  x=w/2;t=h/2
  return [(-x+b,-t),(x-b,-t),(x,-t+b),(x,t-b),(x-b,t),(-x+b,t),(-x,t-b),(-x,-t+b)]
 outer=profile(w,h,.08);inner=profile(w-2*thickness,h-2*thickness,.045)
 v=[[a,1.55+b,z] for z in (depth0,depth1) for ring in (outer,inner) for a,b in ring];f=[]
 for i in range(8):
  j=(i+1)%8
  for a,b,c,d in [(i,j,j+16,i+16),(i+8,i+24,j+24,j+8),(i+16,j+16,j+24,i+24),(i,i+8,j+8,j)]:f.extend([[a,b,c],[a,c,d]])
 if name=='LauncherHousing':
  top=[];other=[]
  for face in f:
   tri=np.array(v)[face];normal=np.cross(tri[1]-tri[0],tri[2]-tri[0]);normal/=np.linalg.norm(normal)
   (top if normal[1]>.5 else other).append(face)
  mesh('TopArmor',v,top,'Blue','Pitch');mesh(name,v,other,mat,'Pitch')
 else:mesh(name,v,f,mat,'Pitch')


# Central armored joint linking two independent-looking four-round pods.
loft('CentralJoint',profile(.74,.70,.07),[(-.48,1),(.38,1)],'Navy','Pitch',(0,1.48,0))
loft('CentralGoldFace',profile(.55,.52,.04),[(.385,1),(.415,1)],'Gold','Pitch',(0,1.48,0))
loft('CentralGoldRoof',profile(.57,.035,.008),[(-.36,1),(.24,1)],'GoldLight','Pitch',(0,1.846,0))
pivots={'Base':np.zeros(3),'Yaw':np.zeros(3),'Pitch':PIVOT}
muzzles=[]
for side,sign in [('L',-1),('R',1)]:
 x=sign*.84;start=len(parts)
 shell('LauncherHousing',.94,.90,-.68,.43,.095,'Navy')
 box('RearArmor',.935,.895,-.74,-.675,1.55,'Navy',bevel=.07)
 box('InteriorBack',.74,.70,-.674,-.66,1.55,'Black',bevel=.04)
 shell('FrontRim',.98,.94,.43,.49,.075,'Edge')
 # Thin internal divider forms four launch cells.
 box('HorizontalDivider',.79,.025,-.60,.46,1.55,'Steel',bevel=.005)
 box('VerticalDivider',.025,.76,-.60,.46,1.55,'Steel',bevel=.005)
 # Vented top panel with a gold perimeter.
 box('TopVentBed',.76,.025,-.50,.31,2.016,'Dark',bevel=.008)
 for j in range(6):box('TopVentSlat'+str(j),.70,.028,-.45+j*.12,-.405+j*.12,2.04,'Steel',bevel=.006)
 for z in [-.55,.36]:box('RoofCrossRail'+str(z),.88,.08,z-.035,z+.035,2.06,'Gold',bevel=.02)
 for a in [-.405,.405]:
  box('RoofSideRail'+str(a),.065,.08,-.55,.36,2.06,'Gold',bevel=.018)
  parts[-1]['v'][:,0]+=a
 # Three raised rear stabilizer fins.
 for a in [-.34,0,.34]:
  v=[[a-.025,1.72,-.70],[a-.025,2.035,-.70],[a-.025,2.035,-1.02],[a+.025,1.72,-.70],[a+.025,2.035,-.70],[a+.025,2.035,-1.02]]
  f=[[0,2,1],[3,4,5],[0,1,4],[0,4,3],[1,2,5],[1,5,4],[2,0,3],[2,3,5]]
  mesh('RearFin'+str(a),v,f,'Gold','Pitch')
 # Hollow six-sided badge on the outboard panel.
 v=[]
 for xx in [sign*.472,sign*.505]:
  for r in [.20,.14]:
   for i in range(6):
    a=2*math.pi*i/6+math.pi/6;v.append([xx,1.55+r*math.sin(a),-.1+r*math.cos(a)])
 f=[]
 for i in range(6):
  j=(i+1)%6
  for a,b,c,d in [(i,j,j+12,i+12),(i+6,i+18,j+18,j+6),(i+12,j+12,j+18,i+18),(i,i+6,j+6,j)]:f.extend([[a,b,c],[a,c,d]])
 if sign<0:f=[t[::-1] for t in f]
 mesh('SideBadge',v,f,'Gold','Pitch')
 for q in parts[start:]:q['v'][:,0]+=x;q['name']+='_'+side
 for row,dy in enumerate([.19,-.19]):
  for col,dx in enumerate([-.205,.205]):
   ident=side+str(row*2+col+1);g='Missile_'+ident;cx=x+dx;cy=1.55+dy
   pivots[g]=np.array([cx,cy,0]);muzzles.append([cx,cy,.84])
   lathe('MissileBody_'+ident,[(-.48,.105),(.46,.105)],'Steel',g,'z',(cx,cy,0),10)
   lathe('Warhead_'+ident,[(.44,.108),(.55,.13),(.66,.115),(.75,.07),(.79,.025),(.80,.005)],'Gold',g,'z',(cx,cy,0),10)
   lathe('Nozzle_'+ident,[(-.56,.065),(-.48,.085)],'Dark',g,'z',(cx,cy,0),10)
loft('IndicatorHousing',profile(.32,.115,.026),[(1.087,1),(1.108,.92)],'Dark','Base',(0,.16,0))
loft('Indicator',profile(.23,.061,.018),[(1.109,1),(1.118,.94)],'Glow','Base',(0,.16,0),True)
# Portable OBJ and Unity local mesh data.
obj=['mtllib Missile3.mtl'];offset=1
for p in parts:
 obj+=['o '+p['name'],'usemtl '+p['mat'],'s off']
 obj+=['v %.7f %.7f %.7f'%tuple(q) for q in p['v']]
 obj+=['f '+' '.join(str(int(k)+offset) for k in f) for f in p['f']];offset+=len(p['v'])
(P/'Models/Missile3.obj').write_text('\n'.join(obj))
mtl=[]
for name,h in colors.items():
 rgb=[int(h[i:i+2],16)/255 for i in (1,3,5)]
 mtl+=['newmtl '+name,'Kd '+' '.join(map(str,rgb)),'Ks 0.12 0.12 0.12','Ns 24']
 if name=='Glow':mtl+=['Ke '+' '.join(map(str,rgb))]
(P/'Models/Missile3.mtl').write_text('\n'.join(mtl))
data={'parts':[]}
for p in parts:
 v=p['v'][p['f'].flatten()]-pivots[p['group']]
 data['parts'].append(dict(name=p['name'],group=p['group'],material=p['mat'],color=colors[p['mat']],emissive=p.get('emissive',False),vertices=[dict(x=float(q[0]),y=float(q[1]),z=float(q[2])) for q in v],triangles=list(range(len(v)))))
(P/'Assets/AIDefendersMissile3/Editor/Missile3MeshData.json').write_text(json.dumps(data,separators=(',',':')))
verts=np.concatenate([p['v'][np.unique(p['f'])] for p in parts])
stats={'parts':len(parts),'triangles':sum(len(p['f']) for p in parts),'bounds':[verts.min(0).tolist(),verts.max(0).tolist()],'baseWidth':BASE_WIDTH,'height':float(verts[:,1].max()),'muzzles':muzzles}
(P/'ModelInfo.json').write_text(json.dumps(stats,indent=2))
for p in parts:
 t=p['v'][p['f']];assert np.all(np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1)>1e-9),p['name']
assert abs(stats['height']-2.1)<1e-8
print(json.dumps(stats))
