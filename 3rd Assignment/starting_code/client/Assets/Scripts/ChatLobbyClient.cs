using shared;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEditor.Sprites;
using UnityEngine;

/**
 * The main ChatLobbyClient where you will have to do most of your work.
 * 
 * @author J.C. Wichman
 */
public class ChatLobbyClient : MonoBehaviour
{
    //reference to the helper class that hides all the avatar management behind a blackbox
    private AvatarAreaManager _avatarAreaManager;
    //reference to the helper class that wraps the chat interface
    private PanelWrapper _panelWrapper;

    [SerializeField] private string _server = "localhost";
    [SerializeField] private int _port = 55555;

    private TcpClient _client;
    private int _myAvatarId = -1;
    private int _mySkin = -1;
    private readonly HashSet<int> _knownIds = new HashSet<int>();
    private bool _sentJoin = false;
    private Vector3 _startPos;

    private void Start()
    {      
        _myAvatarId = UnityEngine.Random.Range(1000, 9999);
        _mySkin = UnityEngine.Random.Range(0, 4);

        _knownIds.Add(_myAvatarId);

        _startPos = new Vector3(
            UnityEngine.Random.Range(-3f, 3f),
            0, 
            UnityEngine.Random.Range(-3f, 3f)
        );

       
        //register for the important events
        _avatarAreaManager = FindFirstObjectByType<AvatarAreaManager>();
        _avatarAreaManager.OnAvatarAreaClicked += onAvatarAreaClicked;

        _panelWrapper = FindFirstObjectByType<PanelWrapper>();
        _panelWrapper.OnChatTextEntered += onChatTextEntered;

        Vector3 startPos = new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, UnityEngine.Random.Range(-3f, 3f));
        AvatarView avatar = _avatarAreaManager.AddAvatarView(_myAvatarId);
        avatar.SetSkin(_mySkin);
        avatar.Move(startPos);

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

        /*try
        {

            if (type == "pJoin")
            {
                int id = packet.ReadInt();
                int skin = packet.ReadInt();
                int x = packet.ReadInt();
                int z = packet.ReadInt();

               
            }
            else if (type == "pMove")
            {
                int id = packet.ReadInt();
                int x = packet.ReadInt();
                int z = packet.ReadInt();
      
            }
            else if (type == "pText")
            {
                int id = packet.ReadInt();
                string message = packet.ReadString();

                if (_avatarAreaManager.HasAvatarView(id))
                    _avatarAreaManager.GetAvatarView(id).Say(message);
            }

            // old string comunication
            string inString = Encoding.UTF8.GetString(inBytes);
            Debug.Log("Received:" + inString);
            showMessage(inString);
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.LogWarning($"Packet error {e.Message}");
            *//*_client.Close();
            connectToServer();*//*
        }*/

        handlePacket(inBytes);
    }



    private void connectToServer()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(_server, _port);
            Debug.Log("Connected to server.");

            /*Packet joinPacket = new Packet();
            joinPacket.Write("pJoin");
            StreamUtil.Write(_client.GetStream(), joinPacket.GetBytes());*/

            sendJoinPackage();
        }
        catch (Exception e)
        {
            //Debug.Log("Could not connect to server:");
            Debug.Log(e.Message);
        }
    }

    private void sendJoinPackage()
    {
        Packet joinPacket = new Packet();
        joinPacket.Write("pJoin");
        joinPacket.Write(_myAvatarId);
        joinPacket.Write(_mySkin);
        joinPacket.Write((int)(_startPos.x * 1000));
        joinPacket.Write((int)(_startPos.z * 1000));
        StreamUtil.Write(_client.GetStream(), joinPacket.GetBytes());

        _sentJoin = true;
    }

    //TODO pass data to the server so that the server can send a position update to all clients (if the position is valid!!)
    private void onAvatarAreaClicked(Vector3 pClickPosition)
    {
        
        Packet movePacket = new Packet();
        movePacket.Write("pMove");
        movePacket.Write(_myAvatarId);
        movePacket.Write((int)(pClickPosition.x * 1000));
        movePacket.Write((int)(pClickPosition.z * 1000));
        StreamUtil.Write(_client.GetStream(), movePacket.GetBytes());

        //Debug.Log("ChatLobbyClient: you clicked on " + pClickPosition);      
    }

    private void onChatTextEntered(string pText)
    {
        _panelWrapper.ClearInput();

        Packet chatPacket = new Packet();
        chatPacket.Write("pText");
        chatPacket.Write(_myAvatarId);
        chatPacket.Write(pText);
        StreamUtil.Write(_client.GetStream(), chatPacket.GetBytes());

        //Debug.Log($"Client with the Id: {_myAvatarId} messaged {pText}");
    }


    private void handlePacket(byte[] data)
    {

        Packet packet = new Packet(data);
        string type = packet.ReadString();

        //Debug.Log($"Echo received from server: {type}");

        switch (type)
        {
            case "pJoin":
                handleJoin(packet);               
                break;
            case "pMove":
                handleMove(packet);
                break;
            case "pText":
                handleText(packet);
                break;
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
            AvatarView avatar = _avatarAreaManager.AddAvatarView(id);
            avatar.SetSkin(skin);
            avatar.Move(new Vector3(x / 1000f, 0, z / 1000f));
        }

        if (id != _myAvatarId) sendJoinPackage();
        Debug.Log($"Client {_client} is joining");
    }

    private void handleMove(Packet p)
    {
        int id = p.ReadInt();
        int x = p.ReadInt();
        int z = p.ReadInt();


        if (_avatarAreaManager.HasAvatarView(id))
            _avatarAreaManager.GetAvatarView(id).Move(new Vector3(x / 1000f, 0, z / 1000f));
        Debug.Log($"Client {_client} is moving");
    }

    private void handleText(Packet p)
    {
        int id = p.ReadInt();
        string message = p.ReadString();

        if (_avatarAreaManager.HasAvatarView(id))
            _avatarAreaManager.GetAvatarView(id).Say(message);

        Debug.Log($"Client {_client} is saying: {message}");
    }

/*    private void showMessage(string pText)
    {
        //This is a stub for what should actually happen
        //What should actually happen is use an ID that you got from the server, to get the correct avatar
        //and show the text message through that
        List<int> allAvatarIds = _avatarAreaManager.GetAllAvatarIds();
        
        if (allAvatarIds.Count == 0)
        {
            Debug.Log("No avatars available to show text through:" + pText);
            return;
        }

        int randomAvatarId = allAvatarIds[UnityEngine.Random.Range(0, allAvatarIds.Count)];
        AvatarView avatarView = _avatarAreaManager.GetAvatarView(randomAvatarId);
        avatarView.Say(pText);
    }*/

}

