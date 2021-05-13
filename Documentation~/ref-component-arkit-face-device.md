# ARKit Face Device component

The ARKit Face Device is a Live Capture Device that records and applies, in real time, face properties from a connected Client Device to an [ARKit Face Actor](ref-component-arkit-face-actor.md).

The ARKit Face Device communicates with a specific connected Client Device (Companion App installed on a physical mobile device) in order to retrieve the face properties.

![](images/ref-component-arkit-face-device.png)

## Bindings

| Property | Function |
|:---|:---|
| **Client Device** | The connected Client Device to use.<br />The selection list reflects the list of Clients that are currently connected to the Server in the [Connections window](ref-window-connections.md). |
| **Actor** | The target ARKit Face Actor to animate. |

## Channels

This section allows you to control which channels to activate or deactivate for recording when the Take Recorder is in Live mode.

Each channel represents a face property that you might want to animate separately from the others during the recording of a take:
* **Blend Shapes**
* **Head Position**
* **Head Rotation**
* **Eyes**
