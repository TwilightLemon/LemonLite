<div align=center>
<img src="https://github.com/TwilightLemon/LemonLite/raw/refs/heads/master/LemonLite/Resources/icon.ico" width="128" height="128"/>

# Lemon Lite (Preview)
A lyric viewer powered by Lemon App, integrated with SMTC.  
✨ *Keep Simple and Delicate* ✨

[<img src="https://img.shields.io/badge/license-GPL%203.0-yellow"/>](LICENSE.txt)
![C#](https://img.shields.io/badge/lang-C%23-orange)
![WPF](https://img.shields.io/badge/UI-WPF-b33bb3)
![GitHub Repo stars](https://img.shields.io/github/stars/TwilightLemon/LemonLite)  
<a href="https://apps.microsoft.com/store/detail/9pjqr6hb006q?launch=true&mode=full"><img src="https://get.microsoft.com/images/en-us%20dark.svg" style="width: 150px;"/></a>

</div>

## Introduction
**Lemon Lite** is a lightweight lyric viewer that integrates with Windows [System Media Transport Controls (SMTC)](https://learn.microsoft.com/en-us/uwp/api/windows.media.systemmediatransportcontrols). It automatically detects media playback from any SMTC-compatible player (e.g., Spotify, QQ Music, etc.), fetches metadata from online sources, and displays lyrics with beautiful synchronized animations.

## Features
- 🎵 **SMTC Integration**: Automatically detects currently playing media through Windows SMTC and fetches matching lyrics online.
- 🪄 **Animated Lyrics**: Enjoy rich lyric effects with word-by-word highlights and smooth progress animations.
- 🌈 **Modern UI Design**: Built with fluent-style acrylic and Mica effects. Fully supports light and dark mode.
- 🖥️ **Desktop Lyric**: Display lyrics on your desktop with customizable styles.
- 🔔 **System Tray Resident**: Stays quietly in the system tray when idle, appears automatically when music plays.
- 🚀 **Lightweight**: A minimalistic application that focuses on lyric viewing without the complexity of a full music player.

## Requirements
- Windows 10 (build 17763) or later
- .NET 8.0 Runtime
- A media player that supports SMTC

## Installation
### Microsoft Store (Recommended)
<a href="https://apps.microsoft.com/store/detail/9pjqr6hb006q?launch=true&mode=full"><img src="https://get.microsoft.com/images/en-us%20dark.svg" style="width: 150px;"/></a>

### Manual Build
1. Clone this repository
2. Open `LemonLite.slnx` in Visual Studio 2022
3. Build and run the project

## Usage
1. Launch Lemon Lite — it will minimize to the system tray.
2. Start playing music in any SMTC-compatible player.
3. The lyric window will automatically appear with synchronized lyrics.
4. Right-click the tray icon to access settings or exit the app.

## TO-DO List
- Settings page for user preferences.
- Support for more lyric formats and sources.
- Complete theme adaptation for all components.