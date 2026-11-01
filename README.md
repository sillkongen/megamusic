# MegaMusic

A tiny Windows desktop music player. Drop audio files onto the window and play them. No library, no saved playlists, no accounts.

## Why

Most players want you to build a library or manage playlists. MegaMusic is for when you just want to drop a few tracks in, hit play, and see what's playing.

## Features

- Drag and drop one or many audio files onto the window
- Play / pause, previous / next, and a seekable progress bar
- Volume slider (starts at 80%)
- Track info: title, artist, album, year, genre, bitrate and file type
- Simple queue of dropped tracks (not saved between runs); the next track plays automatically when one ends

Supported extensions: `.mp3 .wav .wma .m4a .aac .flac .mp4 .ogg`. Actual playback depends on the codecs installed in Windows, so some formats (e.g. flac, ogg) may not play on every machine.

## Controls

| Action | How |
|---|---|
| Add tracks | Drop files on the window |
| Play a specific track | Double-click it in the list |
| Play / pause | Play button or `Space` |
| Previous / next | ⏮ / ⏭ buttons (previous restarts the track if more than 3 s in) |
| Seek | Drag the progress bar |
| Remove tracks | Select one or more (`Ctrl`/`Shift`+click) and press `Delete` |
| Clear the queue (stops playback) | "Clear queue" button or `Ctrl+Delete` |

## Requirements

- Windows
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (the project targets `net10.0-windows`)

## Install

Download `MegaMusic-0.1.1-win-x64.zip` and `SHA256SUMS.txt` from the Releases page, then verify and unzip:

```powershell
(Get-FileHash .\MegaMusic-0.1.1-win-x64.zip -Algorithm SHA256).Hash.ToLower()
# compare with the hash in SHA256SUMS.txt
Expand-Archive .\MegaMusic-0.1.1-win-x64.zip -DestinationPath MegaMusic
.\MegaMusic\MegaMusic.exe
```

This build needs the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0). The exe is unsigned, so Windows SmartScreen may warn on first run.

## Build and run

```powershell
dotnet run
```

Or build only:

```powershell
dotnet build
```

## How it works

- **UI:** WPF (`MainWindow.xaml`).
- **Playback:** `System.Windows.Media.MediaPlayer`, which uses Windows' built-in media stack, so there are no third-party packages.
- **Tag reading:** the `Shell.Application` COM object reads file properties by column name (`TrackInfo.Read` in `MainWindow.xaml.cs`). Missing tags are left blank, and the title falls back to the file name.
- **Queue:** an in-memory `ObservableCollection<Track>` bound to the list box.

## Project layout

| File | Purpose |
|---|---|
| `MegaMusic.csproj` | Project file (WPF, .NET 10) |
| `MainWindow.xaml` | Layout |
| `MainWindow.xaml.cs` | Playback, queue and tag-reading logic |
| `App.xaml`, `App.xaml.cs` | WPF application entry point |

## Possible next steps

Shuffle and repeat, drag-to-reorder, persisted volume, album art.
