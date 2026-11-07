"""Restore detail from pre-action UI masters with minimal, linear processing.
No extra oscillators, noise layers, pitch changes, saturation or time shortening.
"""
from pathlib import Path
import numpy as np
import wave, json
ROOT=Path('C:/Users/User/.codex/visualizations/2026/10/19/01a151a7-b3fb-7f30-b065-52195249c086')
SOURCE=ROOT/'KO_UI_Action/originals'; OUT=ROOT/'KO_UI_Clear'; OUT.mkdir(parents=True,exist_ok=True)
NAMES=['TitleNavigate','SlotNavigate','SlotConfirm','Cancel','CategoryNavigate','DisplayAdjust','DeleteConfirm','DialogOpen','SettingsOpen','VolumeAdjust']
preview=[]; comparison=[]; report=[]
def read(path):
 with wave.open(str(path),'rb') as w:
  assert w.getnchannels()==1 and w.getsampwidth()==2 and w.getframerate()==48000
  return np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768

def write(path,a):
 pcm=np.round(a*32767).astype('<i2')
 with wave.open(str(path),'wb') as w:
  w.setparams((1,2,48000,0,'NONE','not compressed')); w.writeframes(pcm.tobytes())
for name in NAMES:
 a=read(SOURCE/(name+'.wav')); n=len(a); oldpeak=np.abs(a).max()
 # Gentle presence lift, not the previous steep 2-3 kHz roll-off.
 # Symmetric padded filtering avoids a false transient at the tail.
 pad=4096; x=np.pad(a,(pad,pad)); count=1<<(len(x)-1).bit_length()
 f=np.fft.rfftfreq(count,1/48000)
 response=1+.15*np.exp(-.5*((f-2400)/1400)**2)-.06*np.exp(-.5*((f-350)/240)**2)
 response[0]=0
 b=np.fft.irfft(np.fft.rfft(x,count)*response,count)[pad:pad+n]
 # Tiny endpoint ramps only; keep the original envelope and complete tail.
 fade=np.minimum(1,np.arange(n)/48)*np.minimum(1,(n-1-np.arange(n))/144)
 b*=fade
 b*=oldpeak/max(.0001,np.abs(b).max())
 assert np.isfinite(b).all() and np.abs(b).max()<1 and b[0]==0 and b[-1]==0
 write(OUT/(name+'.wav'),b)
 silence=np.zeros(21600)
 preview.extend((b,silence))
 previous=read(ROOT/'KO_UI_Action'/(name+'.wav'))
 comparison.extend((previous,np.zeros(12000),b,silence))
 report.append({'name':name,'seconds':n/48000,'peak':float(np.abs(b).max())})
write(OUT/'UI_Clear_Preview.wav',np.concatenate(preview))
write(OUT/'UI_Before_After.wav',np.concatenate(comparison))
(OUT/'report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))