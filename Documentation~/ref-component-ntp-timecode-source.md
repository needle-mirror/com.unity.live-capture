# NTP Timecode Source component

Add this component along with the [Timecode Synchronizer component](ref-component-timecode-synchronizer.md) to use a Network Time Protocol (NTP) server as the timecode source.

When the component is enabled or a property is modified, it will poll the NTP server for the current time. You can also manually trigger an update using the **Resynchronize** button.

![](images/ref-component-ntp-timecode-source.png)

## Properties

| Property | Function |
|:---|:---|
| **Frame Rate** | The frame rate of the timecode. |
| **Server Address** | The hostname or IP address of NTP server to get the time from. |
