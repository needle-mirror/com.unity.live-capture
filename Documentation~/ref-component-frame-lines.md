# Frame Lines component

The Frame Lines component allows you to render letter/pillar boxes in Game view to represent the sensor size or aspect ratio of a target format, as well as a center marker.

The Frame Lines component automatically applies to any Camera component of the GameObject it belongs to. This component must be on the same Game Object as the Virtual Camera Actor to be able to control it from the device.

Note that in the context of Virtual Camera workflows, the aspect ratio and enabled state of the gate mask, aspect ratio lines, and the center marker are controlled via the Virtual Camera Device settings or the app, and not via the component inspector.


![Film Format Controls](images/ref-component-frame-lines.png)

## Gate Mask

The gate mask applies a fill to the area outside of the "film gate", or the difference between the resolution gate (the aspect ratio of the game view) and the film gate (the aspect ratio of the sensor)

| **Property** | **Description** |
|:---|:---|
| **Gate Mask** | Toggles the visibility of the gate mask. |
| **Gate Mask Opacity** | The opacity of the film gate mask. |

## Aspect Ratio

| **Property** | **Description** |
|:---|:---|
| **Aspect Ratio Lines** | Enable this option to display the aspect ratio lines and mask in the Game view. |
| **Aspect Ratio Preset** | Select a crop aspect ratio from a list of common values. Controlled by the Virtual Camera Device when on the same Game Object as a Virtual Camera Actor. |
| **Aspect Ratio** | The current aspect ratio of the frame lines. Type a value in this field if you need to specify a custom aspect ratio. Controlled by the Virtual Camera Device when on the same Game Object as a Virtual Camera Actor. |
| **Type** | The type of lines to display to frame the cropped view: a four-**Corner** based frame, a whole **Box**, or **None**. |
| **Color** | The color of the frame lines. |
| **Width** | The width of the frame lines. |
| **Fill Opacity** | The opacity of the crop mask. |

## Center Marker

| **Property** | **Description** |
|:---|:---|
| **Center Marker** | Enable this option to display the center marker in Game view. |
| **Type** | The type of marker to materialize the center of the framed view: a **Cross** or a large **Dot**. |

![Frame Lines Example](images/format-mask-reference.png)

---
## Using Frame Lines in URP

If you are using the Universal Render Pipeline (URP), you must enable the `VirtualCameraScriptableRenderFeature` on the project's `UniversalRenderPipelineAsset`.

To do this, press the **Add VirtualCameraScriptableRenderFeature** button.
