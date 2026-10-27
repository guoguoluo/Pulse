# Pulse

A Unity 2022.3.29f1 prototype for a third-person, rhythm-driven narrative puzzle.

Open this directory as a Unity project. `Assets/Project/Scenes/SampleScene.unity`
is the original prototype; `Assets/Project/Scenes/ToonStudy.unity` is the
experimental illustrated look.

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
