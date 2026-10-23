"""Create original seamless wind and leaf foley; no voices, samples or music.

Deterministic source synthesis is CC0-1.0. Spectrum and limiting are measured in
the manifest. Native Unity reproduction and listening still require the game.
"""
import hashlib
import json
import wave
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'Assets/PoolroomsProcedural/Resources/Forest39/Audio'
RATE = 44100


def noise(length, seed, low, high):
    random = np.random.default_rng(seed)
    frequencies = np.fft.rfftfreq(length, 1 / RATE)
    spectrum = np.fft.rfft(random.normal(0, 1, length))
    # A smooth frequency envelope avoids metallic narrow-band resonances.
    envelope = np.zeros_like(frequencies)
    eligible = (frequencies >= low) & (frequencies <= high)
    f = frequencies[eligible]
    envelope[eligible] = np.sin(np.pi * (f - low) / (high - low)) ** 2
    envelope[eligible] /= np.sqrt(np.maximum(f, 1))
    result = np.fft.irfft(spectrum * envelope, length)
    return result / max(float(np.max(np.abs(result))), 1e-9)


def write(name, signal, loop):
    signal -= np.mean(signal)
    peak = np.max(np.abs(signal))
    signal = signal / max(peak, 1) * .74
    if loop:
        # Periodic FFT and whole-cycle modulation already match the seam.
        # Remove the residual first-order edge step for PCM conversion.
        edge = signal[-1] - signal[0]
        signal -= np.linspace(-edge * .5, edge * .5, len(signal))
    else:
        fade = min(int(RATE * .04), len(signal) // 4)
        signal[:fade] *= np.linspace(0, 1, fade)
        signal[-fade:] *= np.linspace(1, 0, fade)
    pcm = np.clip(signal * 32767, -32766, 32766).astype('<i2')
    path = DEST / (name + '.wav')
    with wave.open(str(path), 'wb') as stream:
        stream.setnchannels(1)
        stream.setsampwidth(2)
        stream.setframerate(RATE)
        stream.writeframes(pcm.tobytes())
    return {'path': path.name, 'bytes': path.stat().st_size,
            'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
            'seconds': len(pcm) / RATE, 'loop': loop,
            'peak': float(np.max(np.abs(signal))), 'rms': float(np.sqrt(np.mean(signal ** 2))),
            'seamStep': abs(int(pcm[-1]) - int(pcm[0])) if loop else 0}


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    duration = 24
    count = RATE * duration
    t = np.arange(count) / RATE
    modulation = .55 + .18 * np.sin(2 * np.pi * t / duration) + .08 * np.sin(2 * np.pi * t * 3 / duration)
    wind = noise(count, 390039, 90, 2400) * modulation
    air = noise(count, 390040, 1100, 6500) * (.1 + .04 * np.sin(2 * np.pi * t * 2 / duration))
    files = [write('OakWind', wind + air, True)]
    for index in range(3):
        count = int(RATE * (1.1 + index * .2))
        t = np.arange(count) / RATE
        envelope = np.sin(np.pi * np.arange(count) / (count - 1)) ** 2
        rustle = noise(count, 390100 + index, 650, 9500)
        pulses = .65 + .2 * np.sin(2 * np.pi * t * (6.3 + index))
        files.append(write('OakLeaves' + str(index), rustle * envelope * pulses * .45, False))
    manifest = {'license': 'CC0-1.0', 'author': 'Poolrooms project original procedural foley',
                'sampleRate': RATE, 'channels': 1, 'bits': 16, 'files': files,
                'sourceSamples': 0, 'voicesOrWhispers': False, 'startleStingers': False,
                'nativeListeningVerified': False, 'tool': 'tools/create_forest39_audio.py'}
    (DEST / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print(json.dumps({'clips': len(files), 'bytes': sum(f['bytes'] for f in files)}))


if __name__ == '__main__':
    main()
