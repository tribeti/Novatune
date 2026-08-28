<div align="center">

# Novatune

**A sleek, modern multimedia player for Windows built with WinUI 3 & .NET 10.**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3%20%2F%20WindowsAppSDK-0078D7&logo=windows&logoColor=white)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Author](https://img.shields.io/badge/Author-tribeti-orange.svg?slogo=github)](https://github.com/tribeti)

<br/>

[![Novatune Screenshot](https://i.postimg.cc/jdMs32c7/Screenshot-2026-08-20-102355.png)](https://postimg.cc/sBHnS6mv)

</div>

---

**Novatune** is a lightweight, feature-rich audio and multimedia player designed specifically for Windows 10 and 11. Powered by **WinUI 3** and **.NET 10**, it offers a native, responsive Fluent Design experience with Mica material backdrops, seamless local playback, online YouTube streaming, global online radio, and live IPTV broadcasting.

---

## ✨ Features

- **Local Media Playback**: Play audio (`.mp3`, `.flac`) and video (`.mp4`, `.wmv`, `.mkv`) files, automatically extract ID3/music metadata and album art thumbnails, and manage your current playback queue.
- **YouTube Streaming & Playlists**: Search songs and videos with instant query suggestions, stream high-bitrate audio in the background, and import YouTube playlists with persistent local storage and auto-sync.
- **Online Radio Stations**: Discover and tune into thousands of worldwide live radio stations powered by the Radio Browser API.
- **IPTV & Live TV Broadcasting**: Stream live television channels directly with support for HLS adaptive streams.
- **Global Search in Title Bar**: Unified search box integrated right into the window title bar for searching YouTube, radio stations, and IPTV channels.
- **Rich Playback Controls**: Interactive scrubber timeline with formatted time tooltips, Play/Pause (`Space`), Next/Previous, Shuffle, Repeat, Volume control, Playback Speed (0.25x – 2.0x), and Audio Output Device switcher.
- **Modern Windows 11 UI**: Fluent Design featuring native Mica backdrop, dark/light theme integration, animated visual icons, and responsive layouts.
- **System Tray**: Minimize-to-tray on close, and quick tray context menu (`Open` / `Quit`).
---

## Tech stack

- **Framework**: [.NET 10](https://dotnet.microsoft.com/) & [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) (WinUI 3)
- **Architecture**: MVVM with [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) & [Microsoft.Extensions.DependencyInjection](https://github.com/dotnet/runtime)
- **UI & Windowing**: [WinUIEx](https://github.com/dotMorten/WinUIEx), [CommunityToolkit.WinUI](https://github.com/CommunityToolkit/WindowsCommunityToolkit) (Primitives & Extensions)
- **Media & Streaming**:
  - [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) – YouTube metadata extraction, playlist retrieval & audio stream extraction
  - [Radio Browser API](https://www.radio-browser.info/) – Worldwide live radio directory with mirror pooling
  - IPTV Stream Resolver – Live TV channel streaming & HLS playback (`AdaptiveMediaSource`)
---

## Getting Started

### Prerequisites

- **OS**: Windows 10 version 2004 (Build 19041) or higher / Windows 11 (recommended)
- **SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (net10.0-windows10.0.26100.0)
- **IDE**: Visual Studio 2022 (v18.0 or newer) with:
  - *WinUI Application Development* workload
  - *Desktop development with C++* workload

### Clone & Run

1. **Clone the repository**:
   ```bash
   git clone https://github.com/tribeti/Novatune.git
   cd Novatune
   ```

2. **Open in Visual Studio**:
   - Open `Novatune.slnx` or `Novatune.App/Novatune.App.csproj`.

3. **Restore NuGet Packages & Build**:
   ```bash
   dotnet restore
   dotnet build -p:Platform=x64
   ```

4. **Run**:
   - Press <kbd>F5</kbd> in Visual Studio or run `Novatune.App`.

---

## Third-Party Open Source Credits
Special thanks to the authors and maintainers of the following open-source projects:

| Library | Author / Organization | License | Description |
| :--- | :--- | :--- | :--- |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | Microsoft & CommunityToolkit | MIT | MVVM architecture, observable properties & commands |
| [CommunityToolkit.WinUI](https://github.com/CommunityToolkit/WindowsCommunityToolkit) | Microsoft & CommunityToolkit | MIT | UI controls & visual tree extensions |
| [WinUIEx](https://github.com/dotMorten/WinUIEx) | Morten Nielsen | MIT | Windowing, window management & system tray extensions |
| [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) | Alexey Golub (Tyrrrz) | MIT | YouTube stream & metadata extraction |
| [Microsoft.WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK) | Microsoft | MIT | Windows App SDK / WinUI 3 UI components |

> For a full list of third-party libraries and dependencies, see [THIRD_PARTY_CREDITS.md](THIRD_PARTY_CREDITS.md).

---

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

Copyright © 2025 **[tribeti](https://github.com/tribeti)**. All rights reserved.
