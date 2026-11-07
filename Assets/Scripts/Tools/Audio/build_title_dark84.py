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
WORK=ROOT/'KO_Audio_Rebuild'; OUT=WORK/'dark_title'; OUT.mkdir(exist_ok=True)
RATE=48000; BPM=84; BEAT=60/BPM; N=round(24*4*BEAT*RATE)
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
# Same 60-second / 96 BPM arrangement the user preferred, with sampled instruments.
mix=np.zeros((N,2))
def add(a,start,gain,pan=.5):
 idx=(round(start*RATE)+np.arange(len(a)))%N
 a=a.copy(); a[:,0]*=math.sqrt((1-pan)*2);a[:,1]*=math.sqrt(pan*2)
 mix[idx]+=a*gain
roots=[38,34,31,33]; chords=[(50,53,57),(46,50,53),(43,46,50),(45,49,55)]
melody=[(62,65,64,57),(58,62,60,57),(55,58,62,60),(61,64,67,61)]
for bar in range(24):
 start=bar*4*BEAT; ci=(bar//2)%4; section=bar//8; strength=.78 if bar<4 or 16<=bar<18 else 1
 for beat,gain in [(0,.65),(2,.44),(3.5,.23)]: add(note(0,36,.08,92,True),start+beat*BEAT,gain*strength)
 for beat in (2,): add(note(0,38,.08,62,True),start+beat*BEAT,.12*strength,.54)
 if bar%8==7:
  for j,beat in enumerate((3.25,3.5,3.75)): add(note(0,45-j*2,.08,72,True),start+beat*BEAT,.12,.38)
 for beat,key in [(0,roots[ci]-2),(1.5,roots[ci]-2),(2.75,roots[ci]+5)]: add(note(33,key,.45,76),start+beat*BEAT,.34)
 for j,k in enumerate((0,2,1,2,0,2,1,2)):
  key=chords[ci][k]-2
  # Dry nylon pick with a little pizzicato body instead of a ringing sine arpeggio.
  add(note(24,key,.19,60),start+j*.5*BEAT,.065*strength,.32 if j%2==0 else .68)
  add(note(45,key,.21,72),start+j*.5*BEAT,.15*strength,.58)
 for j,key in enumerate(chords[ci]): add(note(48,key-2,2.55,53),start,.065,.2+j*.3)
 add(note(43,roots[ci]-2,2.2,57),start,.075,.5)
 if bar>=4 and not 16<=bar<18:
  for j,key in enumerate(melody[ci]): add(note(42,key-2,.65 if j<3 else .95,64),start+j*BEAT,.16,.48)
# Recorded steel marks phrase transitions, with the user's slight loudness increase.
for start,speed,gain,pan in [((bar*4+beat)*BEAT,speed,gain,pan) for bar in (7,15,23) for beat,speed,gain,pan in [(3,.86,.075,.4),(3.5,.92,.05,.6),(4,.88,.12,.5)]]:
 a=norm(fade(filt(segment(blade,0,.45,speed),240,4500),.003,.09),1)
 add(np.repeat(a[:,None],2,axis=1),start,gain)
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
gain=np.minimum(1,(.11/np.maximum(level,.00001))**.45)
gain=uniform_filter1d(gain,size=97,mode='wrap')
room*=gain[:,None]
room=norm(room,.72);write('KO_Title_Dark84',room)
report={}
for path in OUT.glob('*.wav'):
 a,sr=sf.read(path,always_2d=True);peak=float(np.abs(a).max());rms=float(np.sqrt(np.mean(a*a)))
 assert np.isfinite(a).all() and peak<1 and rms>0
 report[path.stem]={'seconds':len(a)/sr,'rate':sr,'channels':a.shape[1],'peak':peak,'rms':rms}
report['loop_jump']=float(np.max(np.abs(room[0]-room[-1])))
(OUT/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report),flush=True)