using shared;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
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

    private void Start()
    {
        connectToServer();

        //register for the important events
        _avatarAreaManager = FindFirstObjectByType<AvatarAreaManager>();
        _avatarAreaManager.OnAvatarAreaClicked += onAvatarAreaClicked;

        _panelWrapper = FindFirstObjectByType<PanelWrapper>();
        _panelWrapper.OnChatTextEntered += onChatTextEntered;
    }

    private void connectToServer()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(_server, _port);
            Debug.Log("Connected to server.");

            Packet joinPacket = new Packet();
            joinPacket.Write(new JoinRequest());
            StreamUtil.Write(_client.GetStream(), joinPacket.GetBytes());
        }
        catch (Exception e)
        {
            Debug.Log("Could not connect to server:");
            Debug.Log(e.Message);
        }
    }

    private void onAvatarAreaClicked(Vector3 pClickPosition)
    {
        Debug.Log("ChatLobbyClient: you clicked on " + pClickPosition);
        //TODO pass data to the server so that the server can send a position update to all clients (if the position is valid!!)
    }

    private void onChatTextEntered(string pText)
    {
        _panelWrapper.ClearInput();
        sendString(pText);
    }

    private void sendString(string pOutString)
    {
        try
        {
            //we are still communicating with strings at this point, this has to be replaced with either packet or object communication
            Debug.Log("Sending:" + pOutString);
            byte[] outBytes = Encoding.UTF8.GetBytes(pOutString);
            StreamUtil.Write(_client.GetStream(), outBytes);
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    // RECEIVING CODE

    private void Update()
    {
        if (_client == null && _client.Available == 0) return;

        try
        {
            //we are still communicating with strings at this point, this has to be replaced with either packet or object communication

            byte[] inBytes = StreamUtil.Read(_client.GetStream());
            Packet packet = new Packet(inBytes);
            object obj = packet.ReadObject();

            if (obj is AvatarModel avatar && !_avatarAreaManager.HasAvatarView(avatar.Id))
            {
                AvatarView avatarView = _avatarAreaManager.AddAvatarView(avatar.Id);
                avatarView.SetSkin(avatar.Skin);
                avatarView.Move(avatar.GetPosition());
            }   
            
            // old string comunication
            /*string inString = Encoding.UTF8.GetString(inBytes);
            Debug.Log("Received:" + inString);
            showMessage(inString);*/
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    private void showMessage(string pText)
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
    }

}
