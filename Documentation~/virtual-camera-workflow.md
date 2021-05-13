# Virtual Camera Workflow

**Starting camera position**

Position and orient the camera actor in the scene to configure the starting position and rotation (only accounts for y-rotation) of the virtual camera.

**Anchoring/Platforming**

To anchor the camera to moving object (e.g. dolly cart, car, boat), parent the Actor game object in the scene to the moving object.

**Repositioning in physical space**

Disable AR tracking using the button in the top right to enter a state where tracker movement and rotation around the Y-axis in the physical environment does not translate to movement in the virtual scene, essentially "freezing" the virtual camera to allow the user to reposition in space.

**Control camera settings from the editor**

All camera state can be controlled from the editor. This is useful if you want to have an "operator" workflow where the individual with the device only has to worry about controlling the camera.
