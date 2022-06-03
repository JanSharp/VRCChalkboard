
# Dependencies

Requires the `UpdateManager` from `Common`.

# One Time Setup

Drag the `UpdateManager` prefab into your scene. It must keep that exact name and be at the root of the hierarchy.

# Usage

The way the `RotationGrip` script works is it rotates a given object whenever the object the `RotationGrip` is picked up (using a VRC Pickup script). The object `To Rotate` then gets rotated such that it perfectly faces _away from_ the held object.

With that in mind the recommended structure for using the `RotationGrip` script looks like this:
```
MyObjectRoot
┣╸RotationOrigin (empty GameObject)
┃ ┣╸ActualObjectPart1
┃ ┣╸ActualObjectPart2
┃ ┗╸...
┗╸Grip
```
Where the `Grip` GameObject has the `RotationGrip` script on it and it's transform `To Rotate` is set to `RotationOrigin`.

The names are up to you of course.

# Editor Tools

The `RotationGrip` script comes with 2 (technically 3) utilities in the form of buttons in the inspector for the script.

- Snap in Line (for positioning in the editor)
- Add VRC Pickup (for when you add a `RotationGrip` script to an object that doesn't have a VRC Pickup yet => fastest way to setup an object)
  - Configure VRC Pickup (similar to adding, but instead of adding it's only configuring existing components)

Each one has a descriptive **tooltip**.
