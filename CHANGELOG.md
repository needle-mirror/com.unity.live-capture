# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0-exp.235] - 2021-04-22

### Added
- [Face Capture] Toggle to mirror the rotation data of the head and eyes
- Play/Stop button in preview mode
- [Companion App] Scan button to discover running servers
- Assisted firewall configuration
- [Virtual Camera] Shows the list of available takes
- Recording API
- Timecode API

### Changed
- Record iterations as track overrides
- Improvements to the server's UX
- CustomPassManager gameObject is not hidden anymore
- In Add Component, Live Capture components now appear under their own section
- Add Device button now allows to add a path as well
- Downgraded Timeline dependency to 1.4.7
- A client can only be assigned to a single device at a time.

### Fixed
- [Face Capture] Fixed head and eyes rotation not being correctly applied in the DefaultMapper
- Unnessary animation output rebuild when a LiveLink was set active
- Issue where CompactList search loses focus
- Time jump on start recording/previewing
- Incorrect time offset in recordings

## [0.3.10-preview.166] - 2021-03-12

### Added
- Global Live/Preview button in TakeRecorder. Allows to preview the recorded take for all active devices.
- New format for the take names and asset names. Can be customized with ',' and wildcards.  
- 'Auto Start server on play' option.
- Face Capture App with samples.

### Changed
- New and improved companion app protocol.
- Use OS assigned ports for video server.
- Precise Ray-Mesh Intersection Tracking For Spatial Focus Mode
- Slate names have been renamed to Shot names

### Fixed
- A regression where recordings stop if the selected take is null.
- The TakeRecorder was not updated on creation until selection was changed.
- When device was removed, the TakeRecorder was throwing an exception and list was in inconsistent state.
- Package was not compiling if the optional inputsystem was not present.
- Virtual Camera record button would not appear after upgrade. 
- UX regressions around preview playback. 

## [0.3.9-preview.1] - 2021-02-12

### Added
- Video streaming support for URP.
- URP support for Film Format. 
- Add the ability to change damping from the client.
- Graphics Raycaster supports object picking.
- Spatial Focus Mode support.
- Focus Plane Visualization Feature.
- Support for custom camera data sources.
- Introduces the TakeRecorder component and slate track

### Changed
- Improved default face mapper inspector.
- Shots, takes and playback are now integrated with Timeline's tracks.
- Rename the package to Live-Capture.
- Reset versioning to a major in 0 because the package is still experimental.
- Ergonomic tilt is sent to server on client initialization.
- New UI to create a Companion App Server. 
- Updated package minimum version to Unity 2020.2 and compatible with URP/HDRP 10. 

### Fixed
- [Companion App] Issue where editing ergonomic tilt would set the pedestal space to global.  
- [Companion App] Damping issue with Cinemachine.
- [Companion App] Ergonomic tilt issue where the setting was not updated without a pose update.  
- VideoServer is disposed on device disable in order to avoid running out of ports
- [Companion App] Rebase rotation on Y axis lock to avoid rotation snapping back upon unlock.
- [Companion App] Fixed record button not being hidden by the setting to hide it.
- Issue where server could not be restarted for a while after being stopped
- A server discovery bug where the sending sockets would be recreated constantly when connected to a VPN or network that has broadcasting disabled, throwing many errors.
- A server discovery bug where it stopped polling the network interface updates. In some cases it was extremely slow and causing performance issues.
