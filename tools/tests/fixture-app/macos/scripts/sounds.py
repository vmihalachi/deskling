"""The fixture app's sounds: sunnysays' five recipes, so tools/tests/test_synth.py can check the engine still writes
sunnysays' committed files byte for byte."""


def sounds(engine):
    tone, glide, silence = engine.tone, engine.glide, engine.silence
    C5, D5, E5, G5, A5, C6, E6, A4 = engine.C5, engine.D5, engine.E5, engine.G5, engine.A5, engine.C6, engine.E6, engine.A4
    return {
        "blip": tone(E6, 0.15, gain=0.8, warmth=0.1),
        "hello": tone(C5, 0.16, warmth=0.3) + tone(G5, 0.2, warmth=0.3),
        "shh": glide(E5, A4, 0.4, gain=0.9),
        "tune-1": tone(A4, 0.32, vibrato=0.004) + tone(C5, 0.32, vibrato=0.004) + tone(D5, 0.3, vibrato=0.004)
        + silence(0.04) + tone(C5, 0.44, vibrato=0.006),
        "tune-2": tone(E5, 0.2) + tone(G5, 0.2) + tone(A5, 0.26) + silence(0.05) + tone(G5, 0.22) + tone(C6, 0.5, vibrato=0.006),
    }
