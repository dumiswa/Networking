using shared;
using shared.src.protocol;
using System;
using System.Net.Sockets;
using UnityEngine;

public class ChatLobbyClient : MonoBehaviour
{
    private AvatarAreaManager _avatarAreaManager;
    private PanelWrapper _panelWrapper;

    [SerializeField] private string _server = "localhost";
    [SerializeField] private int _port = 55555;

    private TcpClient _client;
    private int _myAvatarId = -1;

    
    private void Start()
    {
        _avatarAreaManager = FindFirstObjectByType<AvatarAreaManager>();
        _avatarAreaManager.OnAvatarAreaClicked += OnAvatarAreaClicked;

        _panelWrapper = FindFirstObjectByType<PanelWrapper>();
        _panelWrapper.OnChatTextEntered += OnChatTextEntered;

        ConnectToServer();
    }

    private void Update()
    {
        if (_client == null || _client.Available == 0) return;

        byte[] bytes;
        try { bytes = StreamUtil.Read(_client.GetStream()); }
        catch (Exception e) { Debug.LogWarning(e.Message); return; }

        if (bytes != null && bytes.Length > 0)
            HandleIncoming(bytes);
    }

    // outgoing
    private void ConnectToServer()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(_server, _port);
            Debug.Log("Connected to server.");
            // Server sends WorldCommand automatically
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not connect: {e.Message}");
        }
    }

    private void OnAvatarAreaClicked(Vector3 pos)
    {
        var cmd = new MoveCommand(0, (int)(pos.x * 1000), (int)(pos.z * 1000));
        StreamUtil.Write(_client.GetStream(), new Packet(cmd).GetBytes());
    }

    private void OnChatTextEntered(string raw)
    {
        _panelWrapper.ClearInput();
        string msg = raw.Trim();

        const string WHISPER = "/whisper";
        if (msg.StartsWith(WHISPER, StringComparison.OrdinalIgnoreCase))
        {
            string body = msg.Substring(WHISPER.Length).Trim();
            if (body.Length > 0)
                StreamUtil.Write(_client.GetStream(),
                    new Packet(new WhisperCommand(body)).GetBytes());
            return;
        }

        StreamUtil.Write(_client.GetStream(),
            new Packet(new TextCommand(_myAvatarId, msg)).GetBytes());
    }

    // incoming
    private void HandleIncoming(byte[] data)
    {
        ISerializable obj;
        try { obj = new Packet(data).ReadObject(); }
        catch (Exception e) { Debug.LogWarning(e.Message); return; }

        switch (obj)
        {
            case WorldCommand w: HandleWorld(w); break;
            case JoinCommand j: HandleJoin(j); break;
            case MoveCommand m: HandleMove(m); break;
            case TextCommand t: HandleText(t); break;
            case LeaveCommand l: HandleLeave(l); break;
        }
    }

    // command handlers 
    private void HandleWorld(WorldCommand w)
    {
        foreach (var a in w.Avatars)
        {
            if (!_avatarAreaManager.HasAvatarView(a.Id))
            {
                var view = _avatarAreaManager.AddAvatarView(a.Id);
                view.SetSkin(a.Skin);
                view.Move(new Vector3(a.X / 1000f, 0, a.Z / 1000f));
            }
            if (_myAvatarId == -1) _myAvatarId = a.Id;   // first avatar = me
        }
    }

    private void HandleJoin(JoinCommand j)
    {
        if (_avatarAreaManager.HasAvatarView(j.Id)) return;

        var view = _avatarAreaManager.AddAvatarView(j.Id);
        view.SetSkin(j.Skin);
        view.Move(new Vector3(j.X / 1000f, 0, j.Z / 1000f));
    }

    private void HandleMove(MoveCommand m)
    {
        if (_avatarAreaManager.HasAvatarView(m.Id))
            _avatarAreaManager.GetAvatarView(m.Id)
                              .Move(new Vector3(m.X / 1000f, 0, m.Z / 1000f));
    }

    private void HandleText(TextCommand t)
    {
        if (_avatarAreaManager.HasAvatarView(t.Id))
            _avatarAreaManager.GetAvatarView(t.Id).Say(t.Message);
    }

    private void HandleLeave(LeaveCommand l)
    {
        if (_avatarAreaManager.HasAvatarView(l.Id))
            _avatarAreaManager.RemoveAvatarView(l.Id);
    }
}
