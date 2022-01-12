# Virtual Camera workflows

### Saving and loading Virtual Camera Device settings

Easily save and load the settings in your **Virtual Camera Device** (e.g. damping, lens preset, etc.) using Unity's **[Presets](https://docs.unity3d.com/Manual/Presets.html)** system.

### Starting camera position

Position and orient the camera actor in the scene to configure the starting position and rotation (only accounts for y-rotation) of the virtual camera.

### Anchoring/Platforming

To anchor the camera to moving object (e.g. dolly cart, car, boat, etc.), parent the Actor game object in the scene to the moving object.

### Control camera settings from the editor

All camera state can be controlled from the editor. This is useful if you want to have an "operator" workflow where the individual with the device only has to worry about controlling the camera.
