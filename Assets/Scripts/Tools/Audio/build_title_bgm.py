"""Original KO title ambient loop, 64 seconds; requires numpy."""
from pathlib import Path
import numpy as np
import wave
RATE=32000
DURATION=64
n=RATE*DURATION
t=np.arange(n,dtype=np.float64)/RATE
mix=np.zeros((n,2),dtype=np.float64)
def freq(midi): return 440*2**((midi-69)/12)
def event(start,length,midi,gain,pan=.5,metal=False):
 count=int(length*RATE); u=np.arange(count)/RATE
 env=(1-np.exp(-u/.8))*np.exp(-u/(length*.28))
 env*=np.minimum(1,(length-u)/1.4)
 f=freq(midi)
 if metal:
  voice=np.sin(2*np.pi*f*u)*.65+np.sin(2*np.pi*f*2.003*u)*.20*np.exp(-u/1.6)+np.sin(2*np.pi*f*2.997*u)*.06*np.exp(-u/.6)
 else:
  voice=np.sin(2*np.pi*f*u+ .12*np.sin(2*np.pi*.13*u))+.24*np.sin(2*np.pi*f*1.003*u)+.12*np.sin(2*np.pi*f*2*u)
 values=voice*env*gain
 idx=(int(start*RATE)+np.arange(count))%n
 mix[idx,0]+=values*np.sqrt(1-pan); mix[idx,1]+=values*np.sqrt(pan)
# A continuous low D/A bed, with period-locked oscillators for a seamless boundary.
for midi,gain,phase in [(26,.09,.0),(38,.045,.7),(33,.035,1.8)]:
 f=round(freq(midi)*DURATION)/DURATION
 for channel in range(2):
  mix[:,channel]+=gain*np.sin(2*np.pi*f*t+phase+channel*.13)*( .8+.2*np.sin(2*np.pi*t/32+phase))
# Slow minor harmony with long overlapping bowed/organ-like swells.
chords=[(50,53,57),(46,50,53),(43,50,58),(45,52,58)]
for k,chord in enumerate(chords):
 for j,midi in enumerate(chord): event(k*16+j*.45,24,midi,.038,.2+j*.3)
# Sparse, muted bell/metal motif; no rhythmic UI-like chimes.
for start,midi,gain,pan in [(6,62,.035,.3),(13,60,.025,.7),(23,57,.03,.6),(31,53,.024,.25),(40,55,.03,.3),(48,53,.025,.7),(55,52,.032,.5)]:
 event(start,10,midi,gain,pan,True)
# Distant low mechanical strokes, softened attack and no sharp click.
for start in (2,18,34,50): event(start,7,26,.055,.5,True)
# Circular diffuse echoes carry the tail across the loop boundary.
wet=mix.copy()
for delay,gain in [( .37,.19),(.73,.14),(1.13,.11),(1.91,.08),(3.17,.045)]:
 wet+=np.roll(mix[:,::-1],int(delay*RATE),axis=0)*gain
wet-=wet.mean(axis=0)
wet=np.tanh(wet*1.4)
wet*=.54/np.max(np.abs(wet))
out=Path('C:/Users/User/.codex/visualizations/2026/10/19/01a151a7-b3fb-7f30-b065-52195249c086/KO_Title_BGM')
out.mkdir(parents=True,exist_ok=True)
with wave.open(str(out/'KO_Title_Dark.wav'),'wb') as w:
 w.setparams((2,2,RATE,0,'NONE','not compressed')); w.writeframes((wet*32767).astype('<i2').tobytes())
print('duration',DURATION,'peak',float(np.abs(wet).max()),'rms',float(np.sqrt(np.mean(wet**2))),'boundary step',float(np.max(np.abs(wet[0]-wet[-1]))))