# Connect a physical device with the Editor

## Create a Connection in the Editor

1. In the editor, open the **Connections** window: select **Menu** > **Window** > **Live Capture** > **Connections**.

2. Click on the **+** (plus) button and select the type of Connection to add.

3. Select the newly created Connection.

4. Set the **Port** if necessary.  
   **Note:** Only one Unity project using Live Capture can use a given port at a time. If the port is already in use by another program, including other Unity instances, a message should appear, prompting you to select a free port.

5. Enable the Connection using its toggle button.

6. Connect to the Editor from the physical device or app.

7. In the Unity Editor, verify you are connected: the device name should appear in the **Connections** window under the **Connected devices** section.

8.  Select **Auto Start on Play** to have the server automatically start when entering Play Mode.

## Companion app connection

How to connect a companion app such as the Virtual Camera or Face Capture app to the Unity Editor.

### Connection with server discovery

On the app launch screen, select the Unity Editor Connection you want to use and press the **Connect** button.

### Manual connection

On the app launch screen, enter the IP address and Port number displayed in the **Connections** window under **Available Interfaces**.

If there are multiple IP addresses, try each of them until you get the connection.
