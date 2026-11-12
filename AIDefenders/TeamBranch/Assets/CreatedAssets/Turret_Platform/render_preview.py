from pathlib import Path
import runpy,numpy as np
from PIL import Image,ImageDraw,ImageFont
P=Path(__file__).parent
m=runpy.run_path(str(P/'build_model.py'));parts=m['parts'];colors=m['colors']
out=Image.new('RGB',(1500,850),'#e8edf1');draw=ImageDraw.Draw(out)
font='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
def f(s):return ImageFont.truetype(font,s)
draw.text((65,45),'AI DEFENDERS / UNIVERSAL TURRET PLATFORM',fill='#18364b',font=f(32))
for k,az in enumerate([55,55]):
 a=np.deg2rad(az);el=np.deg2rad(35);eye=np.array([np.cos(a)*np.cos(el),np.sin(el),np.sin(a)*np.cos(el)])
 right=np.cross([0,1,0],eye);right/=np.linalg.norm(right);up=np.cross(eye,right)
 im=np.full((630,700,3),[232,237,241],dtype=np.uint8);depth=np.full((630,700),-1e9)
 renderparts=parts
 if k==1 and (P.parent/'MG2_Upgrade_Kit/build_model.py').exists():
  turret=runpy.run_path(str(P.parent/'MG2_Upgrade_Kit/build_model.py'))['parts']
  renderparts=parts+[dict(p,v=p['v']+np.array([0,.301,0])) for p in turret]
 for p in renderparts:
  for t in p['v'][p['f']]:
   normal=np.cross(t[1]-t[0],t[2]-t[0]);normal/=np.linalg.norm(normal)
   if normal@eye<=0:continue
   q=t-[0,.60,0];v=np.stack([350+q@right*145,330-q@up*145,q@eye],axis=1)
   x0=max(0,int(v[:,0].min()));x1=min(699,int(v[:,0].max()+1));y0=max(0,int(v[:,1].min()));y1=min(629,int(v[:,1].max()+1))
   if x0>x1 or y0>y1:continue
   X,Y=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5)
   a0,b,c=v;den=(b[1]-c[1])*(a0[0]-c[0])+(c[0]-b[0])*(a0[1]-c[1])
   if abs(den)<1e-8:continue
   w=((b[1]-c[1])*(X-c[0])+(c[0]-b[0])*(Y-c[1]))/den
   u=((c[1]-a0[1])*(X-c[0])+(a0[0]-c[0])*(Y-c[1]))/den;h=1-w-u;z=w*a0[2]+u*b[2]+h*c[2]
   d=depth[y0:y1+1,x0:x1+1];mask=(w>=0)&(u>=0)&(h>=0)&(z>d)
   light=np.array([-.4,1,.7]);light/=np.linalg.norm(light);shade=.65+.35*max(0,normal@light)
   col=np.array([int(colors[p['mat']][i:i+2],16) for i in (1,3,5)])*(1.0 if p.get('emissive') else shade)
   im[y0:y1+1,x0:x1+1][mask]=col;d[mask]=z[mask]
 out.paste(Image.fromarray(im),(40+k*720,150))
 draw.text((80+k*720,140),['PLATFORM','MG2 UPGRADE / FIT EXAMPLE'][k],fill='#526372',font=f(17))
draw.text((260,800),'3.2 x 3.2 UNITS   /   FLAT MOUNT   /   SHARED TURRET FOOTPRINT',fill='#526372',font=f(18))
out.save(P/'Preview.png')
