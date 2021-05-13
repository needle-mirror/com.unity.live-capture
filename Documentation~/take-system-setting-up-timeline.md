# Setting up Timeline

The Live Capture workflow is integrated within **Timeline** by using **Take Recorder Tracks**. Take Recorder Tracks use a Take Recorder component as binding. Take Recorder Tracks contain clips that represent **Slates**. Each slate exposes a list of **Takes**, only one of which can be selected at a time. Selecting a take in a slate activates the preview of the associated **Timeline**. The **Timeline** will play local to the clip's time.

## Creating a Take Recorder Track

Create a **Take Recorder Track** in your Timeline:

1. Select `Add Button > Unity.VirtualProduction > Take Recorder Track` in the Timeline window.
2. Set a Take Recorder from the Scene as the Track binding.

## Creating Slate clips

1. Hover the Slate Track and hit **Menu Button or Right click > Add Slate Playable Asset**.
2. Resize the new Slate clip to cover the time range the recording should take. Create multiple Slates if needed.

## Preparing a Slate to record Takes

When using the **Take Recorder Track** the recording takes place in the selected slate clip. Use the TimelineWindow's playhead to change the selection by sliding it into the range of the slate clip you are targeting.

New Takes are stored in the `Assets/Takes` folder by default. To specify a different output directory:

1. Select a slate clip.
2. Specify the **Directory** in the Inspector window.

You are now ready to record Takes using your app.
