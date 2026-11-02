# Pulse

A Unity 2022.3.29f1 prototype for a third-person, rhythm-driven narrative puzzle.

Open this directory as a Unity project. `Assets/Project/Scenes/SampleScene.unity`
is the original prototype; `Assets/Project/Scenes/ToonStudy.unity` is the
experimental illustrated look.

## Conductor demo

Open `Assets/Project/Scenes/ToonStudy.unity`. Move with WASD or arrow keys
while moving the mouse downward to conduct; the camera follows automatically.
Each valid stroke briefly illuminates the surroundings. A stroke near the
guide meteor's first beat permanently advances the path. Six distinct hits
unlock the path, and approaching the elevated goal triggers the existing
staircase animation. Missing a beat never removes progress.

The current audio is a clearly synthetic, original placeholder, **not** a
recording of Rachmaninoff's Piano Concerto No. 2. The eight first-beat offsets
and loop length in `FirstBeatTimeline` are placeholder timing data. Both cue
and judgement use those offsets on the DSP clock. The placeholder synth adds
two simple layers after two and four hits; these are not orchestral stems.

For authorized replacement material, place synchronized piano, strings, and
winds audio files in `Assets/Project/Audio/Music/`. Export all three from the
same start time, with matching duration and sample rate. Assign the clips in
the `PlanetPulse` object's `FirstBeatTimeline` component, then replace its
first-beat offsets and loop length with manually marked values relative to
the audio start. Do not use an unlicensed recording or assume constant BPM.
The synth disables itself when clips are assigned. Verify audio/cue alignment
in play mode after replacement; this placeholder does not establish timing
for any eventual performance recording.

### Tuning the conductor

In `ToonStudy`, select `Player > Downstroke Input` for smoothing, downward
speed, reset speed, minimum travel, cooldown, and the small-motion threshold.
Select `ResonanceSystem > Conductor Demo` for Perfect/Great/Good timing windows,
the idle assist, and the number of permanent path segments. The timing windows
are early/late tolerances in seconds around each authored first beat.

`ResonanceSystem > Conductor World Feedback` controls the five transient
brightness levels and their decay. `Player/CameraPivot/Main Camera >
Conductor Blackout` controls the near-black initial image, each rating's
temporary reveal, and the permanent reveal per successful segment. Motion
alone only gives a faint temporary response; only a distinct first-beat hit
advances the path.

Each of the four downstroke ratings (Off Beat, Good, Great, Perfect) has its
own brightness and hold duration in both `Conductor World Feedback` (local
light) and `Conductor Blackout` (whole-screen visibility). `Motion` has a
separate faint setting. The shared rise duration and editable rise curve,
followed by the shared fade-back duration, are under `Shared Light Envelope`
on both components. A new rating begins from the current light level, so
overlapping gestures do not snap the light to zero.

The first-beat guide meteor is drawn in camera space after the blackout, so
its idle glow remains visible even before the first gesture. It brightens
before each authored first beat and streaks at the beat. Adjust its position,
size, idle color, and cue color on `Main Camera > Guide Meteor Overlay`.

`Main Camera > Smooth Follow Camera` controls the third-person framing. The
camera detaches from the player at runtime and smoothly follows from a
world-space offset; `Position Smooth Time` and `Rotation Smooth Time` control
its lag. The default offset is farther back and the field of view is wider
than the original prototype. Mouse movement remains reserved for conducting.

## External assets

The project references third-party art under `Assets/External`, which is not
redistributed in this repository. A fresh checkout will show missing assets
until the original packages are imported with their Unity `.meta` files (and
therefore the same GUIDs). Do not regenerate the metadata for those assets
before restoring them.

The local project uses the Stylized Astronaut character and LowPoly Environment
Pack. Obtain and use each package under its own license. The Unity MCP package
declared in `Packages/manifest.json` also requires Git access during package
resolution.

## Source control

Commit `Assets/Project`, `Packages`, and `ProjectSettings`. Unity-generated
caches, local screenshots, and external licensed assets are intentionally
excluded.
