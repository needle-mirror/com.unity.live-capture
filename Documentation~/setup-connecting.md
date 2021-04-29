# Connecting the Virtual Camera or Live Capture Face app and the Editor

## Starting the server

1. In the editor, open the **Connections** window by selecting `Menu > Window > Connections`

2. Press the **Create Connection** button and choose `Companion App Server`

3. Set the **Port** if desired but note that:
    - Only one Unity project using Live Capture can use a given port at a time
    - If the port is already in use by another program, including other Unity instances, a message box will appear prompting you to select a free port
    
4. Press the **Start** button

5. Connect to the server from a client app

    Select **Auto Start on Play** to have the server automatically start when entering Play Mode.

## Connecting the app with server discovery

1. On the app launch screen, select the server that you want to connect to and press the **Connect** button.
2. You will know you are connected when the **Connections** window status updates to "Connected".

## Connecting the app manually

1. On the app launch screen, input the IP address and Port number displayed in the **Connections** window under **Available Interfaces** (if there are multiple IP addresses just try each of them).
2. You will know you are connected when the **Connections** window status updates to "Connected".
