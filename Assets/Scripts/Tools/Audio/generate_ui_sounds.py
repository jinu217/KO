"""Generate KO's dark medieval-mechanical UI sound set. Python stdlib only.
Usage: python generate_ui_sounds.py OUTPUT_DIRECTORY
"""
import math
import random
import struct
import sys
import wave
from pathlib import Path

RATE = 48000


def render(name, seconds, modes, clicks, peak, cutoff=850):
    rng = random.Random(name)
    count = int(seconds * RATE)
    signal = [0.0] * count
    # Inharmonic damped resonances imitate small steel mechanisms, not UI beeps.
    for frequency, amplitude, decay, start in modes:
        phase = rng.uniform(-0.15, 0.15)
        for i in range(int(start * RATE), count):
            t = i / RATE - start
            attack_rate = 22 if name == 'SlotNavigate' else 900
            attack = 1.0 - math.exp(-t * attack_rate)
            # Small modulation makes the steel resonance rough rather than a clean note.
            roughness = .045 * math.sin(2 * math.pi * 37 * t) * math.exp(-t / .06)
            signal[i] += amplitude * attack * math.exp(-t / decay) * math.sin(2 * math.pi * frequency * t + phase + roughness)
    for start, amplitude, decay in clicks:
        filtered = 0.0
        for i in range(int(start * RATE), min(count, int((start + decay * 8) * RATE))):
            t = i / RATE - start
            noise = rng.uniform(-1, 1)
            # Worn mechanisms scrape and thud rather than produce bright white clicks.
            filtered += (0.025 if name == 'SlotNavigate' else 0.17) * (noise - filtered)
            signal[i] += amplitude * filtered * math.exp(-t / decay) * min(1, t * 650)
    if name == 'SlotNavigate':
        # Clean opening-light wash: smooth swell, no cloth pulses or sub-bass rumble.
        low = mid = upper = 0.0
        a_low = 1 - math.exp(-2 * math.pi * 420 / RATE)
        a_mid = 1 - math.exp(-2 * math.pi * 1450 / RATE)
        a_upper = 1 - math.exp(-2 * math.pi * 2900 / RATE)
        for i in range(count):
            p = i / max(1, count - 1)
            noise = rng.uniform(-1, 1)
            low += a_low * (noise - low)
            mid += a_mid * (noise - mid)
            upper += a_upper * (noise - upper)
            envelope = math.sin(math.pi * p) ** 1.7
            signal[i] = ((mid - low) * .85 + (upper - mid) * .08) * envelope
        dry = signal[:]
        for seconds, gain in ((.023,.10),(.047,.055),(.079,.025)):
            shift = int(seconds * RATE)
            for i in range(shift, count):
                signal[i] += dry[i-shift] * gain
    if name == 'Cancel':
        # A continuous breath/scrape instead of taps, arpeggios, or pitched UI notes.
        low = band = 0.0
        for i in range(count):
            progress = i / max(1, count - 1)
            noise = rng.uniform(-1, 1)
            low += .025 * (noise - low)
            band += .22 * (noise - band)
            envelope = (math.sin(math.pi * progress) ** 1.8 if name == 'SlotNavigate'
                        else (1 - math.exp(-progress * 24)) * (1 - progress) ** 2)
            texture = low * 2.2 + (band - low) * (.55 if name == 'SlotNavigate' else .25)
            signal[i] += texture * envelope * 2.7
    # Round off the upper edge twice: no glassy/chiming treble.
    coefficient = 1 - math.exp(-2 * math.pi * cutoff / RATE)
    for _ in range(2):
        lowpass = 0.0
        for i, value in enumerate(signal):
            lowpass += coefficient * (value - lowpass)
            signal[i] = lowpass
    # Subtle, dark room reflections, kept shorter on frequently repeated navigation.
    reflected = signal[:]
    for delay, gain in ((0.019, 0.13), (0.037, 0.08), (0.061, 0.045)):
        shift = int(delay * RATE)
        for i in range(shift, count):
            reflected[i] += signal[i - shift] * gain
    mean = sum(reflected) / count
    maximum = max(abs(x - mean) for x in reflected) or 1
    samples = []
    for i, value in enumerate(reflected):
        fade = min(1, i / 96, (count - 1 - i) / 480)
        samples.append(int(max(-1, min(1, (value - mean) / maximum * peak * fade)) * 32767))
    with wave.open(str(OUTPUT / (name + '.wav')), 'wb') as wav:
        wav.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        wav.writeframes(struct.pack('<' + 'h' * len(samples), *samples))
    return samples


OUTPUT = Path(sys.argv[1])
OUTPUT.mkdir(parents=True, exist_ok=True)
clips = [
    # A single dark blade impact: short scraped onset, restrained inharmonic ring.
    ('TitleNavigate', .34, [(277,.10,.035,0),(811,.20,.047,0),(1103,.18,.061,0),(1549,.13,.042,0),(2017,.07,.031,0),(2777,.035,.018,0)], [(0,2.8,.0035),(.002,1.1,.014)], .34, 2400),
    # A low, breathing resonance without stepped notes or glittering upper partials.
    ('SlotNavigate', .40, [], [], .23, 2500),
    # A heavy iron bolt engages in two close impacts, ending in a subdued room tail.
    ('SlotConfirm', .54, [(69,.36,.085,0),(137,.20,.08,.025),(307,.09,.065,.048),(719,.045,.035,.048)], [(0,2.3,.012),(.052,1.8,.015)], .40, 1550),
    # Releasing a worn catch: soft metal scrape, then a low closing knock.
    ('Cancel', .22, [], [], .20, 1200),
    # Quiet mechanical detent; lower and shorter than the blade on the main menu.
    ('CategoryNavigate', .17, [(157,.15,.024,0),(431,.09,.024,0),(947,.025,.013,0)], [(0,2.1,.008)], .23, 1450),
    # A small weighted rotary switch: tactile, no melodic progression.
    ('DisplayAdjust', .20, [(127,.18,.028,0),(353,.07,.021,.016),(811,.02,.017,.016)], [(0,2.0,.006),(.018,1.1,.008)], .24, 1250),
]
preview = []
for definition in clips:
    name, duration, modes, clicks, peak = definition[:5]
    cutoff = definition[5] if len(definition) > 5 else 850
    samples = render(name, duration, modes, clicks, peak, cutoff)
    preview += samples + [0] * int(.45 * RATE)
    print(f'{name}: {len(samples)/RATE:.2f}s, peak {max(abs(x) for x in samples)/32767:.3f}')
with wave.open(str(OUTPUT / 'KO_UI_Preview.wav'), 'wb') as wav:
    wav.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
    wav.writeframes(struct.pack('<' + 'h' * len(preview), *preview))
