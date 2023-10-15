using Godot;
using Godot.Collections;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

public partial class TwitchConnection : Node
{
    public const string TwitchURL = "wss://irc-ws.chat.twitch.tv:443";

    public const string TwitchRedirectURL = "http://localhost:3000/";
    public const string TwitchAuthURL = "https://id.twitch.tv/oauth2/authorize";
    public const string TwitchTokenURL = "https://id.twitch.tv/oauth2/token";

    private WebSocketPeer _webSocket;

    private bool _isOpened = false;
    private WebSocketPeer.State _state = WebSocketPeer.State.Closed;

    private TwitchVariables _variables = new();
    private HttpRequest _request;
    private string _stateParameter;

    [Signal]
    public delegate void OnMessageReceivedEventHandler(string user, string message);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        return;
        base._Ready();

        ProcessMode = ProcessModeEnum.Always;

        _stateParameter = ((Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();

        _request = new HttpRequest();
        AddChild(_request);

        OS.ShellOpen(GetCodeURI());
        StartLocalWebServer();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        _webSocket?.Close();
    }

    private void StartLocalWebServer(bool tokenRequest = false)
    {
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add(TwitchRedirectURL);
        httpListener.Start();

        if (tokenRequest)
            httpListener.BeginGetContext(new AsyncCallback(IncomingHttpTokenRequest), httpListener);
        else
            httpListener.BeginGetContext(new AsyncCallback(IncomingHttpRequest), httpListener);
    }

    private void IncomingHttpTokenRequest(IAsyncResult result)
    {
        // get back the reference to our http listener
        var httpListener = (HttpListener)result.AsyncState;

        // fetch the context object
        var httpContext = httpListener.EndGetContext(result);

        // the context object has the request object for us, that holds details about the incoming request
        var httpRequest = httpContext.Request;

        var streamReader = new StreamReader(httpRequest.InputStream);
        GD.Print(streamReader.ReadToEnd());

        GD.Print(httpRequest.Url);
        GD.Print(httpRequest.RemoteEndPoint.ToString());
        GD.Print(httpRequest.LocalEndPoint.ToString());
        foreach (var header in httpRequest.Headers)
        {
            GD.Print(header.ToString());
        }

        var uriBuilder = new UriBuilder($"{httpRequest.Url}");
        var code = httpRequest.QueryString.Get("code");
        var state = httpRequest.QueryString.Get("state");
        var accessToken = httpRequest.QueryString.Get("access_token");

        _variables.AccessToken = string.IsNullOrEmpty(httpRequest.Url.Fragment)
            ? _variables.AccessToken
            : httpRequest.Url.Fragment;
        GD.Print($"{uriBuilder}, {code}, {state}, {accessToken}, {httpRequest.Url.Fragment}");

        var httpResponse = httpContext.Response;
        var responseString = "<html><body><b>DONE!</b><br>(You can close this tab/window now)</body></html>";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

        // send the output to the client browser
        httpResponse.ContentLength64 = buffer.Length;
        System.IO.Stream output = httpResponse.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();

        if (string.IsNullOrEmpty(httpRequest.Url.Fragment)) return;

        httpListener.Close();
        CallDeferred("StartWebSocket");
    }

    private void IncomingHttpRequest(IAsyncResult result)
    {
        // get back the reference to our http listener
        var httpListener = (HttpListener)result.AsyncState;

        // fetch the context object
        var httpContext = httpListener.EndGetContext(result);

        // the context object has the request object for us, that holds details about the incoming request
        var httpRequest = httpContext.Request;

        var uriBuilder = new UriBuilder($"{httpRequest.Url}");
        var code = httpRequest.QueryString.Get("code");
        var state = httpRequest.QueryString.Get("state");

        CallDeferred("SendPostTwitch", code);

        var httpResponse = httpContext.Response;
        var responseString = "<html><body><b>DONE!</b><br>(You can close this tab/window now)</body></html>";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

        // send the output to the client browser
        httpResponse.ContentLength64 = buffer.Length;
        System.IO.Stream output = httpResponse.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();

        httpListener.Close();
    }

    public void SendPostTwitch(string code)
    {
        _request.RequestCompleted += OnRequestCompleted;

        var json = Json.Stringify(new Dictionary<string, string>()
        {
            ["client_id"] = TwitchVariables.ClientId,
            ["client_secret"] = TwitchVariables.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = TwitchRedirectURL,
        });

        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString.Set("client_id", TwitchVariables.ClientId);
        queryString.Set("client_secret", TwitchVariables.ClientSecret);
        queryString.Set("code", code);
        queryString.Set("grant_type", "authorization_code");
        queryString.Set("redirect_uri", TwitchRedirectURL);
        var header = new string[]{
            "Content-Type: application/json",
            $"x-www-form-urlencoded: {queryString}"
        };
        _request.Request(TwitchTokenURL, header, HttpClient.Method.Post, json);
    }

    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        _request.RequestCompleted -= OnRequestCompleted;
        var str = Encoding.UTF8.GetString(body);

        if (responseCode != 200)
        {
            GD.PrintErr($"[{responseCode}] {str}");
            return;
        }

        Godot.Collections.Dictionary json = Json.ParseString(str).AsGodotDictionary();
        if (!json.TryGetValue("access_token", out Variant token))
        {
            GD.PrintErr($"access_token does not exist in {str}");
            return;
        }
        _variables.AccessToken = json["access_token"].ToString();
        StartWebSocket();
    }

