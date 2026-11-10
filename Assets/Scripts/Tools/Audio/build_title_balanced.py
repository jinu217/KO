"""KO sample-based rebuild: approved 96 BPM composition + recorded Foley UI.
Run with bundled Python; dependencies and licensed source files live in WORK.
"""
import sys,json,math,shutil
from pathlib import Path
ROOT=Path('C:/Users/User/.codex/visualizations/2026/10/19/01a151a7-b3fb-7f30-b065-52195249c086')
sys.path.insert(0,str(ROOT/'audio_dependencies'))
import numpy as np
import soundfile as sf
from scipy.signal import butter,sosfilt,resample_poly
from scipy.ndimage import maximum_filter1d,uniform_filter1d
from tinysoundfont import Synth
WORK=ROOT/'KO_Audio_Rebuild'; OUT=WORK/'final_title_balanced'; OUT.mkdir(exist_ok=True)
RATE=48000; BPM=92; BEAT=60/BPM; N=round(24*4*BEAT*RATE)
synth=Synth(samplerate=RATE,gain=-8); font=synth.sfload(str(WORK/'GeneralUser-GS.sf2'))
cache={}
def note(program,key,hold=.25,velocity=85,drums=False):
 identity=(program,key,hold,velocity,drums)
 if identity not in cache:
  synth.sounds_off(); ch=9 if drums else 0
  synth.program_select(ch,font,128 if drums else 0,program,is_drums=drums)
  synth.noteon(ch,key,velocity)
  a=np.frombuffer(synth.generate(round(hold*RATE)),dtype=np.float32).copy().reshape(-1,2)
  synth.noteoff(ch,key)
  b=np.frombuffer(synth.generate(round(1.2*RATE)),dtype=np.float32).copy().reshape(-1,2)
  cache[identity]=np.concatenate((a,b)).astype(np.float64)
 return cache[identity].copy()
def filt(a,low=40,high=9000):
 return sosfilt(butter(2,[low,high],btype='bandpass',fs=RATE,output='sos'),a,axis=0)
