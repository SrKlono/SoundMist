using Godot;
using Godot.Collections;
using System.Linq;
using System.Text;

public partial class Home : Control
{
    TextureRect thumb, background;
    Label title, author;
    AudioStreamPlayer player;
    LineEdit searchBar;
    Soundcloud soundcloud;
    MarginContainer header, footer;
    Button previousBtn, pauseBtn, nextBtn;
    SongProgress songProgress;
    VolumeController volumeController;

    CompressedTexture2D pauseIcon = ResourceLoader.Load<CompressedTexture2D>("res://assets/fluent--pause-32-regular.svg");
    CompressedTexture2D playIcon = ResourceLoader.Load<CompressedTexture2D>("res://assets/fluent--play-32-regular.svg");

    Array<int> playlist = [];
    int playlistIndex = 0;
    int songDuration;

    /*  ------------------------------------TODO------------------------------------


        -------------BACKLOG------------
        - [x] search a song and continues playing indefinitely the song's station
        - [x] have controls over the current song
            - [x] next, previous on playlist
            - [x] stop, play current song
            - [x] volume
        - [x] optimize the http requests
        - [x] make playlists work
        - [ ] otimize http node use 
        - [x] save the volume the user left on 

        ---------FUTURE FEATURES--------
        - [ ] can play playlists and tracks through direct link paste
        - [ ] add song to end of current playlist
        - [ ] have controls over the playlist
            - [ ] select any song to start playing immediatly
            - [ ] delete a song from playlist
        - [ ] improve latency by buffering songs
        - [ ] like songs
        - [ ] playlist node/ui
            - [ ] implementation
            - [ ] final design
            - [x] mockup 
        - [ ] make UI/UX really fun and snappy
        - [x] add a textureprogress to be the slider and make the hslider "invisible"
    */

    ConfigFile config = new();

    public override void _Ready()
    {
        // song info
        thumb = GetNode<TextureRect>("%Thumb");
        background = GetNode<TextureRect>("%Background");
        title = GetNode<Label>("%Title");
        author = GetNode<Label>("%Author");

        // controls
        player = GetNode<AudioStreamPlayer>("%Player");
        searchBar = GetNode<LineEdit>("%SearchBar");
        previousBtn = GetNode<Button>("%Previous");
        pauseBtn = GetNode<Button>("%PausePlay");
        nextBtn = GetNode<Button>("%Next");
        songProgress = GetNode<SongProgress>("%SongProgress");
        volumeController = GetNode<VolumeController>("%VolumeController");

        // API
        soundcloud = GetNode<Soundcloud>("Soundcloud");

        // UI
        header = GetNode<MarginContainer>("%Header");
        footer = GetNode<MarginContainer>("%Footer");

        // Signal connections - controls
        searchBar.TextSubmitted += (string query) =>
        {
            searchBar.Text = "";
            searchBar.PlaceholderText = "Search";
            searchBar.ReleaseFocus();
            playlistIndex = 0;
            player.Stop();
            songProgress.stop();
            soundcloud.searchSong(query);
        };

        player.Finished += playNextSong;

        previousBtn.Pressed += playPreviousSong;

        pauseBtn.Pressed += () =>
        {
            player.StreamPaused = !player.StreamPaused;
            pauseBtn.Icon = player.StreamPaused ? playIcon : pauseIcon;
            if (player.StreamPaused) songProgress.pause();
            else songProgress.resume();
        };

        songProgress.skipSongToValue += (float value) => player.Play(songDuration / 1000 * value);

        nextBtn.Pressed += playNextSong;

        volumeController.SoundValueChanged += (value) => player.VolumeLinear = value;
        volumeController.DragEnded += (value) => {
            config.SetValue("main", "volume", value);
            config.Save("user://soundmist.cfg");
        };

        // node responsiveness
        //header.Resized += () => keepHeight(header, 0.2f);
        //footer.Resized += () => keepHeight(footer, 0.3f);

        // soundcloud signals connect
        soundcloud.SongDataFound += (value) =>
        {
            title.Text = value["title"];
            author.Text = value["author"];
            songDuration = (int)value["duration"].ToFloat();
        };

        soundcloud.ThumbFound += (value) =>
        {
            thumb.Texture = value;
            background.Texture = value;
        };

        soundcloud.Mp3UrlFound += (value) =>
        {
            player.Stream = value;
            player.Play();
            pauseBtn.Icon = pauseIcon;
            songProgress.start(songDuration);
        };

        soundcloud.StationFound += (value) =>
        {
            playlist = value;
        };

        soundcloud.SkipSong += playNextSong;

        soundcloud.NoTracksFound += () => searchBar.PlaceholderText = "Track not found";

        loadConfig();

        searchBar.GrabFocus();
    }

    private void playPreviousSong()
    {
        if (playlistIndex >= 0)
        {
            player.Stop();
            songProgress.stop();
            if (playlistIndex == 0)
            {
                player.Play(0);
                return;
            }
            playlistIndex -= 1;
            soundcloud.playSongById(playlist[playlistIndex]);
        }
    }

    private void playNextSong()
    {
        if (playlist.Count > 0 && playlistIndex <= playlist.Count)
        {
            playlistIndex++;
            player.Stop();
            songProgress.stop();
            soundcloud.playSongById(playlist[playlistIndex]);
        }
    }

    private void keepHeight(Control node, float percentage)
    {
        Vector2 winSize = DisplayServer.WindowGetSize();
        node.CustomMinimumSize = new(0, winSize.Y * percentage);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey eventKey)
            if (eventKey.Pressed && !searchBar.HasFocus())
            {
                searchBar.GrabFocus();
                Input.ParseInputEvent(eventKey);
                searchBar.CaretColumn = searchBar.Text.Length;
            }
    }

    private void loadConfig() {
        Error err = config.Load("user://soundmist.cfg");

        // If the file didn't load, ignore it.
        if (err != Error.Ok)
        {
            volumeController.setVolume(100f);
            return;
        }

        float volume = (float)config.GetValue("main", "volume");
        volumeController.setVolume(volume*100);
    }
}
