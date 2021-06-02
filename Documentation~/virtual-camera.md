# Using the Virtual Camera

* [Getting started](virtual-camera-getting-started.md)
* [Workflow](virtual-camera-workflow.md)
* [App controls and settings](virtual-camera-app-controls.md)
* [Virtual Camera Device](ref-component-virtual-camera-device.md)

## Requirements

* PC running Windows or OSX.

* iPad with ARKit capabilities -- iPad Pro, iPad Air (3rd gen or later), iPad mini (5th gen or later), or iPad (5th gen or later).

* Video streaming requires a PC running Windows 10 with a modern GPU (NVIDIA GTX 1060 or better is recommended though not required).

**Note:** Currently the app functions on an iPhone with non-optimal layout.

## Virtual Camera app features

* Works in both editor edit and play modes.
* Camera tracking via ARKit.
* Additional camera motion control via **virtual joysticks**.
* Live preview via **video streaming** between the editor and iPad companion app.
* **Editor viewport** to see the virtual camera's view from the editor workstation.
* **Record camera performances** to an animation clip.
* **Axis locking** to prevent motion in any combination of 6 degrees of movement.
* Control **focal length**, **focus distance** and **aperture** with configurable damping for a smoother feel.
* **Reticle Auto Focus** mode to automatically set the focus distance based on a screen reticle.
* **Tracking Auto Focus** focus mode to have focus distance dynamically match a scene object's distance to the camera.
* Bi-directional control of camera settings, i.e. control the camera’s settings from either the editor or the companion app.
* Temporarily halt tracking and reposition around the physical space.
* Camera **motion damping**.
* **Motion scaling** to multiply physical motion.
* Steadicam simulation using Cinemachine.
* Non-destructive and iterative workflow -- camera settings like damping and focal length can be changed after a capture session.
* Video streaming support for both built-in, URP and HDRP render pipelines.
* **Snapshots** system to save an image along with your camera's position and metadata like lens information that can be loaded to pick up where you left off.
