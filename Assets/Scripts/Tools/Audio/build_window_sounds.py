"""KO window/delete cues, based on the selected dark UI palette.
Usage: python build_window_sounds.py EXISTING_UI_DIRECTORY OUTPUT_DIRECTORY
"""
import math
import random
import struct
import sys
import wave
from pathlib import Path

RATE = 48000
SOURCE, OUTPUT = Path(sys.argv[1]), Path(sys.argv[2])
OUTPUT.mkdir(parents=True, exist_ok=True)


def load(name):
    with wave.open(str(SOURCE / (name + '.wav')), 'rb') as wav:
        assert wav.getnchannels() == 1 and wav.getsampwidth() == 2
        raw = struct.unpack('<' + 'h' * wav.getnframes(), wav.readframes(wav.getnframes()))
        return [v / 32768 for v in raw], wav.getframerate()


def lowpass(values, frequency):
    a = 1 - math.exp(-2 * math.pi * frequency / RATE)
    state, result = 0.0, []
    for value in values:
        state += a * (value-state)
        result.append(state)
    return result


def layer(destination, source, source_rate, speed, gain, delay=0):
    start = int(delay * RATE)
    for i in range(len(destination) - start):
        p = i / RATE * source_rate * speed
        j = int(p)
        if j + 1 >= len(source):
            break
        destination[start+i] += (source[j] * (1-(p-j)) + source[j+1]*(p-j)) * gain


def friction(duration, seed, lower, upper):
    rng = random.Random(seed)
    raw = [rng.uniform(-1, 1) for _ in range(int(duration * RATE))]
    lo, hi = lowpass(raw, lower), lowpass(raw, upper)
    return [(b-a) * math.sin(math.pi*i/(len(raw)-1))**1.7
            for i, (a,b) in enumerate(zip(lo,hi))]


def save(name, values, peak):
    values = lowpass(values, 2200)
    mean = sum(values) / len(values)
    maximum = max(abs(v-mean) for v in values)
    result = []
    for i, v in enumerate(values):
        fade = min(1, i/(RATE*.003), (len(values)-1-i)/(RATE*.05))
        result.append(int((v-mean)/maximum*peak*fade*32767))
    with wave.open(str(OUTPUT/(name+'.wav')), 'wb') as wav:
        wav.setparams((1,2,RATE,0,'NONE','not compressed'))
        wav.writeframes(struct.pack('<'+'h'*len(result),*result))
    print(name, round(len(result)/RATE,3), 's; peak', round(max(abs(v) for v in result)/32767,3))
    return result


confirm, confirm_rate = load('SlotConfirm')
cancel, cancel_rate = load('Cancel')
detent, detent_rate = load('CategoryNavigate')

# Deliberate, low single lock: not a success melody and not a repeated rattle.
delete = [0.0] * int(.46 * RATE)
layer(delete, confirm, confirm_rate, .78, .85)
layer(delete, detent, detent_rate, .70, .15, .045)
delete = lowpass(delete, 1050)
for i in range(len(delete)):
    t = i/RATE
    delete[i] += .025 * math.sin(2*math.pi*83*t) * (1-math.exp(-t*400)) * math.exp(-t/.055)
results = [save('DeleteConfirm', delete, .31)]

# A short, restrained worn-leather slide with a quiet mechanical engagement.
dialog = friction(.25, 'dialog-slide', 230, 1150)
layer(dialog, cancel, cancel_rate, .95, .26)
layer(dialog, detent, detent_rate, .8, .08, .055)
results.append(save('DialogOpen', dialog, .19))

# Same material, broader gesture to follow the settings panel's sliding entrance.
settings = friction(.34, 'settings-slide', 260, 1450)
layer(settings, cancel, cancel_rate, .72, .19)
layer(settings, detent, detent_rate, .72, .09, .12)
results.append(save('SettingsOpen', settings, .20))

preview = []
for result in results:
    preview += result + [0]*int(.65*RATE)
with wave.open(str(OUTPUT/'WindowSounds_Preview.wav'),'wb') as wav:
    wav.setparams((1,2,RATE,0,'NONE','not compressed'))
    wav.writeframes(struct.pack('<'+'h'*len(preview),*preview))
