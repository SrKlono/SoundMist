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

    Array<int> playlist = [];
    int playlistIndex = 0;

    /*
        --------------TODO--------------
        - [x] optimize the http requests
        - [x] make playlists work
        - [ ] playlist node/ui
            - [ ] implementation
            - [ ] final design
            - [x] mockup
        - [ ] start using song objects
        - [ ] otimize http node use 


        -------------BACKLOG------------
        - [ ] search a song and continues playing indefinitely the song's station
        - [ ] have controls over the current song
            - [ ] next, previous on playlist
            - [ ] stop, play current song
            - [ ] volume
        

        ---------FUTURE FEATURES--------
        - [ ] can play playlists and tracks through direct link paste
        - [ ] add song to end of current playlist
        - [ ] have controls over the playlist
            - [ ] select any song to start playing immediatly
            - [ ] delete a song from playlist
        - [ ] improve latency by buffering songs
    */

    public override void _Ready()
    {
        thumb = GetNode<TextureRect>("%Thumb");
        background = GetNode<TextureRect>("%Background");
        title = GetNode<Label>("%Title");
        author = GetNode<Label>("%Author");
        player = GetNode<AudioStreamPlayer>("%Player");
        searchBar = GetNode<LineEdit>("%SearchBar");
        soundcloud = GetNode<Soundcloud>("Soundcloud");
        header = GetNode<MarginContainer>("%Header");
        footer = GetNode<MarginContainer>("%Footer");

        searchBar.TextSubmitted += (string query) => {
            playlistIndex = 0;
            if(player.Playing) player.Stop();
            soundcloud.searchSong(query);
        };

        player.Finished += () => {
            if(playlist.Count > 0) {
                soundcloud.playNextSong(playlist[playlistIndex]);
                playlistIndex ++;    
            }
        };

        // node responsiveness
        //header.Resized += () => keepHeight(header, 0.2f);
        //footer.Resized += () => keepHeight(footer, 0.2f);

        // soundcloud signals connect
        soundcloud.titleAndAuthorFound += (value) => {
            title.Text = value[0];
            author.Text = value[1];
        };

        soundcloud.thumbFound += (value) => {
            thumb.Texture = value;
            background.Texture = value;
        };

        soundcloud.mp3URLFound += (value) => {
            player.Stream = value;
            player.Play();
        };

        soundcloud.stationFound += (value) => {
            playlist = value;
            GD.Print(playlist);
        };
    
    }

    private void keepHeight(Control node, float percentage) {
        Vector2 winSize = DisplayServer.WindowGetSize();
        node.CustomMinimumSize = new(0, winSize.Y * percentage);
    }
}
