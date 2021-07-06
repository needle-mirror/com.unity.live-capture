# Known limitations
## Feature limitations

* Cinemachine does not support physical camera settings when using URP.
* Cinemachine damping only works in play mode.
* Spatial Focus Mode does not account for skinned mesh blend shapes.
* Spatial Focus Mode cannot track meshes that are not [readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html). Note that statically batched meshes are not readable. It is possible to disable static batching through Unity's Player settings under Other Settings. It is also possible to uncheck the "Static" toggle on specific GameObjects.

## Performance
* The solution works in both Edit Mode and Play Mode, but there can be performance issues in Edit Mode, particularly when depth of field is enabled (i.e. any Focus Mode other than "Clear").
