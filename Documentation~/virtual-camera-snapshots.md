# Virtual Camera Snapshots

Snapshots let you take a rich screenshot so that you later return to that position and/or load the snapshot metadata from that snapshot. The metadata includes information like the lens, camera body and pose. Snapshots can restore the Timeline's time and slate clip used when the snapshot was taken, if the clip is available in the PlayableDirector previewing in the Timeline window.

## In Editor

The Snapshots controller is available on the [Virtual Camera Device](ref-component-virtual-camera-device.md) component.

### Snapshots list

The snapshot entries show a thumbnail of the screen, shot name, timecode, lens information and sensor size.

![](images/virtual-camera-snapshot-inspector.png)

### Snapshots controls

| Button            | Function                                                     |
| :---------------- | :----------------------------------------------------------- |
| **Take Snapshot** | Captures a snapshot with the current camera position and metadata. |
| **Go To**         | Restores the camera's position and rotation of the selected snapshot and the Timeline's shot if available, without loading the camera related metadata. |
| **Load**          | Restores the camera's position, rotation, lens and body of the selected snapshot and the Timeline's shot if available. |