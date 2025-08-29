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

    Array<int> playlist = [];
    int playlistIndex = 0;
    int songDuration;

    /*  ------------------------------------TODO------------------------------------


        -------------BACKLOG------------
        - [x] search a song and continues playing indefinitely the song's station
        - [ ] have controls over the current song
            - [x] next, previous on playlist
            - [x] stop, play current song
            - [ ] volume
        - [x] optimize the http requests
        - [x] make playlists work
        - [ ] otimize http node use 

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
        - [ ] start using song objects
        - [ ] make UI/UX really fun and snappy
        - [ ] add a textureprogress to be the slider and make the hslider "invisible"
    */

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

        // API
        soundcloud = GetNode<Soundcloud>("Soundcloud");

        // UI
        header = GetNode<MarginContainer>("%Header");
        footer = GetNode<MarginContainer>("%Footer");

        // Signal connections - controls
        searchBar.TextSubmitted += (string query) => {
            playlistIndex = 0;
            player.Stop();
            songProgress.stop();
            soundcloud.searchSong(query);
        };

        player.Finished += playNextSong;

        previousBtn.Pressed += playPreviousSong;

        pauseBtn.Pressed += () => {
            player.StreamPaused = !player.StreamPaused;
            if(player.StreamPaused) songProgress.stop();
            else songProgress.resume();
        };

        nextBtn.Pressed += playNextSong;

        // node responsiveness
        //header.Resized += () => keepHeight(header, 0.2f);
        //footer.Resized += () => keepHeight(footer, 0.2f);

        // soundcloud signals connect
        soundcloud.songDataFound += (value) => {
            title.Text = value["title"];
            author.Text = value["author"];
            songDuration = (int)value["duration"].ToFloat();
        };

        soundcloud.thumbFound += (value) => {
            thumb.Texture = value;
            background.Texture = value;
        };

        soundcloud.mp3URLFound += (value) => {
            player.Stream = value;
            player.Play();
            songProgress.start(songDuration);
        };

        soundcloud.stationFound += (value) => {
            playlist = value;
        };
    
    }

    private void playPreviousSong() {
        if(playlistIndex >= 0) {
            player.Stop();
            if(playlistIndex == 0) {
                player.Play(0);
                return;
            }
            playlistIndex -= 1;
            soundcloud.playSongById(playlist[playlistIndex]);
        }
    }

    private void playNextSong() {
        if(playlist.Count > 0 && playlistIndex <= playlist.Count) {
            playlistIndex ++;
            player.Stop();
            soundcloud.playSongById(playlist[playlistIndex]);
        }
    }

    private void keepHeight(Control node, float percentage) {
        Vector2 winSize = DisplayServer.WindowGetSize();
        node.CustomMinimumSize = new(0, winSize.Y * percentage);
    }
}
