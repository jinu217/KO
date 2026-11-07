"""Rework KO's existing UI palette into short dark action cues.
Preserves originals beside previews for exact rollback. No game music is changed.
"""
from pathlib import Path
import json, shutil, wave
import numpy as np
RATE=48000
SOURCE=Path('C:/Unity/KO/Assets/Audio/UI')
OUT=Path('C:/Users/User/.codex/visualizations/2026/10/19/01a151a7-b3fb-7f30-b065-52195249c086/KO_UI_Action')
BACKUP=OUT/'originals'; BACKUP.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(117)
def read(path):
 with wave.open(str(path),'rb') as w:
  assert w.getsampwidth()==2
  rate=w.getframerate(); a=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
  a=a.reshape(-1,w.getnchannels()).mean(axis=1)
 return np.interp(np.arange(round(len(a)*RATE/rate))*rate/RATE,np.arange(len(a)),a)
def eq(a,low=100,high=4000):
 # Pad before spectral processing to prevent wrapping transients into the tail.
 n=len(a); count=1<<(2*n-1).bit_length(); f=np.fft.rfftfreq(count,1/RATE)
 response=(1-np.exp(-(f/low)**2))*np.exp(-(f/high)**4)
 return np.fft.irfft(np.fft.rfft(a,count)*response,count)[:n]
def fit(a,n): return np.pad(a[:n],(0,max(0,n-len(a))))
def scratch(n,low,high):
 a=eq(rng.standard_normal(n),low,high)
 a/=max(.001,np.max(np.abs(a)))
 return a
blade=read(Path('C:/Unity/KO/Assets/Audio/Sources/BladeImpact_UI_Base.wav'))
blade/=max(.001,np.max(np.abs(blade)))
# name: speed, duration, peak, upper cutoff, small recorded steel layer
specs={
'TitleNavigate':(1.06,.29,.28,3600,.0),
'SlotNavigate':(1.10,.31,.18,2900,.0),
'SlotConfirm':(1.14,.43,.34,3000,.065),
'Cancel':(1.12,.19,.19,2600,.0),
'CategoryNavigate':(1.12,.15,.21,3200,.04),
'DisplayAdjust':(1.10,.18,.22,3100,.035),
'DeleteConfirm':(1.12,.39,.32,2400,.075),
'DialogOpen':(1.08,.23,.19,2800,.015),
'SettingsOpen':(1.10,.29,.20,2800,.02),
'VolumeAdjust':(1.04,.12,.17,2300,.0),
}
preview=[]; report=[]
for name,(speed,duration,peak,cut,steel) in specs.items():
 src=SOURCE/(name+'.wav'); backup=BACKUP/src.name
 if not backup.exists(): shutil.copy2(src,backup)
 a=read(backup); n=round(duration*RATE); t=np.arange(n)/RATE
 original_peak=max(.001,np.max(np.abs(a)))
 a=np.interp(np.arange(n)*speed,np.arange(len(a)),a,left=0,right=0)
 a=eq(a,100 if name!='VolumeAdjust' else 350,cut)
 a/=original_peak
 # Keep the luminous slot wash, remove diffuse tail. No pitched sweep added.
 if name=='SlotNavigate': a*=np.exp(-t*1.8)
 elif name=='TitleNavigate': a*=np.exp(-t*2.3)
 elif name=='VolumeAdjust': a=.82*a+.11*scratch(n,500,2000)*np.sin(np.pi*np.arange(n)/(n-1))**1.3
 elif name=='Cancel':
  a=.88*a+.09*scratch(n,250,1600)*np.sin(np.pi*np.arange(n)/(n-1))**1.5
 else:
  a*=np.exp(-t*1.3)
 if steel:
  b=np.interp(np.arange(n)*.96,np.arange(len(blade)),blade,left=0,right=0)
  a+=eq(b,300,cut)*steel*np.exp(-t*13)
 # Gentle low body only on confirmations, not a bright success jingle.
 if name in ('SlotConfirm','DeleteConfirm'):
  a+=.07*np.sin(2*np.pi*96*t)*(1-np.exp(-t*400))*np.exp(-t*27)
 a-=np.mean(a)
 fade=np.minimum(1,np.arange(n)/(RATE*.0025))*np.minimum(1,(n-1-np.arange(n))/(RATE*.035))
 a*=fade
 a*=peak/max(.001,np.max(np.abs(a)))
 assert np.isfinite(a).all() and np.max(np.abs(a))<1
 pcm=np.round(a*32767).astype('<i2')
 with wave.open(str(OUT/(name+'.wav')),'wb') as w:
  w.setparams((1,2,RATE,0,'NONE','not compressed')); w.writeframes(pcm.tobytes())
 preview.extend([pcm,np.zeros(round(.45*RATE),dtype='<i2')])
 report.append({'name':name,'seconds':duration,'peak':float(np.max(np.abs(a)))})
with wave.open(str(OUT/'UI_Action_Preview.wav'),'wb') as w:
 w.setparams((1,2,RATE,0,'NONE','not compressed')); w.writeframes(np.concatenate(preview).tobytes())
(OUT/'manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))