    private void StartWebSocket()
    {
        _webSocket = new();
        _webSocket.SetMeta("Authorization", $"Bearer {_variables.AccessToken}");
        _webSocket.ConnectToUrl(TwitchURL);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        UpdateWebSocket();
    }

    private void UpdateWebSocket()
    {
        if (_webSocket == null) return;

        _webSocket.Poll();
        var state = _webSocket.GetReadyState();

        if (state == WebSocketPeer.State.Open)
        {
            while (_webSocket.GetAvailablePacketCount() > 0)
            {
                var msg = System.Text.Encoding.Default.GetString(_webSocket.GetPacket()).Trim();
                GD.Print($"Packet: {msg}");
                ParseTwitchMessage(msg);
            }
        }

        if (_state == state) return;
        switch (state)
        {
            case WebSocketPeer.State.Open:
                if (!_isOpened)
                {
                    _isOpened = true;
                    GD.Print($"Connected to {TwitchURL} !");
                    //_webSocket.SendText("CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands");
                    _webSocket.SendText($"PASS oauth:{_variables.AccessToken}");
                    _webSocket.SendText($"NICK <{TwitchVariables.Nick}>");
                    _webSocket.SendText($"JOIN #darkrewar");
                }
                break;
            case WebSocketPeer.State.Closed:
                var code = _webSocket.GetCloseCode();
                var reason = _webSocket.GetCloseReason();
                GD.Print($"Connection to {TwitchURL} closed: [{code}] {reason}");
                break;
            case WebSocketPeer.State.Closing:
                GD.Print($"Closing connection from {TwitchURL} ...");
                break;
            case WebSocketPeer.State.Connecting:
                GD.Print($"Trying to connect to {TwitchURL} ...");
                break;
        }

        _state = state;
    }

    private void ParseTwitchMessage(string msg)
    {
        Regex pingRegex = new(@"^PING");
        if (pingRegex.IsMatch(msg))
        {
            _webSocket.SendText("PONG :tmi.twitch.tv");
            return;
        }

        Regex regex = new(@"^:(\w+)!.*\.tmi\.twitch\.tv\s(\w+)\s#darkrewar\s:(.*)$");
        if (!regex.IsMatch(msg)) return;
        var matches = regex.Matches(msg);
        string username = matches[0].Groups[1].Value;
        string command = matches[0].Groups[2].Value;
        string message = matches[0].Groups[3].Value;
        EmitSignal(nameof(OnMessageReceived), username, message);
    }

    private string GetCodeURI()
    {
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString.Set("response_type", "code");
        queryString.Set("client_id", TwitchVariables.ClientId);
        queryString.Set("redirect_uri", TwitchRedirectURL);
        //queryString.Set("scope", "chat:read+user:read:subscriptions");
        queryString.Set("scope", "chat:read");
        queryString.Set("state", _stateParameter);

        return $"https://id.twitch.tv/oauth2/authorize?{queryString}";
    }

    private string GetTokenURI()
    {
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString.Set("response_type", "token");
        queryString.Set("client_id", TwitchVariables.ClientId);
        queryString.Set("redirect_uri", TwitchRedirectURL);
        //queryString.Set("scope", "channel:manage:polls+channel:read:polls+channel:read:message");
        //queryString.Set("scope", "chat:read+user:read:subscriptions");
        queryString.Set("scope", "chat:read");
        queryString.Set("state", _stateParameter);

        UriBuilder builder = new("https", "id.twitch.tv/oauth2/authorize");
        builder.Query = queryString.ToString();

        return $"https://id.twitch.tv/oauth2/authorize?{queryString}";
    }
}

[Serializable]
public struct TwitchAPITokenResponse
{
    public string access_token;
    public long expires_in;
    public string refresh_token;
    public string[] scope;
    public string token_type;
}
