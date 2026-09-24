#!/usr/bin/env python3
"""
Generate a small synthetic SFX pack for the BackToSchool Unity project.

The output lands in Assets/Resources/GeneratedGameSfx so the clips can be
loaded at runtime with Resources.Load without touching scene references.
"""

from __future__ import annotations

import math
import random
import re
import struct
import uuid
import wave
from pathlib import Path


SAMPLE_RATE = 44_100
ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "Resources" / "GeneratedGameSfx"


def sample_count(duration: float) -> int:
    return max(1, int(round(duration * SAMPLE_RATE)))


def silence(duration: float) -> list[float]:
    return [0.0] * sample_count(duration)


def mix(*buffers: list[float]) -> list[float]:
    length = max((len(buf) for buf in buffers), default=0)
    out = [0.0] * length
    for buf in buffers:
        for i, value in enumerate(buf):
            out[i] += value
    return out


def add_at(base: list[float], clip: list[float], start_time: float) -> None:
    start_index = int(start_time * SAMPLE_RATE)
    end_index = min(len(base), start_index + len(clip))
    for i in range(start_index, end_index):
        base[i] += clip[i - start_index]


def normalize(buffer: list[float], target_peak: float = 0.92) -> list[float]:
    peak = max((abs(x) for x in buffer), default=1.0)
    if peak <= 1e-6:
        return buffer
    scale = target_peak / peak
    return [x * scale for x in buffer]


