# Virtual Camera workflows

## Adding a Virtual Camera Device

The **[Virtual Camera Device](ref-component-virtual-camera-device.md)** is responsible for updating and listening to the connected client device. It also updates the target **[Virtual Camera Actor](ref-component-virtual-camera-actor.md)** with the latest data received from the client.

Click the **+** button from the **Capture Devices** list in the **Take Recorder** component and select **Virtual Camera Device**.

## Adding a Virtual Camera Actor

Add a **Virtual Camera Actor** by going to **Menu > GameObject > Create > Live Capture > Camera > Virtual Camera Actor**.

## General

**Saving and loading Virtual Camera Device settings**

Easily save and load the settings in your **Virtual Camera Device** (e.g. damping, lens preset, etc.) using Unity's **[Presets](https://docs.unity3d.com/Manual/Presets.html)** system.

**Starting camera position**

Position and orient the camera actor in the scene to configure the starting position and rotation (only accounts for y-rotation) of the virtual camera.

**Anchoring/Platforming**

To anchor the camera to moving object (e.g. dolly cart, car, boat, etc.), parent the Actor game object in the scene to the moving object.

**Control camera settings from the editor**

All camera state can be controlled from the editor. This is useful if you want to have an "operator" workflow where the individual with the device only has to worry about controlling the camera.
