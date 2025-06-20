using shared;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

/**
 * The main ChatLobbyClient where you will have to do most of your work.
 * 
 * @author J.C. Wichman
 */
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
        _avatarAreaManager.OnAvatarAreaClicked += onAvatarAreaClicked;

        _panelWrapper = FindFirstObjectByType<PanelWrapper>();
        _panelWrapper.OnChatTextEntered += onChatTextEntered;

        connectToServer();
    }

    private void Update()
    {
        if (_client == null || _client.Available == 0) return;

        byte[] inBytes;
        try
        {
            inBytes = StreamUtil.Read(_client.GetStream());
            if (inBytes.Length == 0) return;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Stream read failed: {e.Message}");
            return;
        }

        handlePacket(inBytes);
    }


    private void connectToServer()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(_server, _port);
            Debug.Log("Connected to server.");

            // ask to join (no id/skin sent)
            Packet joinReq = new Packet();
            joinReq.Write("joinReq");
            StreamUtil.Write(_client.GetStream(), joinReq.GetBytes());
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not connect: {e.Message}");
        }
    }


    private void onAvatarAreaClicked(Vector3 pClickPosition)
    {
        Packet movePacket = new Packet();
        movePacket.Write("moveReq");
        movePacket.Write((int)(pClickPosition.x * 1000));
        movePacket.Write((int)(pClickPosition.z * 1000));
        StreamUtil.Write(_client.GetStream(), movePacket.GetBytes());
    }

    private void onChatTextEntered(string pText)
    {
        _panelWrapper.ClearInput();

        string trimText = pText.Trim();

        const string WHISPER_CMD = "/whisper";

        if (trimText.StartsWith(WHISPER_CMD, StringComparison.OrdinalIgnoreCase))
        {
            string whisperMsg = trimText.Substring(WHISPER_CMD.Length);
            if (whisperMsg.Length == 0) return;

            Packet whisper = new Packet(); 
            whisper.Write("whisperReq");
            whisper.Write(whisperMsg);
            StreamUtil.Write(_client.GetStream(), whisper.GetBytes());
            return;
        }
       
        Packet chatPacket = new Packet();
        chatPacket.Write("textReq");
        chatPacket.Write(pText);
        StreamUtil.Write(_client.GetStream(), chatPacket.GetBytes());
    }


    private void handlePacket(byte[] data)
    {
        Packet packet = new Packet(data);
        string type = packet.ReadString();

        switch (type)
        {
            case "world": handleWorld(packet); break;
            case "pJoin": handleJoin(packet); break;
            case "pMove": handleMove(packet); break;
            case "pText": handleText(packet); break;
            case "pWhisper": handleWhisper(packet); break;
            case "pLeave": handleLeave(packet); break;
        }
    }

    private void handleWorld(Packet p)
    {
        int n = p.ReadInt();
        for (int i = 0; i < n; i++)
        {
            int id = p.ReadInt();
            int skin = p.ReadInt();
            int x = p.ReadInt();
            int z = p.ReadInt();

            if (!_avatarAreaManager.HasAvatarView(id)) //if we dont know this avatar 
            {
                AvatarView av = _avatarAreaManager.AddAvatarView(id);
                av.SetSkin(skin);
                av.Move(new Vector3(x / 1000f, 0, z / 1000f));
            }
            if (_myAvatarId == -1)   // first packet that contains 'me' is always me      
                _myAvatarId = id;                  

        }
    }

    private void handleJoin(Packet p)
    {
        int id = p.ReadInt();
        int skin = p.ReadInt();
        int x = p.ReadInt();
        int z = p.ReadInt();

        if (!_avatarAreaManager.HasAvatarView(id))
        {
            AvatarView av = _avatarAreaManager.AddAvatarView(id);
            av.SetSkin(skin);
            av.Move(new Vector3(x / 1000f, 0, z / 1000f));
        }
    }

    private void handleMove(Packet p)
    {
        int id = p.ReadInt();
        int x = p.ReadInt();
        int z = p.ReadInt();

        if (_avatarAreaManager.HasAvatarView(id))
            _avatarAreaManager.GetAvatarView(id)
                .Move(new Vector3(x / 1000f, 0, z / 1000f));
    }

    private void handleText(Packet p)
    {
        int id = p.ReadInt();
        string msg = p.ReadString();

        if (_avatarAreaManager.HasAvatarView(id))
            _avatarAreaManager.GetAvatarView(id).Say(msg);
    }

    private void handleWhisper(Packet p)
    {
        int id = p.ReadInt();
        string msg = p.ReadString();

        if (_avatarAreaManager.HasAvatarView(id))
            _avatarAreaManager.GetAvatarView(id).Say($"(whisper) {msg}");
    }

    private void handleLeave(Packet p)
    {
        int id = p.ReadInt();
        if (_avatarAreaManager.HasAvatarView(id))
            _avatarAreaManager.RemoveAvatarView(id);
    }
}
