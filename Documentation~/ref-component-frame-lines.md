# Frame Lines component

The Frame Lines component allows you to render letter/pillar boxes in Game view to represent the sensor size or aspect ratio of a target format.

The Frame Lines component automatically applies to any Camera component of the GameObject it belongs to.


![Film Format Controls](images/ref-component-frame-lines.png)

## Gate Mask

The gate mask shows where the resolution gate (the aspect ratio of the display) is rendering more than the film gate (the aspect ratio from the sensor).

| **Property** | **Description** |
|:---|:---|
| **Gate Mask Opacity** | The opacity of the film gate mask. |

## Aspect Ratio

| **Property** | **Description** |
|:---|:---|
| **Show Aspect Ratio** | Enable this option to display the aspect ratio lines and mask in Game view. |
| **Aspect Ratio Preset** | Select a crop aspect ratio from a list of common values. |
| **Aspect Ratio** | The current aspect ratio of the crop mask. Type a value in this field if you need to specify a custom aspect ratio. |
| **Line** | The type of lines to display to frame the cropped view: a four-**Corner** based frame, a whole **Box**, or **None**. |
| **Line Color** | The color of the crop lines. |
| **Line Width** | The width of the crop lines. |
| **Fill Opacity** | The opacity of the crop mask. |

## Center Marker

| **Property** | **Description** |
|:---|:---|
| **Show Center Marker** | Enable this option to display the center marker in Game view. |
| **Center Marker** | The type of marker to materialize the center of the framed view: a **Cross**, a large **Dot**, or **None**. |

![Frame Lines Example](images/format-mask-reference.png)

---
## Using Frame Lines in URP

If you are using the Universal Render Pipeline (URP), you must enable the `VirtualCameraScriptableRenderFeature` on the project's `UniversalRenderPipelineAsset`.

To do this, press the **Add VirtualCameraScriptableRenderFeature render feature** button.
