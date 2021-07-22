# Getting Started

## Tracking tips

Tracking works best when the relative position of the head and device is fixed, as in the following scenarios:

* (Preferred) The device is mounted to the performer's head via a helmet.
* The device is mounted to a static surface and the performer does not move their head or look around.

In this case the **Head Position** and **Head Rotation** channels on the **ARKit Face Device** should be disabled.

## Samples

There are basic sample characters you can access by opening the **Package Manager** window, clicking on the **Live Capture** package, and importing the **ARKit Face Capture** sample.

## Preparing a scene for face capture

### Setting up a character

See the documentation for the [Default Face Mapper](ref-component-arkit-default-face-mapper.md).

### Creating a Take Recorder

1. Create a new GameObject with a **Take Recorder** component by going to **Menu > GameObject > Live Capture > Take Recorder**.
3. Set the shot name using the **Shot** field in the **Take Recorder** component.
3. Select an output directory using the **Directory** field in the **Take Recorder** component.

### Creating an ARKit Face Device

1. In the **Take Recorder** component, click on the **+** button from the **Capture Devices** list.
2. Select **ARKit Face Device** to create a child GameObject with an **ARKit Face Device** component.
3. In the newly created **ARKit Face Device** component, assign a **Face Actor** to the **Actor** field.
4. In the same **ARKit Face Device** component, select a connected client from the **Client Device** dropdown.
