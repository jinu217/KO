"""Original rhythmic KO title theme. 96 BPM, 24 bars, seamless circular mix."""
from pathlib import Path
import numpy as np
import wave
RATE=48000
BPM=108
BEAT=60/BPM
DURATION=24*4*BEAT
N=round(RATE*DURATION)
mix=np.zeros((N,2)); rng=np.random.default_rng(711)
def hz(note): return 440*2**((note-69)/12)
def add(signal,start,gain=1,pan=.5):
 idx=(int(start*RATE)+np.arange(len(signal)))%N
 mix[idx,0]+=signal*gain*np.sqrt(1-pan)
 mix[idx,1]+=signal*gain*np.sqrt(pan)
def tone(note,length,style):
 t=np.arange(int(length*RATE))/RATE; f=hz(note)
 if style=='pluck':
  v=sum(np.sin(2*np.pi*f*k*t+.018*np.sin(2*np.pi*4.7*t))*np.exp(-t*k*.9)/(k**1.25) for k in range(1,7))
  env=(1-np.exp(-t*110))*np.exp(-t*7)*np.minimum(1,(length-t)/.045)
 elif style=='bass':
  v=np.sin(2*np.pi*f*t)+.28*np.sin(2*np.pi*f*2*t)+.09*np.sin(2*np.pi*f*3*t)
  env=(1-np.exp(-t*90))*np.exp(-t*2)*np.minimum(1,(length-t)/.06)
 else:
  v=sum((np.sin(2*np.pi*f*k*t+.10*np.sin(2*np.pi*4.5*t))+ .20*np.sin(2*np.pi*f*k*1.0005*t))/(k**1.8) for k in range(1,6))
  env=(1-np.exp(-t*20))*np.exp(-t/(length*.65))*np.minimum(1,(length-t)/.2)
 return v*env
# Deep, soft-edged drums rather than bright cymbals.
t=np.arange(int(.65*RATE))/RATE
kick=np.sin(2*np.pi*(48*t+45*.025*(1-np.exp(-t/.025))))*np.exp(-t*10)*(1-np.exp(-t*800))
noise=rng.standard_normal(len(t)); spectrum=np.fft.rfft(noise); f=np.fft.rfftfreq(len(t),1/RATE)
noise=np.fft.irfft(spectrum*np.exp(-(f/2100)**4)*(1-np.exp(-(f/150)**2)),n=len(t))
snare=(noise*.45+np.sin(2*np.pi*150*t)*.3)*np.exp(-t*19)*(1-np.exp(-t*700))
tom=np.sin(2*np.pi*(80*t+22*.04*(1-np.exp(-t/.04))))*np.exp(-t*9)*(1-np.exp(-t*400))
# Six four-bar phrases: D minor, Bb, G minor, A tension; original melody.
roots=[38,34,31,33]
chords=[(50,53,57),(46,50,53),(43,46,50),(45,49,55)]
melody=[(62,65,64,57),(58,62,60,57),(55,58,62,60),(61,64,67,61)]
for bar in range(24):
 start=bar*4*BEAT; section=bar//8; ci=(bar//2)%4; root=roots[ci]
 intensity=.78 if bar<4 or 16<=bar<18 else 1
 for beat,g in [(0,.25),(.75,.10),(1.5,.15),(2,.20),(2.75,.105),(3.5,.13)]: add(kick,start+beat*BEAT,g*intensity)
 for beat in (1,3): add(snare,start+beat*BEAT,.21*intensity,.54)
 if bar%4==3:
  for beat in (3.25,3.5,3.75): add(tom,start+beat*BEAT,.065,.38)
 for beat,note in [(0,root),(.75,root),(1.5,root+12),(2,root),(2.75,root),(3.5,root+7)]: add(tone(note,.38,'bass'),start+beat*BEAT,.125*intensity)
 pattern=[0,2,1,2,0,1,2,1]
 for j,k in enumerate(pattern):
  note=chords[ci][k]+(12 if section==2 and j in (3,7) else 0)
  add(tone(note,.32,'pluck'),start+j*.5*BEAT,.064*intensity,.30 if j%2==0 else .70)
 # Held harmony under the pulse.
 for j,note in enumerate(chords[ci]): add(tone(note,3.1,'string'),start,.018,.2+j*.3)
 if bar>=4 and not 16<=bar<18:
  for j,note in enumerate(melody[ci]):
   add(tone(note,.95 if j<3 else 1.2,'string'),start+(j+([0,.25,0,-.25][j]))*BEAT,.061,.48)
