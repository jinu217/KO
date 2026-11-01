"""KO UI sound set, using CC0 recorded steel and original shaped textures.
Usage: python build_dark_ui_set.py RECORDED_BLADE_WAV OUTPUT_DIRECTORY
See TitleNavigate_SOURCE.md for the recording license.
"""
import math
import random
import struct
import sys
import wave
from pathlib import Path

RATE = 48000
OUT = Path(sys.argv[2])
OUT.mkdir(parents=True, exist_ok=True)
with wave.open(sys.argv[1], 'rb') as wav:
    assert wav.getsampwidth() == 2 and wav.getnchannels() == 1
    raw = struct.unpack('<' + 'h' * wav.getnframes(), wav.readframes(wav.getnframes()))
    source_rate = wav.getframerate()
source = [v / 32768 for v in raw]


def lowpass(samples, cutoff):
    a = 1 - math.exp(-2 * math.pi * cutoff / RATE)
    state = 0.0
    result = []
    for value in samples:
        state += a * (value - state)
        result.append(state)
    return result


def steel(duration, speed=1.0, offset=0.0):
    result = []
    for i in range(int(duration * RATE)):
        pos = (offset + i / RATE * speed) * source_rate
        j = int(pos)
        result.append(source[j] * (1 - (pos-j)) + source[j+1] * (pos-j)
                      if j+1 < len(source) else 0.0)
    return result


def texture(duration, seed, bottom, top):
    rng = random.Random(seed)
    noise = [rng.uniform(-1, 1) for _ in range(int(duration * RATE))]
    upper, lower = lowpass(noise, top), lowpass(noise, bottom)
    return [a-b for a, b in zip(upper, lower)]


def finish(name, samples, peak, fade_in=.002, fade_out=.04):
    # Shared small, dark acoustic space; no long bright or musical ringing.
    dry = samples[:]
    for delay, gain in ((.011,.07),(.029,.025)):
        shift = int(delay * RATE)
        for i in range(shift, len(samples)):
            samples[i] += dry[i-shift] * gain
    mean = sum(samples) / len(samples)
    samples = [v-mean for v in samples]
    maximum = max(abs(v) for v in samples) or 1
    pcm = []
    for i, value in enumerate(samples):
        fade = min(1, i / max(1, fade_in * RATE), (len(samples)-1-i) / max(1, fade_out * RATE))
        pcm.append(int(value / maximum * peak * fade * 32767))
    with wave.open(str(OUT / (name + '.wav')), 'wb') as wav:
        wav.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        wav.writeframes(struct.pack('<' + 'h'*len(pcm), *pcm))
    print(name, round(len(pcm)/RATE, 3), 's', 'peak', round(max(abs(v) for v in pcm)/32767, 3))
    return pcm


sounds = []
# Shorten the existing recorded blade impact; soften its high edge and level.
title = lowpass(steel(.34, speed=1.0), 2900)
sounds.append(finish('TitleNavigate', title, .25, fade_out=.09))

# A restrained light movement: broad smooth texture, no pitches or low wind rumble.
slot = texture(.34, 'slot-light', 520, 1700)
for i in range(len(slot)):
    p = i / (len(slot)-1)
    slot[i] *= math.sin(math.pi*p) ** 1.9
sounds.append(finish('SlotNavigate', lowpass(slot, 2400), .16, .012, .055))

# Recorded, muffled mechanism closes with a small secondary catch.
confirm = lowpass(steel(.34, speed=.67), 1100)
catch = lowpass(steel(.065, speed=.9), 1600)
for i, value in enumerate(catch):
    confirm[i+int(.055*RATE)] += value * .22
sounds.append(finish('SlotConfirm', confirm, .29, fade_out=.075))

# Dry, soft leather-like release, without any repeated clicks.
cancel = texture(.19, 'soft-release', 260, 1100)
for i in range(len(cancel)):
    p = i/(len(cancel)-1)
    cancel[i] *= (1-math.exp(-p*28)) * (1-p)**2.5
sounds.append(finish('Cancel', cancel, .14, .004, .035))

# Small tactile details share the recorded material but sit below title/confirm.
category = lowpass(steel(.11, speed=1.15), 1500)
sounds.append(finish('CategoryNavigate', category, .14, fade_out=.045))
display = lowpass(steel(.13, speed=.85), 1200)
sounds.append(finish('DisplayAdjust', display, .15, fade_out=.05))

preview = []
for sound in sounds:
    preview.extend(sound)
    preview.extend([0] * int(.55*RATE))
with wave.open(str(OUT/'KO_UI_Preview.wav'), 'wb') as wav:
    wav.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
    wav.writeframes(struct.pack('<'+'h'*len(preview), *preview))
