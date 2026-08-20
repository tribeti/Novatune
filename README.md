<div align="center">

# Novatune

**A sleek, modern multimedia player for Windows built with WinUI 3 & .NET 10.**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3%20%2F%20WindowsAppSDK-0078D7?style=flat-square&logo=windows&logoColor=white)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)
[![Author](https://img.shields.io/badge/Author-tribeti-orange.svg?style=flat-square&logo=github)](https://github.com/tribeti)

<br/>

[![Novatune Screenshot](https://i.postimg.cc/jdMs32c7/Screenshot-2026-08-20-102355.png)](https://postimg.cc/sBHnS6mv)

</div>

---

## 🌟 Overview

**Novatune** is a lightweight, feature-rich audio and multimedia player designed specifically for Windows 10 and 11. Powered by **WinUI 3** and **.NET 10**, it offers a native, responsive Fluent Design experience with Mica material backdrops, seamless local playback, online YouTube streaming, global online radio, and live IPTV broadcasting.

---

## ✨ Features

- **Local Music Playback**: Pick audio files or entire music folders, view metadata and album art, and manage your current playback queue.
- **YouTube Streaming**: Search for any song or video on YouTube with fast instant search, automatic thumbnail loading, and background audio streaming.
- **Online Radio Stations**: Discover and tune into thousands of worldwide live radio stations powered by the Radio Browser API with caching.
- **IPTV & Live TV Channels**: Stream live television channels directly in the app.
- **Playlist Management**: Create custom online playlists, save favorites, import playlists, shuffle, and repeat.
- **Full Playback Controls**: Interactive scrubber timeline, volume control, next/previous track navigation, shuffle & repeat modes, and keyboard shortcuts (e.g. `Space` to Play/Pause).
- **Modern Windows 11 UI**: Beautiful Fluent Design featuring native Mica backdrop, dark/light theme integration, and smooth animations.
- **System Tray Integration**: Option to minimize Novatune to the system tray for uninterrupted background listening.

---

## 🛠️ Built With

- **Framework**: [.NET 10](https://dotnet.microsoft.com/) & [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) (WinUI 3)
- **UI & Controls**: [DevWinUI](https://github.com/Ghost1372/DevWinUI), [WinUIEx](https://github.com/dotMorten/WinUIEx), [CommunityToolkit.WinUI](https://github.com/CommunityToolkit/WindowsCommunityToolkit)
- **Architecture**: MVVM with [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- **Media & Streaming**:
  - [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) – Fast YouTube metadata extraction & media stream resolution
  - Radio Browser API – Worldwide live radio directory
  - IPTV Stream Resolver – Live channel streaming

---

## 🚀 Getting Started

### Prerequisites

- **OS**: Windows 10 version 1809 (Build 17763) or Windows 11 (recommended)
- **SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **IDE**: Visual Studio 2022 (v17.12 or newer) with:
  - *.NET Desktop Development* workload
  - *Windows App SDK C# Templates* installed

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
   dotnet build
   ```

4. **Run**:
   - Press <kbd>F5</kbd> in Visual Studio or run `Novatune.App`.

---

## 🤝 Third-Party Open Source Credits
Special thanks to the authors and maintainers of the following open-source projects:

| Library | Author / Organization | License | Description |
| :--- | :--- | :--- | :--- |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | Microsoft & CommunityToolkit | MIT | MVVM architecture & source generators |
| [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) | Alexey Golub (Tyrrrz) | MIT | YouTube stream & metadata extraction |
| [WinUIEx](https://github.com/dotMorten/WinUIEx) | Morten Nielsen | MIT | Windowing & system extensions for WinUI 3 |
| [DevWinUI](https://github.com/Ghost1372/DevWinUI) | Ghost1372 | MIT | Fluent controls & modern UI components |

> For a full list of third-party libraries and dependencies, see [THIRD_PARTY_CREDITS.md](THIRD_PARTY_CREDITS.md).

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

Copyright © 2025 **[tribeti](https://github.com/tribeti)**. All rights reserved.
