using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.Reflection;

class TCPServerSample
{
    /**
	 * This class implements a simple concurrent TCP Echo server.
	 * Read carefully through the comments below.
	 */

    class Client
    {
        public TcpClient ServerClient;
        public NetworkStream Stream;
        public string Name = "";
        public string Room = "general";
        public Client(TcpClient client)
        {
            ServerClient = client;
            Stream = client.GetStream();
        }
    }


    static int guestCounter = 1;
    static readonly Dictionary<TcpClient, Client> clients = new Dictionary<TcpClient, Client>();
    //static readonly List<Client> clients = new List<Client>();

    static readonly Dictionary<string, HashSet<Client>> rooms =
        new Dictionary<string, HashSet<Client>>(StringComparer.OrdinalIgnoreCase)
        { { "general", new HashSet<Client>() } };

    public static void Main()
    {
        Console.WriteLine("Server started on port 55555");

        var listener = new TcpListener(IPAddress.Any, 55555);
        listener.Start();


        while (true)
        {
            AcceptNewClient(listener);
            ProcessClients();
            CleanupFaultyClients();
            Thread.Sleep(100);
        }
    }


    static void AcceptNewClient(TcpListener listener)
    {
        while (listener.Pending()) // accept only if client is ready (avoids blocking)
        {
            TcpClient tcpClient = null;
            try
            {
                tcpClient = listener.AcceptTcpClient(); //accept client and return a listener
                var newClient = new Client(tcpClient);

                newClient.Name = $"guest{guestCounter++}";
               
                clients.Add(tcpClient, newClient); //adds to dictionary
                rooms["general"].Add(newClient);

                StreamUtil.Write(newClient.Stream, System.Text.Encoding.UTF8.GetBytes($"__yourclientnameis:{newClient.Name}"));
                BroadcastToRoom(newClient.Room, $"~{newClient.Name} joined the server and entered {newClient.Room}");
                Console.WriteLine($"Accepted {newClient.Name}");

                foreach (var client in clients.Values)
                {
                    StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"\n {newClient.Name} joined the server"));
                }
            }
            catch (Exception ex)
            {
                Console.Write($"Error while accpeting cleint: {ex.Message}");

                if (tcpClient != null && clients.TryGetValue(tcpClient, out var faulty))
                    RemoveClient(faulty);
            }
        }
    }


    //process input from clients
    static void ProcessClients()
    {
        foreach (var newClient in clients.ToArray()) //safety check if client is removed 
        {
            var client = newClient.Value;
            try
            {
                if (client.ServerClient.Available == 0) continue;

                string message = System.Text.Encoding.UTF8.GetString(StreamUtil.Read(client.Stream)); //read from send buffer

                if (message.StartsWith("/setname"))
                {
                    string newName = message.Substring(8).Trim().ToLower();
                    HandleSetName(client, newName);
                    continue;
                }

                if (message.StartsWith("/whisper"))
                {
                    HandleWhisper(client, message);
                    continue;
                }

                if (message.Equals("/help"))
                {
                    HandleHelp(client);
                    continue;
                }

                if (message.Equals("/list"))
                {
                    HandleList(client);
                    continue;
                }

                if (message.StartsWith("/join"))
                {
                    HandleRoomJoin(client, message.Substring(6).Trim());
                    continue;
                }

                if (message.StartsWith("/listrooms", StringComparison.OrdinalIgnoreCase))
                {
                    HandleListRooms(client);
                    continue;
                }

                if (message.StartsWith("/listroom", StringComparison.OrdinalIgnoreCase))
                {
                    HandleListRoom(client);
                    continue;
                }
                
                BroadcastToRoom(client.Room, $"[{DateTime.Now:HH:mm}] {client.Name}: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client had error: {ex}");
                RemoveClient(client);
            }

        }
    }


    static void CleanupFaultyClients()
    {
        foreach (var faultyClient in clients.Keys.ToArray())
        {
            var client = clients[faultyClient];

            try
            {
                if (!client.ServerClient.Connected ||   //check if socket is still connected
                    (client.ServerClient.Client.Poll(0, SelectMode.SelectRead) && client.ServerClient.Available == 0))
                {
                    Console.WriteLine($"{client.Name} disconnected");
                    RemoveClient(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cleanup error on {client.Name}: {ex.Message}");
                RemoveClient(client);
            }
        }
    }


    static void HandleSetName(Client sender, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"\n~Name cannot be empty, please choose different name.~"));
            return;
        }

        bool isNameTaken = clients.Values.Any(c => c != sender && c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));

        if (isNameTaken)
            StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"\n~User '{newName}' is already taken~"));
        else
        {
            string oldName = sender.Name;
            sender.Name = newName;
            Console.WriteLine($"{oldName} changed name to {newName}");

            foreach (var client in clients.Values)
            {
                if (client.Name.Equals(sender.Name))
                {
                    StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"\n~You changed your name to {newName}~"));
                }
                else StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"\n~{oldName} changed their name to {newName}~"));
            }


        }
    }

    static void RemoveClient(Client client)
    {
        Console.WriteLine($"Removed client {client.Name}");
        try { client.Stream?.Close(); } catch { } // closes networkstream
        try { client.ServerClient.Close(); } catch { } // closes socket
        clients.Remove(client.ServerClient); //removes from dictionary
    }

    static void HandleWhisper(Client sender, string message)
    {
        string[] parts = message.Split(' ', 3);
        if (parts.Length == 3)
        {
            string targetName = parts[1].ToLower();
            string whisperMessage = parts[2];

            // look for target by name
            var target = clients.Values.FirstOrDefault(c => c.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));

            if (target != null && target != sender)
            {
                StreamUtil.Write(target.Stream, System.Text.Encoding.UTF8.GetBytes($"[{DateTime.Now:HH:mm}] (whisper from {sender.Name}): {whisperMessage}"));
                StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"[{DateTime.Now:HH:mm}] (you whispered to {target.Name}): {whisperMessage}"));
            }
            else if (target == sender) StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"~You can not whisper to yourself, please whisper to a valid client~"));
            else StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"\n~Target {targetName} does not exist~"));
        }
        else if (parts.Length < 3)
        {
            StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"~please use /whisper <target> <msg>~"));
            return;
        }
    }
    static void HandleHelp(Client client)
    {
        if (client != null)
        {
            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"\n~Commands: ~"));
            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~Use '/setname' + Desired Name to change your name~"));
            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~Use '/whisper' + target + message to whisper to a client~"));
            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~Use '/help' to see all commands~"));
            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~Use '/list' to see all connected clients~"));
            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~Use '/join' <room> to create / enter a room ~"));
            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~Use '/listroom' or '/listrooms' to see all rooms~"));
        }
    }
    static void HandleList(Client client)
    {
        if (client != null)
        {
            string names = string.Join(", ", clients.Values.Select(c => c.Name));
             StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"Online: {names}"));
        }
    }
    /*static void HandleList(Client client)
    {
        if (client != null)
        {
            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"\nList of Online Clients:"));
            foreach (var c in clients.ToArray())
            {
                StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"{c.Value.Name}"));
            }
        }
    }*/


    static void HandleRoomJoin(Client sender, string roomName)
    {
        if (string.IsNullOrEmpty(roomName)) return;

        rooms[sender.Room].Remove(sender);

        if (!rooms.ContainsKey(roomName)) 
            rooms[roomName] = new HashSet<Client>();

        sender.Room = roomName;
        rooms[roomName].Add(sender);
        
        StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"~You joined {roomName}"));
        BroadcastToRoom(roomName, $"~{sender.Name} joined the room~");
    }

    static void HandleListRooms(Client client)
    {
        string list = string.Join(", ", rooms.Keys);
        StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"Active rooms: {list}"));
    }
    static void HandleListRoom(Client client)
    {
        string users = string.Join(", ", rooms[client.Room].Select(c => c.Name));
        StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"Users in {client.Room}: {users}"));
    }

    static void BroadcastToRoom(string room, string message)
    {
        foreach (var c in rooms[room])
        {
            StreamUtil.Write(c.Stream, System.Text.Encoding.UTF8.GetBytes(message));
        }
    }
}