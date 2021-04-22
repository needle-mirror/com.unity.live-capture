# Using video streaming

## Configuring video streaming server

1. Find the `Video Server` section in the **Virtual Camera Device**
2. In the video server section, make sure the `Camera` property is set to the virtual camera actor's camera
3. Use the following fields to configure the video server:

| **Field**                | **Function**                                                 |
| :----------------------- | :----------------------------------------------------------- |
| __Camera__               | Sets the camera shown by the video stream. |
| __Resolution__           | Sets the base resolution of the video stream. |
| __Resolution Profile__   | Scales the `Resolution` to match a common vertical resolution while preserving the aspect ratio. Using a smaller resolution can decrease latency and improve networking performance. |
| __Frame Rate__           | Sets the framerate of the video stream. Typically this should match the refresh rate of the devices used to view the video stream, in order to minimize latency. |
| __Quality__              | Sets the bit-rate of the video stream. Using a lower quality can improve networking performance. |
| __Prioritize Latency__   | Attempt to minimize latency at the cost of performance. Disable if you are having framerate issues. |

## Advanced video streaming

The video servers use the RTSP protocol to transport the video stream, so it is possible to connect any RTSP client to the video streaming server.
