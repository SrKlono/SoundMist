using System.Linq;
using System.Text;
using Godot;
using Godot.Collections;

public partial class Soundcloud : Node
{
    [Signal] public delegate void songDataFoundEventHandler( Dictionary<string, string> value );
    [Signal] public delegate void thumbFoundEventHandler( ImageTexture value);
    [Signal] public delegate void mp3URLFoundEventHandler( AudioStreamMP3 value );
    [Signal] public delegate void stationFoundEventHandler( Array<int> value );

    HttpRequest searchRequest, thumbRequest, streamURLRequest, mp3Request, stationRequest, singleSongRequest;
    string baseURL = "https://api-v2.soundcloud.com/search?q=";
    string after = "&client_id=ZhY1ZEiuWYBY7krRgevXg7fRXCIaRw6r&offset=0&linked_partitioning=1&app_locale=pt_BR";
    string stationBaseURL = "https://api-v2.soundcloud.com/resolve?url=https%3A//soundcloud.com/discover/sets/track-stations%";
    string stationAfter = "&client_id=7mtmc4DOqfwBtnxQCjHDXvghFjF9MQMM&app_version=1756304203&app_locale=pt_BR";
    
    /*  
        3A1983589551
          1983589551
        example station 
        
        https://api-v2.soundcloud.com/tracks/1983589551?client_id=ZhY1ZEiuWYBY7krRgevXg7fRXCIaRw6r
        https://api-v2.soundcloud.com/resolve?url=https%3A//soundcloud.com/discover/sets/track-stations%3A1983589551&client_id=7mtmc4DOqfwBtnxQCjHDXvghFjF9MQMM&app_version=1756304203&app_locale=pt_BR

        the station only grabs the songs IDs, the fetching of the song data only happens as the user scrolls down
        so my approach should be similar, fetch ~10 songs and stretch that as they play, to always keep +10 songs
        in queue
    */
    
    public override void _Ready()
    {
        searchRequest = GetNode<HttpRequest>("SearchRequest");
        thumbRequest = GetNode<HttpRequest>("ThumbRequest");
        streamURLRequest = GetNode<HttpRequest>("StreamURLRequest");
        mp3Request = GetNode<HttpRequest>("MP3Request");
        stationRequest = GetNode<HttpRequest>("StationRequest");
        singleSongRequest = GetNode<HttpRequest>("SongRequest");

        searchRequest.RequestCompleted += processSearchRequest;
        thumbRequest.RequestCompleted += processThumbRequest;
        streamURLRequest.RequestCompleted += processStreamURLRequest;
        mp3Request.RequestCompleted += processMP3Request;
        stationRequest.RequestCompleted += processStationRequest;
        singleSongRequest.RequestCompleted += processSingleSong;
    }

    public void searchSong(string query, int songNum = 1) {
        searchRequest.Request(baseURL + query + "&limit=" + songNum + after );
    }

    public void playSongById(int songId) {
        singleSongRequest.Request("https://api-v2.soundcloud.com/tracks/" + songId + "?client_id=ZhY1ZEiuWYBY7krRgevXg7fRXCIaRw6r");
    }

    private void processSingleSong(long result, long responseCode, string[] headers, byte[] body) {
        if (result != (long)HttpRequest.Result.Success) {
            GD.PushError("Song request couldn't be completed.");
            return;
        }

        Dictionary songData = Json.ParseString(Encoding.UTF8.GetString(body)).AsGodotDictionary();
        streamURLRequest.Request(processSong(songData) + "?client_id=ZhY1ZEiuWYBY7krRgevXg7fRXCIaRw6r");
    }

    private void processSearchRequest(long result, long responseCode, string[] headers, byte[] body) {
        if (result != (long)HttpRequest.Result.Success) {
            GD.PushError("Song request couldn't be completed.");
            return;
        }
        
        Dictionary json = Json.ParseString(Encoding.UTF8.GetString(body)).AsGodotDictionary();
        Array songList = (Array)json["collection"];

        Dictionary songData = (Dictionary)songList[0];
        
        streamURLRequest.Request(processSong(songData) + "?client_id=ZhY1ZEiuWYBY7krRgevXg7fRXCIaRw6r");
        stationRequest.Request(stationBaseURL + "3A" + (int)songData["id"] + stationAfter);
    }

    private void processStationRequest(long result, long responseCode, string[] headers, byte[] body) {
        Dictionary json = Json.ParseString(Encoding.UTF8.GetString(body)).AsGodotDictionary();
        Array stationSongList = (Array)json["tracks"];
        
        Array<int> IdList = [];
        
        foreach (Dictionary item in stationSongList.Select(v => (Dictionary)v)) {
            IdList.Add((int)item["id"]);
        }

        EmitSignal(Soundcloud.SignalName.stationFound, IdList);
    }

    private void processThumbRequest(long result, long responseCode, string[] headers, byte[] body) {
        if (result != (long)HttpRequest.Result.Success) {
            // todo check headers for the input

            GD.PushError("Thumb couldn't be downloaded.");
            return;
        }
        
        Image img = new();
        var error = img.LoadJpgFromBuffer(body);
        if(error != Error.Ok) GD.PushError("Couldn't load thumb");
        
        ImageTexture tex = ImageTexture.CreateFromImage(img);

        EmitSignal(Soundcloud.SignalName.thumbFound, tex);
    }
    
    private void processStreamURLRequest(long result, long responseCode, string[] headers, byte[] body) {
        Dictionary json = Json.ParseString(Encoding.UTF8.GetString(body)).AsGodotDictionary();        
        mp3Request.Request(json["url"].ToString());
    }

    private void processMP3Request(long result, long responseCode, string[] headers, byte[] body) {
        AudioStreamMP3 songAudio = new();
        songAudio = AudioStreamMP3.LoadFromBuffer(body);
        
        EmitSignal(Soundcloud.SignalName.mp3URLFound, songAudio);
    }

    private string processSong(Dictionary songData) {
        string thumbURL = songData["artwork_url"].ToString();
        string trackTitle = songData["title"].ToString();
        string trackAuthor = "unknown";
        string duration = songData["duration"].ToString();

        GD.Print(songData);

        // dont know WHY TF this works only inverted(!), but it DOES, and it doesn't make any sense
        if(!songData.ContainsKey("publisher_metadata")) {
            trackAuthor = ((Dictionary)songData["publisher_metadata"])["artist"].ToString();
        } else {
            trackAuthor = ((Dictionary)songData["user"])["username"].ToString();
        }

        thumbRequest.Request(thumbURL.Replace("large", "t500x500"));

        Array transcodings = (Array)((Dictionary)songData["media"])["transcodings"];

        string streamURL = ""; // catch error here later
        foreach (Dictionary item in transcodings.Select(v => (Dictionary)v)) {
            if(((string)item["url"]).Contains("stream/progressive"))
                streamURL = (string)item["url"];
        }

        EmitSignal(
            Soundcloud.SignalName.songDataFound,
            new Dictionary<string, string>{
                {"title", trackTitle}, 
                {"author", trackAuthor},
                {"duration", duration}
            }
        );

        return streamURL;
    }
}