def write_wav(path: Path, buffer: list[float]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = bytearray()
    for sample in normalize(buffer):
        clamped = max(-1.0, min(1.0, sample))
        pcm.extend(struct.pack("<h", int(clamped * 32767.0)))

    with wave.open(str(path), "wb") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(SAMPLE_RATE)
        handle.writeframes(bytes(pcm))


def lowpass(buffer: list[float], cutoff_hz: float) -> list[float]:
    if cutoff_hz <= 0:
        return buffer[:]

    rc = 1.0 / (2.0 * math.pi * cutoff_hz)
    dt = 1.0 / SAMPLE_RATE
    alpha = dt / (rc + dt)
    out: list[float] = []
    current = 0.0
    for sample in buffer:
        current += alpha * (sample - current)
        out.append(current)
    return out


def highpass(buffer: list[float], cutoff_hz: float) -> list[float]:
    if cutoff_hz <= 0:
        return buffer[:]

    rc = 1.0 / (2.0 * math.pi * cutoff_hz)
    dt = 1.0 / SAMPLE_RATE
    alpha = rc / (rc + dt)
    out: list[float] = []
    y = 0.0
    previous_x = 0.0
    for sample in buffer:
        y = alpha * (y + sample - previous_x)
        previous_x = sample
        out.append(y)
    return out


def sine_wave(
    duration: float,
    frequency_start: float,
    frequency_end: float | None = None,
    amplitude: float = 1.0,
    vibrato_hz: float = 0.0,
    vibrato_depth: float = 0.0,
) -> list[float]:
    count = sample_count(duration)
    phase = 0.0
    end_frequency = frequency_end if frequency_end is not None else frequency_start
    out: list[float] = []
    for index in range(count):
        t = index / SAMPLE_RATE
        pct = index / max(1, count - 1)
        frequency = frequency_start + (end_frequency - frequency_start) * pct
        if vibrato_hz > 0.0 and vibrato_depth > 0.0:
            frequency += math.sin(2.0 * math.pi * vibrato_hz * t) * vibrato_depth
        phase += 2.0 * math.pi * frequency / SAMPLE_RATE
        out.append(math.sin(phase) * amplitude)
    return out


def noise_wave(duration: float, seed: str, amplitude: float = 1.0) -> list[float]:
    rng = random.Random(seed)
    return [(rng.uniform(-1.0, 1.0) * amplitude) for _ in range(sample_count(duration))]


def apply_exp_decay(buffer: list[float], speed: float, floor: float = 0.0) -> list[float]:
    out: list[float] = []
    total = max(1, len(buffer) - 1)
    for i, sample in enumerate(buffer):
        t = i / total
        env = floor + (1.0 - floor) * math.exp(-speed * t)
        out.append(sample * env)
    return out


def apply_linear_fade(buffer: list[float], attack: float, release: float) -> list[float]:
    count = len(buffer)
    attack_samples = max(1, int(attack * SAMPLE_RATE))
    release_samples = max(1, int(release * SAMPLE_RATE))
    out: list[float] = []
    for i, sample in enumerate(buffer):
        gain = 1.0
        if i < attack_samples:
            gain *= i / attack_samples
        if i >= count - release_samples:
            gain *= max(0.0, (count - i) / release_samples)
        out.append(sample * gain)
    return out


def bell_strike(duration: float, fundamentals: list[tuple[float, float, float]]) -> list[float]:
    count = sample_count(duration)
    out = [0.0] * count
    for frequency, amplitude, decay_speed in fundamentals:
        phase = 0.0
        for index in range(count):
            t = index / SAMPLE_RATE
            phase += 2.0 * math.pi * frequency / SAMPLE_RATE
            out[index] += math.sin(phase) * amplitude * math.exp(-decay_speed * t)
    return apply_linear_fade(out, 0.003, 0.08)


def generate_correct_answer() -> list[float]:
    clip = silence(0.55)
    notes = [
        (523.25, 0.0, 0.16, 0.55),
        (659.25, 0.11, 0.18, 0.48),
        (783.99, 0.23, 0.26, 0.42),
    ]
    for frequency, start, duration, amplitude in notes:
        tone = mix(
            sine_wave(duration, frequency, amplitude=amplitude),
            sine_wave(duration, frequency * 2.0, amplitude=amplitude * 0.18),
        )
        shaped = apply_linear_fade(apply_exp_decay(tone, 4.6), 0.004, 0.05)
        add_at(clip, shaped, start)
    sparkle = highpass(noise_wave(0.24, "correct-sparkle", 0.14), 2500.0)
    sparkle = apply_exp_decay(sparkle, 8.0)
    add_at(clip, sparkle, 0.15)
    return clip


def generate_wrong_answer() -> list[float]:
    clip = silence(0.52)
    segments = [
        (245.0, 0.0),
        (196.0, 0.12),
        (146.0, 0.24),
    ]
    for frequency, start in segments:
        duration = 0.16
        base = sine_wave(duration, frequency, frequency_end=frequency * 0.94, amplitude=0.48)
        buzz = sine_wave(duration, frequency * 2.0, frequency_end=frequency * 1.88, amplitude=0.12)
        harsh = highpass(noise_wave(duration, f"wrong-{frequency}", 0.08), 900.0)
        tone = mix(base, buzz, harsh)
        shaped = apply_linear_fade(apply_exp_decay(tone, 5.8), 0.003, 0.04)
        add_at(clip, shaped, start)
    return clip


def generate_button_press() -> list[float]:
    duration = 0.10
    click = highpass(noise_wave(duration, "button-press", 0.36), 1900.0)
    click = apply_exp_decay(click, 18.0)
    thump = sine_wave(duration, 180.0, 140.0, amplitude=0.24)
    thump = apply_exp_decay(thump, 14.0)
    return apply_linear_fade(mix(click, thump), 0.001, 0.03)


def generate_question_start() -> list[float]:
    duration = 0.42
    chirp = sine_wave(duration, 340.0, 760.0, amplitude=0.42)
    chirp = apply_exp_decay(chirp, 3.4)
    airy = highpass(noise_wave(duration, "question-start", 0.12), 1800.0)
    airy = apply_exp_decay(airy, 9.0)
    ping = bell_strike(0.26, [(1180.0, 0.30, 7.0), (1760.0, 0.12, 8.5)])
    return mix(chirp, airy, ping)


def generate_question_time_warning() -> list[float]:
    clip = silence(1.18)
    pulses = [
        (0.00, 820.0, 0.13, 0.38),
        (0.22, 880.0, 0.13, 0.42),
        (0.44, 940.0, 0.18, 0.50),
    ]
    for start, frequency, duration, amplitude in pulses:
        tone = mix(
            sine_wave(duration, frequency, frequency_end=frequency * 1.03, amplitude=amplitude),
            sine_wave(duration, frequency * 2.0, frequency_end=frequency * 2.06, amplitude=amplitude * 0.14),
        )
        tick = highpass(noise_wave(duration, f"time-warning-{frequency}", 0.08), 2200.0)
        shaped = apply_linear_fade(apply_exp_decay(mix(tone, tick), 6.8), 0.002, 0.05)
        add_at(clip, shaped, start)
    tail = highpass(noise_wave(0.22, "time-warning-tail", 0.05), 2600.0)
    add_at(clip, apply_exp_decay(tail, 11.0), 0.68)
    return clip


def generate_interval_bell() -> list[float]:
    clip = silence(2.05)
    partials = [
        (880.0, 0.84, 2.4),
        (1320.0, 0.46, 2.9),
        (1848.0, 0.24, 3.6),
        (2480.0, 0.12, 4.8),
        (3150.0, 0.08, 5.6),
    ]
    add_at(clip, bell_strike(1.55, partials), 0.0)
    add_at(clip, bell_strike(1.35, partials), 0.34)
    return clip


def generate_vigia_alert() -> list[float]:
    duration = 0.78
    whistle = sine_wave(
        duration,
        1280.0,
        frequency_end=940.0,
        amplitude=0.72,
        vibrato_hz=8.0,
        vibrato_depth=18.0,
    )
    overtone = sine_wave(duration, 2550.0, 1875.0, amplitude=0.16, vibrato_hz=8.0, vibrato_depth=30.0)
    burst = highpass(noise_wave(0.16, "vigia-alert", 0.20), 1400.0)
    burst = apply_exp_decay(burst, 12.0)
    clip = mix(apply_exp_decay(whistle, 2.5, 0.18), apply_exp_decay(overtone, 3.3), burst)
    return apply_linear_fade(clip, 0.002, 0.12)


def generate_player_footstep(seed: str, heavy: bool = False) -> list[float]:
    duration = 0.20 if not heavy else 0.24
    low_freq = 95.0 if not heavy else 72.0
    cutoff = 1200.0 if not heavy else 900.0
    noise_amp = 0.24 if not heavy else 0.28
    thump_amp = 0.42 if not heavy else 0.58

    gravel = lowpass(noise_wave(duration, seed, noise_amp), cutoff)
    gravel = apply_exp_decay(gravel, 15.0 if not heavy else 12.0)
    thump = sine_wave(duration, low_freq, frequency_end=low_freq * 0.78, amplitude=thump_amp)
    thump = apply_exp_decay(thump, 16.0 if not heavy else 12.5)
    squeak = highpass(noise_wave(duration, seed + "-squeak", 0.06), 2100.0)
    squeak = apply_exp_decay(squeak, 24.0)
    return apply_linear_fade(mix(gravel, thump, squeak), 0.001, 0.05)


def generate_vigia_walk(seed: str) -> list[float]:
    duration = 0.23
    cloth = lowpass(noise_wave(duration, seed + "-cloth", 0.16), 1000.0)
    cloth = apply_exp_decay(cloth, 13.0)
    heel = sine_wave(duration, 78.0, frequency_end=62.0, amplitude=0.30)
    heel = apply_exp_decay(heel, 13.5)
    shoe = highpass(noise_wave(duration, seed + "-shoe", 0.04), 1800.0)
    shoe = apply_exp_decay(shoe, 18.0)
    return apply_linear_fade(mix(cloth, heel, shoe), 0.001, 0.06)


def generate_vigia_run(seed: str) -> list[float]:
    duration = 0.25
    cloth = lowpass(noise_wave(duration, seed + "-cloth", 0.22), 950.0)
    cloth = apply_exp_decay(cloth, 11.0)
    heel = sine_wave(duration, 68.0, frequency_end=52.0, amplitude=0.52)
    heel = apply_exp_decay(heel, 11.5)
    shoe = highpass(noise_wave(duration, seed + "-shoe", 0.07), 1700.0)
    shoe = apply_exp_decay(shoe, 21.0)
    return apply_linear_fade(mix(cloth, heel, shoe), 0.001, 0.06)


def current_guid(meta_path: Path) -> str | None:
    if not meta_path.exists():
        return None
    match = re.search(r"^guid:\s*([0-9a-f]{32})$", meta_path.read_text(), re.MULTILINE)
    return match.group(1) if match else None


def ensure_folder_meta(folder: Path) -> None:
    meta_path = folder.with_name(folder.name + ".meta")
    guid = current_guid(meta_path) or uuid.uuid4().hex
    meta_path.write_text(
        "\n".join(
            [
                "fileFormatVersion: 2",
                f"guid: {guid}",
                "folderAsset: yes",
                "DefaultImporter:",
                "  externalObjects: {}",
                "  userData: ",
                "  assetBundleName: ",
                "  assetBundleVariant: ",
                "",
            ]
        )
    )


def ensure_audio_meta(audio_file: Path) -> None:
    meta_path = audio_file.with_name(audio_file.name + ".meta")
    guid = current_guid(meta_path) or uuid.uuid4().hex
    meta_path.write_text(
        "\n".join(
            [
                "fileFormatVersion: 2",
                f"guid: {guid}",
                "AudioImporter:",
                "  serializedVersion: 6",
                "  defaultSettings:",
                "    loadType: 0",
                "    sampleRateSetting: 0",
                "    sampleRateOverride: 44100",
                "    compressionFormat: 1",
                "    quality: 1",
                "    conversionMode: 0",
                "  platformSettingOverrides: {}",
                "  forceToMono: 0",
                "  normalize: 1",
                "  preloadAudioData: 1",
                "  loadInBackground: 0",
                "  ambisonic: 0",
                "  3D: 0",
                "  userData: ",
                "  assetBundleName: ",
                "  assetBundleVariant: ",
                "",
            ]
        )
    )


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    ensure_folder_meta(OUT_DIR)

    clips = {
        "answer_button_press.wav": generate_button_press(),
        "correct_answer.wav": generate_correct_answer(),
        "interval_bell.wav": generate_interval_bell(),
        "player_footstep_01.wav": generate_player_footstep("player-step-a", heavy=False),
        "player_footstep_02.wav": generate_player_footstep("player-step-b", heavy=False),
        "question_start.wav": generate_question_start(),
        "question_time_warning.wav": generate_question_time_warning(),
        "vigia_alert.wav": generate_vigia_alert(),
        "vigia_run_footstep_01.wav": generate_vigia_run("vigia-run-a"),
        "vigia_run_footstep_02.wav": generate_vigia_run("vigia-run-b"),
        "vigia_walk_footstep_01.wav": generate_vigia_walk("vigia-walk-a"),
        "vigia_walk_footstep_02.wav": generate_vigia_walk("vigia-walk-b"),
        "wrong_answer.wav": generate_wrong_answer(),
    }

    for filename, buffer in clips.items():
        output_path = OUT_DIR / filename
        write_wav(output_path, buffer)
        ensure_audio_meta(output_path)
        print(f"generated {output_path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