# Dry ticking percussion and tom answers reinforce momentum without bright cymbals.
ht=np.arange(int(.12*RATE))/RATE
hn=rng.standard_normal(len(ht)); hf=np.fft.rfftfreq(len(ht),1/RATE)
hat=np.fft.irfft(np.fft.rfft(hn)*np.exp(-(hf/4800)**4)*(1-np.exp(-(hf/1800)**2)),n=len(ht))
hat*=np.exp(-ht*55)*(1-np.exp(-ht*1100))
for bar in range(24):
 for j in range(8):
  if 16<=bar<18 and j%2==0: continue
  add(hat,(bar*4+j*.5)*BEAT,.025 if j%2 else .017,.35 if j%2 else .65)
 if bar%8==7:
  for j,note in enumerate((43,41,38)):
   add(tone(note,.32,'bass'),(bar*4+3+j/3)*BEAT,.09,.3+j*.2)
# Soft wide echoes and room, wrapping tails for looping.
wet=mix.copy()
for delay,gain in [(.094,.045),(.217,.035),(.3125,.065),(.625,.025)]:
 wet+=np.roll(mix[:,::-1],int(delay*RATE),axis=0)*gain
wet-=wet.mean(axis=0)
# Keep the master linear to avoid saturation harmonics.
# Recorded CC0 blade impact, short softened tails; three distant exchanges.
with wave.open('C:/Unity/KO/Assets/Audio/Sources/BladeImpact_UI_Base.wav','rb') as source:
 raw=np.frombuffer(source.readframes(source.getnframes()),dtype='<i2').astype(np.float64)/32768
 channels=source.getnchannels(); source_rate=source.getframerate()
 raw=raw.reshape(-1,channels).mean(axis=1)
fx=np.zeros_like(wet)
for start,speed,gain,pan in [((bar*4+beat)*BEAT,speed,gain,pan) for bar in (7,15,23) for beat,speed,gain,pan in [(3,.88,.07,.4),(3.5,.94,.05,.6),(4,.90,.105,.5)]]:
 count=int(len(raw)/source_rate/speed*RATE)
 hit=np.interp(np.arange(count)/RATE*source_rate*speed,np.arange(len(raw)),raw)
 hit/=max(.001,np.abs(hit).max())
 f=np.fft.rfftfreq(count,1/RATE)
 hit=np.fft.irfft(np.fft.rfft(hit)*np.exp(-(f/2900)**4)*(1-np.exp(-(f/380)**2)),n=count)
 hit*=np.exp(-np.arange(count)/RATE/.20)*np.minimum(1,np.arange(count)/(RATE*.004))*np.minimum(1,(count-1-np.arange(count))/(RATE*.065))
 idx=(int(start*RATE)+np.arange(count))%N
 fx[idx,0]+=hit*gain*np.sqrt(1-pan); fx[idx,1]+=hit*gain*np.sqrt(pan)
wet+=fx+np.roll(fx[:,::-1],int(.3125*RATE),axis=0)*.065+np.roll(fx,int(.625*RATE),axis=0)*.025

wet*=.75/np.max(np.abs(wet))
out=Path('C:/Users/User/.codex/visualizations/2026/10/19/01a151a7-b3fb-7f30-b065-52195249c086/KO_Title_BGM_Action'); out.mkdir(parents=True,exist_ok=True)
with wave.open(str(out/'KO_Title_Action.wav'),'wb') as w:
 w.setparams((2,2,RATE,0,'NONE','not compressed')); w.writeframes((wet*32767).astype('<i2').tobytes())
print('seconds',DURATION,'BPM',BPM,'peak',float(np.abs(wet).max()),'RMS',float(np.sqrt(np.mean(wet**2))))
assert np.isfinite(wet).all() and np.abs(wet).max()<1
print('boundary step',float(np.max(np.abs(wet[0]-wet[-1]))))
