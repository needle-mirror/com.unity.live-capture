# Getting Started

## App

Install the Virtual Camera app [via Test Flight](installing-app.md).

### Tracking tips

* AR camera tracking involves image analysis, which requires a clear image. Patterned surfaces are best.

* Tracking quality is reduced when the camera can’t see details, such as when the camera is pointed at a blank wall or the scene is too dark.

* Walk around the environment a bit before trying to record a performance.

## Optimal editor layout

There is a performance cost for every editor window that is visible. To prevent hitching, use a [custom editor layout](https://docs.unity3d.com/Manual/CustomizingYourWorkspace.html) when capturing data from the device. The layout should only have a **Game View** and a **Live Capture** window visible.

![image](images/optimal-editor-layout.png)

## Setting up the Take Recorder

1. Create a new GameObject with a **Take Recorder** component by going to **Menu > GameObject > Live Capture > Take Recorder**.
3. Set the Take name using the **Name** field in the **Slate Database** component.
3. Select an output directory using the **Directory** field in the **Slate Database** component.

## Setting up a scene with the Virtual Camera

### Using the basic Virtual Camera Actor

A setup that uses the **Camera** component.
1. Disable any active cameras in the scene.
2. Add the **Virtual Camera Actor** by going to **Menu > GameObject > Create > Live Capture > Camera > Virtual Camera Actor**.

### Using the Cinemachine Camera Actor

Currently **Cinemachine** is used to drive special camera settings and camera position and aim damping.

1. Add the **Cinemachine Camera Actor** by going to **Menu > Assets > Create > Live Capture > Camera > Cinemachine Camera Actor**.

### Creating a Virtual Camera Device

1. In the **Take Recorder** component, click on the **+** button from the **Capture Devices** list.
2. Select **Virtual Camera Device** to create a child GameObject with a **Virtual Camera Device** component.
3. In the newly created **VirtualCameraDevice** component, set a **Virtual Camera Actor** into the **Actor** field.
4. In the same **VirtualCameraDevice** component, select a connected client from the **Client Device** dropdown.

You are now ready to record Takes with your **Companion App**.

## Setting up Timeline

The Live Capture workflow is integrated within **Timeline** by using **Slate Tracks**. Slate Tracks use a **Slate Database** as binding. Slate Tracks contain clips that represent **Slates**. Each Slate shows a list of **Takes** from the selected directory, only one of which can be selected at a time. Selecting a Take in a Slate activates the preview of the associated **Timeline**. The Timeline will play local to the Slate's clip time, like in the **Control Track**.

### Creating a Slate Track

Create a **Slate Track** in your Timeline:
1. Select **Add Button > Unity.VirtualProduction > Slate Track** in the Timeline window.
2. Set a **Slate Database** from the Scene as the Track binding.

### Creating Slate clips

1. Hover the Slate Track and hit **Menu Button or Right click > Add Slate Playable Asset**.
2. Resize the new Slate clip to cover the time range the recording should take. Create multiple Slates if needed.

### Preparing a Slate to record Takes

When using the **Slate Track** the recording takes place in the selected slate clip. With the TimelineWindow **Preview** on, the **Slate Database** component shows a **Slate** field that contains the list of all slate clips set in the tracks. Select the slate to use using the **Slate** field or use the TimelineWindow's playhead to change the selection by sliding it into the range of the slate clip you are targeting.

### Instantiating from the Live Capture Window
New Takes are stored in the `Assets/Takes` folder by default. To specify a different output directory:
1. Select a Slate clip.
2. Specify the **Directory** in the Inspector window.

If there is no actor in the scene you can instantiate one by opening the **Live Capture** window:
1. Open the **Live Capture** window from **Menu > Window > Live Capture**
2. Select **Actor Type** (a **Cinemachine Camera Actor** will only appear as an option if you have the Cinemachine package installed) and press the **Create** button to automatically add the given actor object to the scene
You are now ready to record Takes in the current Slate with your **Companion App**.

## Render pipeline compatibility

| **Feature**         | **Built-in Render Pipeline** | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ------------------- | ---------------------------- | ----------------------------------- | ------------------------------------------ |
| **Depth Of Field**  | No                           | Yes (1)                             | Yes                                        |
| **Film Format**     | No                           | Yes                                 | Yes                                        |
| **Video Streaming** | No                           | Yes                                 | Yes                                        |


**Notes**:

1. At the moment Depth Of Field is only supported when using the basic Virtual Camera Actor (as opposed to the Cinemachine one).
