# Novatune

A modern Windows audio player application built with WinUI 3 and .NET 8. App2 allows you to play local audio files, stream music from YouTube, and manage your playlists with a rich, user-friendly interface.

## Features

- **Local Audio Playback**: Browse and play audio files from your device, view metadata, and manage favorites.
- **YouTube Streaming**: Search for YouTube videos and stream audio directly within the app.
- **Playlist Management**: Create, shuffle, and repeat playlists.
- **Modern UI**: Built with WinUI 3 for a responsive and attractive user experience.
- **Media Controls**: Full-featured playback controls, including seek, volume, and playback state.
- **Favorites**: Mark and manage your favorite tracks.

## Getting Started

### Prerequisites
- Windows 10 version 17763 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or later (with UWP/WinUI workloads)

### Build and Run
1. Clone the repository:git clone <your-repo-url>
2. Open the solution in Visual Studio.
3. Restore NuGet packages.
4. Build and run the project.

## Usage
- **Local Files**: Use the folder picker to add your music library. Select tracks to play, or mark them as favorites.
- **Online (YouTube)**: Use the search bar to find YouTube videos. Click a result to stream its audio.
- **Playback Controls**: Use the media controls to play, pause, skip, shuffle, or repeat tracks.

## Technologies Used
- .NET 8
- WinUI 3
- [LibVLCSharp](https://github.com/videolan/libvlcsharp) (for media playback)
- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) (for YouTube integration)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)

## Third-Party Libraries and Credits
This project makes use of the following open source libraries:

- [LibVLCSharp](https://github.com/videolan/libvlcsharp) - LGPL 2.1
- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) - MIT
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MIT
- [WinUIEx](https://github.com/dotMorten/WinUIEx) - MIT

Please refer to each library's repository for their respective licenses and attributions.

## License
This project is for educational and personal use. See [LICENSE](LICENSE) for more information.

---
