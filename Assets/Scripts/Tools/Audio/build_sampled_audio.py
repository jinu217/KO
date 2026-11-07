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
WORK=ROOT/'KO_Audio_Rebuild'; OUT=WORK/'rendered'; OUT.mkdir(exist_ok=True)
RATE=48000; BEAT=.625; N=60*RATE
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
# Recorded actions, edited with minimal filtering; no synthesized noise or pitched UI chirps.
steel=norm(filt(segment(blade,0,.34),160,6500),1)
lock=norm(filt(segment(latch,0,.40),150,7000),1)
lightlock=norm(filt(segment(small,0,.24),180,7200),1)
cloth=norm(filt(cloth,180,8000),1)
pen=norm(filt(pen,280,9000),1)
def layer(base,other,gain,delay=0):
 start=round(delay*RATE); count=min(len(other),len(base)-start)
 if count>0: base[start:start+count]+=other[:count]*gain
 return base
ui={}
ui['TitleNavigate']=fade(segment(steel,0,.34),.002,.08)
ui['CategoryNavigate']=fade(segment(lightlock,0,.15),.002,.035)
ui['DisplayAdjust']=fade(segment(lock,.03,.18),.002,.035)
ui['SlotConfirm']=layer(np.zeros(round(.43*RATE)),lock,.85)
layer(ui['SlotConfirm'],steel,.16,.025)
ui['DeleteConfirm']=layer(np.zeros(round(.46*RATE)),lock,.65)
layer(ui['DeleteConfirm'],segment(small,.18,.21),.5,.09)
ui['Cancel']=fade(segment(cloth,.58,.24,1.12),.015,.07)
ui['DialogOpen']=fade(segment(cloth,.28,.28,1.06),.01,.07)
layer(ui['DialogOpen'],lightlock,.07,.1)
ui['SettingsOpen']=fade(segment(cloth,.29,.36,.97),.012,.08)
layer(ui['SettingsOpen'],lightlock,.06,.16)
# Natural brush-like fabric sweep plus a very quiet bowed sample for luminous weight.
ui['SlotNavigate']=fade(segment(cloth,.32,.36,.96),.025,.08)
bow=note(48,62,.22,55).mean(axis=1)
layer(ui['SlotNavigate'],norm(fade(segment(bow,.05,.36),.05,.12),1),.045)
ui['VolumeAdjust']=fade(segment(pen,16.66,.13),.004,.025)
peaks={'TitleNavigate':.27,'SlotNavigate':.17,'SlotConfirm':.34,'Cancel':.18,'CategoryNavigate':.21,'DisplayAdjust':.21,'DeleteConfirm':.31,'DialogOpen':.19,'SettingsOpen':.20,'VolumeAdjust':.17}
order=['TitleNavigate','SlotNavigate','SlotConfirm','Cancel','CategoryNavigate','DisplayAdjust','DeleteConfirm','DialogOpen','SettingsOpen','VolumeAdjust']
preview=[]
for name in order:
 ui[name]=norm(fade(ui[name],.001,.015),peaks[name]); write(name,ui[name]); preview.extend((ui[name],np.zeros(round(.5*RATE))))
write('UI_Preview',np.concatenate(preview))
# Same 60-second / 96 BPM arrangement the user preferred, with sampled instruments.
mix=np.zeros((N,2))
def add(a,start,gain,pan=.5):
 idx=(round(start*RATE)+np.arange(len(a)))%N
 a=a.copy(); a[:,0]*=math.sqrt((1-pan)*2);a[:,1]*=math.sqrt(pan*2)
 mix[idx]+=a*gain
roots=[38,34,31,33]; chords=[(50,53,57),(46,50,53),(43,46,50),(45,49,55)]
melody=[(62,65,64,57),(58,62,60,57),(55,58,62,60),(61,64,67,61)]
for bar in range(24):
 start=bar*4*BEAT; ci=bar%4; section=bar//4; strength=.83 if section in (0,3) else 1
 for beat,gain in [(0,.7),(1.5,.42),(2,.6),(3.5,.36)]: add(note(0,36,.08,92,True),start+beat*BEAT,gain*strength)
 for beat in (1,3): add(note(0,38,.08,77,True),start+beat*BEAT,.22*strength,.54)
 if bar%4==3:
  for j,beat in enumerate((3.25,3.5,3.75)): add(note(0,45-j*2,.08,72,True),start+beat*BEAT,.19,.38)
 for beat,key in [(0,roots[ci]),(1.5,roots[ci]),(2,roots[ci]),(3,roots[ci]+7)]: add(note(33,key,.32,85),start+beat*BEAT,.40)
 for j,k in enumerate((0,2,1,2,0,2,1,2)):
  key=chords[ci][k]+(12 if section in (2,5) and j>=4 else 0)
  # Dry nylon pick with a little pizzicato body instead of a ringing sine arpeggio.
  add(note(24,key,.20,78),start+j*.5*BEAT,.30*strength,.32 if j%2==0 else .68)
  add(note(45,key,.16,62),start+j*.5*BEAT,.07*strength,.58)
 for j,key in enumerate(chords[ci]): add(note(48,key,1.95,57),start,.045,.2+j*.3)
 if section in (1,2,4,5):
  for j,key in enumerate(melody[ci]): add(note(42,key,.55 if j<3 else .8,73),start+j*BEAT,.19,.48)
# Recorded steel marks phrase transitions, with the user's slight loudness increase.
for start,speed,gain,pan in [(9.375,.88,.085,.4),(9.6875,.94,.06,.6),(10,.90,.135,.5),(29.375,.88,.08,.6),(29.6875,.94,.06,.4),(30,.90,.128,.5),(49.375,.88,.085,.4),(49.6875,.94,.06,.6),(50,.90,.135,.5)]:
 a=norm(fade(filt(segment(blade,0,.45,speed),240,4500),.003,.09),1)
 add(np.repeat(a[:,None],2,axis=1),start,gain)
# Restrained room, no master saturation; circular tails preserve the loop.
room=mix.copy()
for delay,gain in [(.079,.055),(.151,.04),(.3125,.035)]: room+=np.roll(mix[:,::-1],round(delay*RATE),axis=0)*gain
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
room=norm(room,.72);write('KO_Title_Sampled',room)
report={}
for path in OUT.glob('*.wav'):
 a,sr=sf.read(path,always_2d=True);peak=float(np.abs(a).max());rms=float(np.sqrt(np.mean(a*a)))
 assert np.isfinite(a).all() and peak<1 and rms>0
 report[path.stem]={'seconds':len(a)/sr,'rate':sr,'channels':a.shape[1],'peak':peak,'rms':rms}
report['loop_jump']=float(np.max(np.abs(room[0]-room[-1])))
(OUT/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report),flush=True)