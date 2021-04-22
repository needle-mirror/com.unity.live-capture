## The Virtual Camera Device component

Use the following fields to configure the Virtual Camera Device:

### Connections

| **Field**          | **Function**                                                 |
| ------------------ | ------------------------------------------------------------ |
| __Client Device__  | The connected client to use.                                 |
| __Actor__          | The target camera actor to animate.                          |
| __Inputs__         | The list of data channels to override using an external data source. |

### Camera properties

| **Field**                | **Function**                                                 |
| :----------------------- | :----------------------------------------------------------- |
| __Lens Preset__          | Set the values stored in one of the available `LensPreset` assets. |
| __Focal Length__         | The `focal length` of the lens in millimeters.               |
| __Focal Length Range__   | The min and max values of the `focal length`.                |
| __Focus Distance__       | The `focus distance` of the lens in world units.             |
| __Focus Distance Range__ | The min and max values of the `focus distance`.              |
| __Aperture__             | The `aperture` of the lens in f-numbers.                     |
| __Aperture Range__       | The min and max values of the `aperture`.                    |
| __Shift__                | The horizontal and vertical shift from the center of the sensor. |
| __Blade Count__          | Number of diaphragm blades the camera uses to form the aperture. |
| __Curvature__            | Maps an aperture range to blade curvature. Aperture blades become more visible on bokeh at higher aperture values. |
| __Barrel Clipping__      | The strength of the `cat eye` effect. You can see this effect on bokeh as a result of lens shadowing (distortion along the edges of the frame). |
| __Anamorphism__          | Stretch the sensor to simulate an anamorphic look. Positive values distort the camera vertically, negative will distort the Camera horizontally. |
| __Sensor Preset__        | Set the sensor size from default presets or custom presets defined in `FormatPresets` assets in the project. |
| __Sensor Size__          | The size of the camera sensor in millimeters.                |
| __ISO__                  | The sensibility of the real-world camera sensor. Higher values increase the Camera's sensitivity to light and result in faster exposure times |
| __Shutter Speed__        | The exposure time in seconds for the camera. Lower values result in less exposed pictures. |

### Live Link

Controls what `Channels` are active during `Live` mode.

| **Field**     | **Function**                                                 |
| ------------- | ------------------------------------------------------------ |
| __Channels__  | The flags representing the activation of channels during live mode. |

### Rig

Controls how the position of the virtual camera should be mapped into the virtual world.

| **Field**     | **Function**                                                 |
| ------------- | ------------------------------------------------------------ |
| __Pose__      | Camera world coordinates, corresponds to the localPose relative to the origin. |
| __Origin__    | Represents the camera point of origin. `localPose` is expressed relatively to this origin. |
| __LocalPose__ | Will set the position of the camera relative to the origin. Reseting the localpose to (0,0,0) will set the camera to the origin point. |

### Settings

Represents the camera settings. Controls the way the motion of the camera should behave as well as the reticle. Parameters are shared with the connected client device.

| **Field**             | **Function**                                                 |
| --------------------- | ------------------------------------------------------------ |
| __Damping__           | Damping can be enabled or disabled. The `body` of the damping represents how much time it takes in seconds to  reach the target position. The `aim` is the same as the body but for the rotation. |
| __Position Lock__     | Lock the position of an axis. The position lock is relative to the origin. |
| __Rotation Lock__     | Lock the rotation of an axis. The rotation lock is relative to the origin. |
| __Zero Dutch__        | Force the dutch rotation to be zero. Only enabled when the `rotationLock` dutch axis is disabled. |
| __Ergonomic Tilt__    | Offset the camera rotation on the tilt axis. Useful to be more confortable while using the client. Only applied when the client is connected. |
| __Motion Scale__      | Scaling applied to the device motion when controlling the virtualcamera position. A scale of (1,1,1) means that the VirtualCamera movement will match the device position in the real world. Useful if the virtual world is bigger than the room used. |
| __Joystick Speed__    | Used to set the speed of the joysticks. A speed of (1,2,1) will move the pedestal axis two time faster. |
| __Pedestal Space__    | Whether or not the the pedestal joystick motion be relative to the Origin or the Local pose. |
| __Focus Mode__        | Sets the focus to manual, automatic or spatial. |
| __Reticle Position__ | Normalized position of the reticle. |
| __Reticle Control__  | Should the reticle be controlled from the game view as well as from the device (remark: not mutually exclusive). |

### Video Server

These settings configure the video stream sent to the client device. See [Using video streaming](video-streaming.md) for more details.
