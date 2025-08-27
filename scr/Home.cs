using Godot;
using Godot.Collections;
using System.Linq;
using System.Text;

public partial class Home : Control
{
    TextureRect thumb, background;
    Label title, author;
    HttpRequest httpRequest;
    AudioStreamPlayer player;
    Dictionary song;
    LineEdit searchBar;
    string mp3URL;

    class Song {
        string name, author, streamURL;
        ImageTexture thumbTexture;
    }
    Array playlist = [];

    string baseURL = "https://api-v2.soundcloud.com/search?q=";
    int songNumPerFetch = 4; // its acctually the index number of songs starting from 0, so 4 songNumPerFetch = 5 songs
    string after = "&client_id=ZhY1ZEiuWYBY7krRgevXg7fRXCIaRw6r&limit=&offset=0&linked_partitioning=1&app_locale=pt_BR";

    public override void _Ready()
    {
        thumb = GetNode<TextureRect>("%Thumb");
        background = GetNode<TextureRect>("%Background");
        title = GetNode<Label>("%Title");
        author = GetNode<Label>("%Author");
        player = GetNode<AudioStreamPlayer>("%Player");
        searchBar = GetNode<LineEdit>("%SearchBar");
        httpRequest = GetNode<HttpRequest>("HTTPRequest");

        searchBar.TextSubmitted += searchSong;
    }

    private void searchSong(string query) {
        if(player.Playing) player.Stop();
        httpRequest.RequestCompleted += OnRequestCompleted;
        httpRequest.Request(baseURL + query + after);
    }
    
    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        Dictionary json = Json.ParseString(Encoding.UTF8.GetString(body)).AsGodotDictionary();
        //GD.Print(json["collection"]);
        Array songList = (Array)json["collection"];

        song = (Dictionary)songList[0];

        string thumbURL = song["artwork_url"].ToString();
        string trackTitle = song["title"].ToString();
        string trackAuthor;

        // dont know WHY TF this works only inverted(!), but it DOES. It doesn't make sense
        if(!song.ContainsKey("publisher_metadata")) {
            trackAuthor = ((Dictionary)song["publisher_metadata"])["artist"].ToString();
        } else {
            trackAuthor = ((Dictionary)song["user"])["username"].ToString();
        }

        httpRequest.RequestCompleted -= OnRequestCompleted;

        httpRequest.RequestCompleted += OnThumbRequestCompleted;
        httpRequest.Request(thumbURL.Replace("large", "t500x500"));

        title.Text = trackTitle;
        author.Text = trackAuthor;
    }

    private void OnThumbRequestCompleted(long result, long responseCode, string[] headers, byte[] body) {
        if (result != (long)HttpRequest.Result.Success) {
            GD.PushError("Image couldn't be downloaded. Try a different image.");
        }
        
        Image img = new();
        var error = img.LoadJpgFromBuffer(body);
        if(error != Error.Ok) GD.PushError("Couldn't load thumb");
        
        ImageTexture tex = ImageTexture.CreateFromImage(img);

        httpRequest.RequestCompleted -= OnThumbRequestCompleted;
        httpRequest.RequestCompleted += OnStreamRequestCompleted;

        Array transcodings = (Array)((Dictionary)song["media"])["transcodings"];

        string streamURL = ""; // catch error here later
        foreach (Dictionary item in transcodings.Select(v => (Dictionary)v)) {
            if(((string)item["url"]).Contains("stream/progressive"))
                streamURL = (string)item["url"];
        }

        httpRequest.Request(streamURL + "?client_id=ZhY1ZEiuWYBY7krRgevXg7fRXCIaRw6r");

        thumb.Texture = tex;
        background.Texture = tex;
    }

    private void OnStreamRequestCompleted(long result, long responseCode, string[] headers, byte[] body) {
        Dictionary json = Json.ParseString(Encoding.UTF8.GetString(body)).AsGodotDictionary();

        httpRequest.RequestCompleted -= OnStreamRequestCompleted;
        httpRequest.RequestCompleted += OnMp3RequestCompleted;

        mp3URL = json["url"].ToString();
        GD.Print("MP3URL: " + mp3URL);

        httpRequest.Request(mp3URL);
    }

    private void OnMp3RequestCompleted(long result, long responseCode, string[] headers, byte[] body) {
        AudioStreamMP3 songAudio = new();
        songAudio = AudioStreamMP3.LoadFromBuffer(body);

        httpRequest.RequestCompleted -= OnMp3RequestCompleted;
        
        player.Stream = songAudio;
        player.Play();
    }
}
