from pathlib import Path
import runpy,numpy as np
from PIL import Image,ImageDraw,ImageFont
P=Path(__file__).parent
m=runpy.run_path(str(P/'build_model.py'));parts=m['parts'];colors=m['colors']
original=[(q['v'].copy(),q['mat']) for q in parts]
baseColors=colors.copy();frames=[]
times=[0,.06,.12,.18,.24,.29,.32,.38,.45,.55,.65,.75,1.0]
for now in times:
 colors=baseColors.copy();offset=0;scale=1
 if now<.30:
  u=now/.30;scale=.45+.75*u*u*(3-2*u)
 elif now<.35:scale=1.5;offset=-.1
 elif now<.75:
  t=(now-.35)/.40;offset=-.1*(1-t)**2;scale=max(0,(t-.35)/.65)
 for q,(v,mat) in zip(parts,original):
  q['v']=v.copy();q['mat']=mat
  if q['group']=='ChargeOrb':q['v']=(q['v']-[0,1.53,1.17])*scale+[0,1.53,1.17]
  if q['group'] in ('Pitch','ChargeOrb'):q['v'][:,2]+=offset
 if .30<=now<.35:
  colors['Glow']='#ffffff';colors['Core']='#ffffff'
 out=Image.new('RGB',(1500,850),'#e8edf1');draw=ImageDraw.Draw(out)
 font='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
 def f(s):return ImageFont.truetype(font,s)
 draw.text((65,45),'AI DEFENDERS / PLASMA LEVEL 2 / FIRING MOTION',fill='#18364b',font=f(32))
 for k,az in enumerate([55,235]):
  a=np.deg2rad(az);el=np.deg2rad(25);eye=np.array([np.cos(a)*np.cos(el),np.sin(el),np.sin(a)*np.cos(el)])
  right=np.cross([0,1,0],eye);right/=np.linalg.norm(right);up=np.cross(eye,right)
  im=np.full((630,700,3),[232,237,241],dtype=np.uint8);depth=np.full((630,700),-1e9)
  for p in parts:
   for t in p['v'][p['f']]:
    normal=np.cross(t[1]-t[0],t[2]-t[0]);length=np.linalg.norm(normal)
    if length<1e-10:continue
    normal/=length
    if normal@eye<=0:continue
    q=t-[0,1,.1];v=np.stack([350+q@right*175,330-q@up*175,q@eye],axis=1)
    x0=max(0,int(v[:,0].min()));x1=min(699,int(v[:,0].max()+1));y0=max(0,int(v[:,1].min()));y1=min(629,int(v[:,1].max()+1))
    if x0>x1 or y0>y1:continue
    X,Y=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5)
    a0,b,c=v;den=(b[1]-c[1])*(a0[0]-c[0])+(c[0]-b[0])*(a0[1]-c[1])
    if abs(den)<1e-8:continue
    w=((b[1]-c[1])*(X-c[0])+(c[0]-b[0])*(Y-c[1]))/den
    u=((c[1]-a0[1])*(X-c[0])+(a0[0]-c[0])*(Y-c[1]))/den;h=1-w-u;z=w*a0[2]+u*b[2]+h*c[2]
    d=depth[y0:y1+1,x0:x1+1];mask=(w>=0)&(u>=0)&(h>=0)&(z>d)
    light=np.array([-.4,1,.7]);light/=np.linalg.norm(light);shade=.65+.35*max(0,normal@light)
    col=np.array([int(colors[p['mat']][i:i+2],16) for i in (1,3,5)])*(1.0 if p['mat'] in ('Glow','Core') else shade)
    im[y0:y1+1,x0:x1+1][mask]=col;d[mask]=z[mask]
  out.paste(Image.fromarray(im),(40+k*720,150))
  draw.text((80+k*720,140),['FRONT / THREE-QUARTER','REAR / THREE-QUARTER'][k],fill='#526372',font=f(17))
 draw.text((260,800),'CHARGE  /  FLASH & RECOIL  /  RECOVERY  -  SOFTWARE PREVIEW',fill='#526372',font=f(18))
 frames.append(out.resize((900,510),Image.Resampling.LANCZOS))
frames[6].save(P/'FirePreview.png')
frames[0].save(P/'FirePreview.gif',save_all=True,append_images=frames[1:],duration=[60,60,60,60,50,30,50,70,100,100,100,250,450],loop=0)
print('Rendered charge / shot / recovery cycle')
