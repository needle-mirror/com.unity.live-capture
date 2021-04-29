# About Live Capture

Use the Live Capture package to connect to the Unity Virtual Camera and Unity Face Capture companion apps to capture and record camera motion and face performances.

## Available client apps

Two apps compatible with the Live Capture package are currently available (for iOS):

* The [Unity Virtual Camera app](virtual-camera.md) allows you to capture and record camera motion through an iPhone or iPad as if you were physically in the Unity Scene.
* The [Unity Face Capture app](face-capture.md) allows you to capture and record face movements through an iPhone or iPad and apply them on a character in your Unity Scene.

## Live Capture package features

* Manage the [connections](ref-window-connections.md) between your data sources and the Unity Editor.
* Use the [take system](take-system.md) to record, manage, and play back Live Capture takes.
* Use [timecode synchronization](timecode-synchronization.md) to temporally synchronize all your data sources and manage their synchronization status in the Unity Editor

## Installation

To use the features described in this documentation, you must:
* [Install the Live Capture package](#install-app) in your Unity Editor and meet specific system requirements.
* [Install companion apps](#install-package) on your mobile device according to the feature you want to use.
* [Meet specific network requirements](#network-requirements).

<a name="install-package"></a>
### Installing the Live Capture package

To install the Live Capture package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Manual/upm-ui-install.html).

| Requirements |  |
|---|---|
| **Unity Editor**   | **Unity Editor 2020.3.16f1** or later version. |
| **Platform**       | Windows or macOS. |

<a name="install-app"></a>
### Installing the apps on your mobile device

The Unity Virtual Camera and Unity Face Capture apps are currently available on iPhone and iPad.

| App name | Device requirements | Get the app |
|:---|:---|:---|
| **Unity Virtual Camera** | iPad or iPhone with:<br />• iOS 14.5 or higher<br />• ARKit capabilities (implied with the required iOS version)| [![Unity Virtual Camera](images/app-store-badge.png)](https://apps.apple.com/us/app/unity-virtual-camera/id1478175507) |
| **Unity Face Capture** | iPhone or iPad with:<br />• iOS 14.6 or higher<br />• ARKit _face tracking_ capabilities ([device supporting Face ID](https://support.apple.com/en-us/HT209183) **or** [device with an A12 Bionic chip](https://en.wikipedia.org/wiki/Apple_A12)) | [![Unity Face Capture](images/app-store-badge.png)](https://apps.apple.com/us/app/unity-face-capture/id1544159771) |

### Network requirements

* Your mobile devices and your Unity Editor workstation must have access to the **same network**.

* You must **disable** any active **VPNs**.

* On Windows 10, you must set your **Wi-Fi** network to **Private**.

* Your **firewall** must allow your Unity Editor program to get **inbound connections** from external apps of your local network.

[Get more information on how to meet those network requirements in Windows](setup-network.md).

## Known issues and limitations

See the list of current [known issues and limitations](known-issues-limitations.md) that you might experience with the Live Capture package and companion apps, along with some workarounds.
