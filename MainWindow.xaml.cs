using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MegaMusic;

public class Track
{
    public required string Path { get; init; }
    public string Display { get; set; } = "";
    public bool Playing { get; set; }
}

public partial class MainWindow : Window
{
    private static readonly string[] Extensions =
        { ".mp3", ".wav", ".wma", ".m4a", ".aac", ".flac", ".mp4", ".ogg" };

    private readonly MediaPlayer _player = new() { Volume = 0.8 };
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly ObservableCollection<Track> _tracks = new();
    private Track? _current;
    private bool _playing;
    private bool _dragging;
    private bool _updating;

    public MainWindow()
    {
        InitializeComponent();
        TrackList.ItemsSource = _tracks;
        _player.MediaOpened += (_, _) =>
        {
            var total = _player.NaturalDuration.HasTimeSpan ? _player.NaturalDuration.TimeSpan : TimeSpan.Zero;
            SeekSlider.Maximum = Math.Max(total.TotalSeconds, 1);
            SeekSlider.IsEnabled = true;
            DurText.Text = Format(total);
        };
        _player.MediaEnded += (_, _) =>
        {
            var next = NextTrack(1);
            if (next != null) Play(next);
            else { _player.Stop(); SetPlaying(false); }
        };
        _player.MediaFailed += (_, e) =>
        {
            SetPlaying(false);
            TitleText.Text = "Can't play this file";
            ArtistText.Text = e.ErrorException?.Message ?? "";
        };
        _timer.Tick += (_, _) =>
        {
            if (_dragging) return;
            _updating = true;
            SeekSlider.Value = Math.Min(_player.Position.TotalSeconds, SeekSlider.Maximum);
            _updating = false;
            PosText.Text = Format(_player.Position);
        };
        _timer.Start();
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
        var added = new List<Track>();
        foreach (var f in files.Where(f => Extensions.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant())))
        {
            var track = new Track { Path = f, Display = System.IO.Path.GetFileNameWithoutExtension(f) };
            _tracks.Add(track);
            added.Add(track);
        }
        // Start playing the first dropped track unless something is already playing.
        if (added.Count > 0 && !_playing) Play(added[0]);
    }

    private void Play(Track track)
    {
        _current = track;
        TrackList.SelectedItem = track;
        _player.Open(new Uri(track.Path));
        SeekSlider.IsEnabled = false;
        SeekSlider.Value = 0;
        PlayButton.IsEnabled = true;
        ShowInfo(track);
        _player.Play();
        SetPlaying(true);
    }

    private Track? NextTrack(int step)
    {
        if (_current == null) return null;
        int i = _tracks.IndexOf(_current) + step;
        return i >= 0 && i < _tracks.Count ? _tracks[i] : null;
    }

    private void ShowInfo(Track track)
    {
        var info = TrackInfo.Read(track.Path);
        var title = string.IsNullOrWhiteSpace(info.Title)
            ? System.IO.Path.GetFileNameWithoutExtension(track.Path) : info.Title;
        TitleText.Text = title;
        ArtistText.Text = info.Artist;
        AlbumText.Text = info.Album;
        DetailText.Text = string.Join("  •  ",
            new[] { info.Year, info.Genre, info.Bitrate,
                    System.IO.Path.GetExtension(track.Path).TrimStart('.').ToUpperInvariant() }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        Title = $"{title} - MegaMusic";
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e) => TogglePlay();

    private void TogglePlay()
    {
        if (_current == null) return;
        if (_playing) _player.Pause(); else _player.Play();
        SetPlaying(!_playing);
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        var next = NextTrack(1);
        if (next != null) Play(next);
    }

    private void Prev_Click(object sender, RoutedEventArgs e)
    {
        // Restart the current track if already a few seconds in, like most players.
        if (_player.Position.TotalSeconds > 3 && _current != null) { _player.Position = TimeSpan.Zero; return; }
        var prev = NextTrack(-1);
        if (prev != null) Play(prev);
    }

    private void TrackList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TrackList.SelectedItem is Track t) Play(t);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space) { TogglePlay(); e.Handled = true; }
        else if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Control) { ClearQueue(); e.Handled = true; }
        else if (e.Key == Key.Delete && TrackList.SelectedItems.Count > 0)
        {
            foreach (var t in TrackList.SelectedItems.Cast<Track>().ToList()) RemoveTrack(t);
            e.Handled = true;
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => ClearQueue();

    private void ClearQueue()
    {
        if (_current != null) RemoveTrack(_current);
        _tracks.Clear();
    }

    private void RemoveTrack(Track t)
    {
        if (t == _current)
        {
            _player.Stop();
            SetPlaying(false);
            _current = null;
            PlayButton.IsEnabled = false;
            SeekSlider.IsEnabled = false;
            SeekSlider.Value = 0;
            TitleText.Text = "Drop tracks here";
            ArtistText.Text = AlbumText.Text = DetailText.Text = "";
            PosText.Text = DurText.Text = "0:00";
        }
        _tracks.Remove(t);
    }

    private void SetPlaying(bool playing)
    {
        _playing = playing;
        PlayButton.Content = playing ? "Pause" : "Play";
    }

    private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) =>
        _player.Volume = e.NewValue;

    private void Seek_Down(object sender, MouseButtonEventArgs e) => _dragging = true;

    private void Seek_Up(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        _player.Position = TimeSpan.FromSeconds(SeekSlider.Value);
    }

    private void Seek_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_dragging && !_updating) PosText.Text = Format(TimeSpan.FromSeconds(e.NewValue));
    }

    private static string Format(TimeSpan t) =>
        t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
}

// Reads tags through the Windows Shell, so no third-party packages are needed.
public record TrackInfo(string Title, string Artist, string Album, string Year, string Genre, string Bitrate)
{
    public static TrackInfo Read(string path)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application")!;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic folder = shell.NameSpace(System.IO.Path.GetDirectoryName(path)!);
            dynamic item = folder.ParseName(System.IO.Path.GetFileName(path));

            var map = new Dictionary<string, string>();
            for (int i = 0; i < 400; i++)
            {
                string header = folder.GetDetailsOf(null, i);
                if (string.IsNullOrEmpty(header) || map.ContainsKey(header)) continue;
                string value = folder.GetDetailsOf(item, i);
                if (!string.IsNullOrWhiteSpace(value)) map[header] = value.Trim('\u200E', '\u200F', ' ');
            }

            string Get(params string[] keys) =>
                keys.Select(k => map.GetValueOrDefault(k)).FirstOrDefault(v => v != null) ?? "";

            return new TrackInfo(Get("Title"), Get("Contributing artists", "Authors"), Get("Album"),
                Get("Year"), Get("Genre"), Get("Bit rate"));
        }
        catch
        {
            return new TrackInfo("", "", "", "", "", "");
        }
    }
}
