# Timecode Synchronizer component

Use this component in any GameObject of your current Scene to enable timecode synchronization between compatible Live Capture devices connected to the Unity Editor.

To get a functional Timecode Synchronizer, you must use this component along with another component according to the timecode source you need to use: [LTC Timecode Source](ref-component-ltc-timecode-source.md), [NTP Timecode Source](ref-component-ntp-timecode-source.md), or [System Clock Timecode Source](ref-component-system-clock-timecode-source.md).

![](images/ref-component-timecode-synchronizer.png)

## General properties

| Property | Function |
|:---|:---|
| **Display Timecode** | Displays the current timecode in the Game view.<br /><br />**Note:** This timecode display is burnt into the Game view. If you want to export the Game view, you might first need to disable this option. |
| **Timecode Source** | The timecode source to use as the reference for synchronizing all your connected data sources.<br /><br />The selection list includes all timecode source components present in any GameObject of the current Scene. The mention in parentheses indicates the name of the GameObject that holds the component. |
| **Delay** | The delay (in frames) applied to the source timecode. Use a positive value to compensate for high-latency data sources. |

## Timed data sources

| Property | Function |
|:---|:---|
| **Device Name** | The name of the connected data source. |
| **Status** | The current timecode synchronization status of the data source.<br />• **synced** (green): The source data is synchronized<br />• **ahead** (yellow): The source data is ahead of the global time<br />• **behind** (yellow): The source data is behind the global time<br />• **no data** (gray): The source data is missing<br />• **no source** (red): The source data is not available |
| **Buffer** | The sample buffer size (in frames). Larger values may help when synchronizing with high-latency sources at the cost of performance. <br /><br />Adjust the value to minimally overlap with buffers of the other data sources. |
| **Offset** | The time offset (in frames) applied to sample timecodes. |

### List management

| Control | Function |
|:---|:---|
| Handle | Use the handles at the left to manually reorder the list. |
| Checkbox | Enable or disable the synchronization of the corresponding listed data source. |
| **+** (plus) | Add a data source to synchronize among the compatible Live Capture devices currently connected to the Unity Editor. |
| **-** (minus) | Remove the selected data source from the list. |

## Calibrate

Use the Calibrate button to automatically adjust the Delay and all timed data source Buffer values so that all data sources are synchronized.
