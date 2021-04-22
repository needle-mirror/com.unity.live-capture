## Setting up Timeline

The Live Capture workflow is integrated within **Timeline** by using **Device Tracks**. Device Tracks use an Actor as binding and operate a **VirtualProductionDevice**. Device Tracks contain clips that represent **Shots**. Each Shot stores a list of **Takes**, only one of which can be selected at a time. Selecting a Take in a Shot activates the preview of the associated **AnimationClip**. The **AnimationClip** will play local to the Shot's time.

### Creating a Virtual Camera Device Track

Create a **Virtual Camera Device Track** in your Timeline:

1. Select `Add Button > Unity.VirtualProduction > Virtual Camera Device Track` in the Timeline window.
2. Set a Camera Actor from the Scene as the Track binding.

### Creating Shots

1. Hover the Virtual Camera Device Track and hit `Menu Button or Right click > Add Shot`.
2. Resize the new Shot clip to cover the time range the recording should take. Create multiple Shots if needed.

### Preparing Shot to record Takes

Set Shot as current:

* Select the Timeline's playhead and slide it into the range of the Shot you're targeting.
  OR
* Activate the `Preview` in the Timeline window and select a Shot from the list in the **Live Capture** window.

New Takes are stored in the `Assets` folder by default. To specify a different output directory:

1. Select a Shot clip.
2. Specify the `Directory` in the Inspector window.

You are now ready to record Takes in the current Shot with your app.