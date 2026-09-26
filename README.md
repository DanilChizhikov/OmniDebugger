# OmniDebugger
[![Unity Version](https://img.shields.io/badge/unity-6000.0+-000.svg)](https://unity.com/releases/editor/archive)

## Overview
OmniDebugger is a runtime cheat and debug panel for Unity. It is built on UI Toolkit, so it draws on top of any
project without pulling in uGUI, TextMeshPro, or any other dependency.

## Table of Contents
- [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [Manual Installation](#manual-installation)
    - [UPM Installation](#upm-installation)
- [License](#license)

## Getting Started

### Prerequisites
- [GIT](https://git-scm.com/downloads)
- [Unity](https://unity.com/releases/editor/archive) 6000.0+

### Manual Installation
1. Download the .unitypackage from the [releases](https://github.com/DanilChizhikov/OmniDebugger/releases/) page.
2. Import com.dtech.omnidebugger.x.x.x.unitypackage into your project.

### UPM Installation
1. Open the manifest.json file in your project's Packages folder.
2. Add the following line to the dependencies section:
    ```json
    "com.dtech.omnidebugger": "https://github.com/DanilChizhikov/OmniDebugger.git",
    ```
3. Unity will automatically import the package.

If you want to set a target version, OmniDebugger uses the `v*.*.*` release tag so you can specify a version like #v1.0.0.

For example `https://github.com/DanilChizhikov/OmniDebugger.git#v1.0.0`.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