def load(path):
 a,rate=sf.read(path,always_2d=True); a=a.mean(axis=1)
 if rate!=RATE:
  g=math.gcd(rate,RATE); a=resample_poly(a,RATE//g,rate//g)
 return a

def segment(a,start,length,speed=1):
 n=round(length*RATE); positions=start*RATE+np.arange(n)*speed
 return np.interp(positions,np.arange(len(a)),a,left=0,right=0)
def fade(a,attack=.003,release=.04):
 n=len(a); env=np.minimum(1,np.arange(n)/(RATE*attack))*np.minimum(1,(n-1-np.arange(n))/(RATE*release))
 return a*env if a.ndim==1 else a*env[:,None]
def norm(a,peak): return a*peak/max(.00001,float(np.max(np.abs(a))))
def write(name,a): sf.write(OUT/(name+'.wav'),a,RATE,subtype='PCM_24')
blade=load(Path('C:/Unity/KO/Assets/Audio/Sources/BladeImpact_UI_Base.wav'))
cloth=load(WORK/'Cloth.mp3'); latch=load(WORK/'Latch.mp3'); small=load(WORK/'SmallLatch.mp3'); pen=load(WORK/'Pen.mp3')
# Final 92 BPM title arrangement, 24 bars, sampled instruments.
mix=np.zeros((N,2))
def add(a,start,gain,pan=.5,target=None):
 if target is None: target=mix
 idx=(round(start*RATE)+np.arange(len(a)))%N
 a=a.copy(); a[:,0]*=math.sqrt((1-pan)*2);a[:,1]*=math.sqrt(pan*2)
 target[idx]+=a*gain
# Original two-bar hook: short-short-held, then an unresolved falling answer.
# C minor -> Ab -> F minor -> G tension, two bars each.
roots=[36,32,29,31]
chords=[(48,51,55),(44,48,51),(41,44,48),(43,50,56)]
hooks=[(60,63,62,55,58,55,59,60),(60,63,62,56,58,56,55,56),
       (60,63,62,53,56,55,56,53),(59,62,61,55,56,55,59,55)]
for bar in range(24):
 start=bar*4*BEAT; ci=(bar//2)%4; section=bar//8
 thin=16<=bar<18
 strength=.82 if bar<4 else (.58 if thin else 1)
 # Weighted pulse with an anticipatory kick: deliberate but still moving forward.
 for beat,g in [(0,.49),(1.5,.20),(2,.33),(3.5,.17)]:
  add(note(0,36,.075,86,True),start+beat*BEAT,g*strength*.50)
 for beat in (1,3):
  if thin and beat==1: continue
  add(note(0,38,.08,62,True),start+beat*BEAT,.135*strength*.50,.53)
 if bar%8==7:
  for beat,key in [(3,45),(3.5,43),(3.75,41)]:
   add(note(0,key,.08,66,True),start+beat*BEAT,.13*.50,.4)
 # 3+3+2 eighth-note accents bind the bass and low string figure.
 for beat,key,g in [(0,roots[ci],.23),(1.5,roots[ci],.17),(3,roots[ci]+7,.15)]:
  add(note(33,key,.38,76),start+beat*BEAT,g*strength)
  add(note(45,key+12,.20,67),start+beat*BEAT,.095*strength,.42)
 if not thin:
  for j,k in enumerate((0,2,1,2,0,2,1,2)):
   accent=1 if j in (0,3,6) else .62
   add(note(45,chords[ci][k],.17,64),start+j*.5*BEAT,.065*accent,.36 if j%2==0 else .64)
 # Bowed low bed supplies weight; low organ is only an unobtrusive foundation.
 if bar%2==0:
  add(note(43,roots[ci],4.05,54),start,.10,.5)
  for j,key in enumerate(chords[ci]): add(note(49,key,3.6,49),start,.041,.2+j*.3)
  add(note(19,roots[ci]+12,3.65,36),start,.018,.5)
 # The hook is present immediately and comes back with the same rhythmic identity.
 phrase=hooks[ci]
 seq=[(0,phrase[0],.28),(.75,phrase[1],.28),(1.5,phrase[2],.78),(3,phrase[3],.45)] if bar%2==0 else [(0,phrase[4],.40),(1.25,phrase[5],.38),(2,phrase[6],.38),(3,phrase[7],.65)]
 lead=.16 if section==0 else (.19 if section==1 else .15)
 if thin: seq=seq[:2];lead=.11
 for beat,key,hold in seq:
  add(note(42,key,hold,64),start+beat*BEAT,lead*1.4,.49)
  # Dry low guitar doubles the onset quietly, avoiding a bright synthetic lead.
  add(note(24,key-12,.16,56),start+beat*BEAT,.048*1.15,.57)
 if section==1 and bar%2==1:
  add(note(42,phrase[-1]-12,1.15,53),start+2.5*BEAT,.065,.4)
# Steel appears only at internal transitions. No final-bar hits can wrap into the intro.
steel=np.zeros_like(mix)
for start,speed,gain,pan in [((bar*4+beat)*BEAT,speed,gain,pan) for bar in (7,15) for beat,speed,gain,pan in [(3,.86,.075,.4),(3.5,.92,.05,.6),(4,.88,.12,.5)]]:
 a=norm(fade(filt(segment(blade,0,.45,speed),240,4500),.003,.09),1)
 add(np.repeat(a[:,None],2,axis=1),start,gain*.15,target=steel)
assert np.count_nonzero(steel[:round(16*RATE)])==0, "Steel leaked into intro"
assert np.count_nonzero(steel[-round(16*RATE):])==0, "Steel can wrap across loop"
mix+=steel
# Restrained room, no master saturation; circular tails preserve the loop.
room=mix.copy()
for delay,gain in [(.095,.065),(.187,.045),(.5*BEAT,.035)]: room+=np.roll(mix[:,::-1],round(delay*RATE),axis=0)*gain
room=filt(room,32,15000); room-=room.mean(axis=0)
# Short end-to-start crossfade removes residual filter startup mismatch.
xf=round(.012*RATE); anchor=(room[-1]+room[0])*.5
for j in range(xf):
 w=(1-j/xf)**2; room[j]=room[j]*(1-w)+anchor*w; room[-1-j]=room[-1-j]*(1-w)+anchor*w
# Gentle gain reduction catches isolated drum peaks without saturating the master.
level=maximum_filter1d(np.max(np.abs(room),axis=1),size=481,mode='wrap')
gain=np.minimum(1,(.075/np.maximum(level,.00001))**.48)
gain=uniform_filter1d(gain,size=97,mode='wrap')
room*=gain[:,None]
room=norm(room,.72)
reference,_=sf.read(WORK/'final_title/KO_Title_Final.wav',always_2d=True)
match=round(16*RATE)
room*=np.sqrt(np.mean(reference[:match]**2)/np.mean(room[:match]**2))
write('KO_Title_Final',room)
report={}
for path in OUT.glob('*.wav'):
 a,sr=sf.read(path,always_2d=True);peak=float(np.abs(a).max());rms=float(np.sqrt(np.mean(a*a)))
 assert np.isfinite(a).all() and peak<1 and rms>0
 report[path.stem]={'seconds':len(a)/sr,'rate':sr,'channels':a.shape[1],'peak':peak,'rms':rms}
report['intro_steel_peak']=float(np.max(np.abs(steel[:round(16*RATE)])))
report['loop_jump']=float(np.max(np.abs(room[0]-room[-1])))
(OUT/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report),flush=True)