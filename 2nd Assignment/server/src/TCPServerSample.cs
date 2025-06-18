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

        public Client(TcpClient client)
        {
            ServerClient = client;
            Stream = client.GetStream();
        }
    }


    static int guestCounter = 1;
    static readonly Dictionary<TcpClient, Client> clients = new Dictionary<TcpClient, Client>();
	//static readonly List<Client> clients = new List<Client>();


    public static void Main ()
	{
		Console.WriteLine("Server started on port 55555");

		var listener = new TcpListener (IPAddress.Any, 55555);
		listener.Start ();

		//List<TcpClient> clients = new List<TcpClient>();
		//List<Client> clients = new List<Client>();

		while (true)
		{
			/*//First big change with respect to example 001
			//We no longer block waiting for a client to connect, but we only block if we know
			//a client is actually waiting (in other words, we will not block)
			//In order to serve multiple clients, we add that client to a list
			

			//Second big change, instead of blocking on one client, 
			//we now process all clients IF they have data available
		   *//* foreach (TcpClient client in clients)
			{
				if (client.Available == 0) continue;
				NetworkStream stream = client.GetStream();
				StreamUtil.Write(stream, StreamUtil.Read(stream));
			}*//*

			
			//Although technically not required, now that we are no longer blocking, 
			//it is good to cut your CPU some slack*/

            AcceptNewClient(listener);
            ProcessClients();
            CleanupFaultyClients();
			Thread.Sleep(100);
		}
	}



	static void AcceptNewClient(TcpListener listener)
	{
        TcpClient tcpClient = null;
        while (listener.Pending())
        {
            try
            {
                tcpClient = listener.AcceptTcpClient();
                var newClient = new Client(tcpClient);

                newClient.Name = $"Guest{guestCounter++}";
                Console.WriteLine($"Accepted {newClient.Name}");

                clients.Add(tcpClient, newClient);
                StreamUtil.Write(newClient.Stream, System.Text.Encoding.UTF8.GetBytes($"__yourclientnameis:{newClient.Name}"));

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



	static void ProcessClients()
	{
        foreach (var newClient in clients.ToArray())
        {
            var client = newClient.Value;
            try
            {
                if (client.ServerClient.Available == 0) continue;

                string message = System.Text.Encoding.UTF8.GetString(StreamUtil.Read(client.Stream));

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

                if (message.StartsWith("/help"))
                {
                    HandleHelp(client);
                    continue;
                }

                if (message.StartsWith("/list"))
                {
                    HandleList(client);
                    continue;
                }

                foreach (var other in clients.Values)
                {
                    //if (other != client)
                    StreamUtil.Write(other.Stream, System.Text.Encoding.UTF8.GetBytes($"[{DateTime.Now:HH:mm}] {client.Name}: {message}"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{client.Name} disconnected or had problems logging in {ex.Message}");
                client.Stream.Close();
                client.ServerClient.Close();
                clients.Remove(client.ServerClient);
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
                if (!client.ServerClient.Connected || (client.ServerClient.Client.Poll(0, SelectMode.SelectRead) && client.ServerClient.Available == 0))
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

        bool isNameTaken = clients.Values.Any(cl => cl != sender && cl.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));

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
        try { client.Stream?.Close(); } catch { }
        try { client.ServerClient.Close(); } catch { }
        clients.Remove(client.ServerClient);
    }

    static void HandleWhisper(Client sender, string message)
	{
        string[] parts = message.Split(' ', 3);
        if (parts.Length == 3)
        {
            string targetName = parts[1].ToLower();
            string whisperMessage = parts[2];

            var target = clients.Values.FirstOrDefault(c => c.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));

            if (target != null)
            {
                StreamUtil.Write(target.Stream, System.Text.Encoding.UTF8.GetBytes($"[{DateTime.Now:HH:mm}] (whisper from {sender.Name}): {whisperMessage}"));
                StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"[{DateTime.Now:HH:mm}] (you whispered to {target.Name}): {whisperMessage}"));
            }
            else StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"\nTarget {targetName} does not exist"));
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
        }
    }
	static void HandleList(Client client)
	{
        if (client != null)
        {
            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"\nList of Online Clients:")); 
            foreach (var c in clients.ToArray())
            {
                StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"{c.Value.Name}"));
            }
        }
    }
}