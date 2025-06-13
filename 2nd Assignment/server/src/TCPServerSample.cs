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

	static int guestCounter = 1;

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

    public static void Main (string[] args)
	{
		Console.WriteLine("Server started on port 55555");

		TcpListener listener = new TcpListener (IPAddress.Any, 55555);
		listener.Start ();

		//List<TcpClient> clients = new List<TcpClient>();
		List<Client> clients = new List<Client>();

		while (true)
		{
			//First big change with respect to example 001
			//We no longer block waiting for a client to connect, but we only block if we know
			//a client is actually waiting (in other words, we will not block)
			//In order to serve multiple clients, we add that client to a list
			while (listener.Pending())
			{
				var tcpClient = listener.AcceptTcpClient();
				var newClient = new Client(tcpClient);

				newClient.Name = $"Guest{guestCounter++}";
				Console.WriteLine($"Accepted {newClient.Name}");

				clients.Add(newClient);
				StreamUtil.Write(newClient.Stream, System.Text.Encoding.UTF8.GetBytes($"__yourclientnameis:{newClient.Name}"));
			}

			//Second big change, instead of blocking on one client, 
			//we now process all clients IF they have data available
			/*foreach (TcpClient client in clients)
			{
				if (client.Available == 0) continue;
				NetworkStream stream = client.GetStream();
				StreamUtil.Write(stream, StreamUtil.Read(stream));
			}*/

			foreach (var client in clients.ToArray())
			{
				try
				{
					if (client.ServerClient.Available == 0) continue;

                    string message = System.Text.Encoding.UTF8.GetString(StreamUtil.Read(client.Stream));

                    if (message.StartsWith("/setname"))
					{
						string newName = message.Substring(8).Trim().ToLower();

						if (string.IsNullOrWhiteSpace(newName))
						{
                            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~Name cannot be empty, please choose different name.~"));
							continue;
                        }

						bool isNameTaken = clients.Exists(c => !ReferenceEquals(c, client) && c.Name.Equals(newName, StringComparison.Ordinal));

						if (isNameTaken)                        
                            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~User '{newName}' is already taken~"));                        
						else
						{
							string oldName = client.Name;
							client.Name = newName;
							Console.WriteLine($"{oldName} changed name to {newName}");

                            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~You changed your name to {newName}~"));
                        }	
						continue;
					}

					if (message.StartsWith("/whisper"))
					{
						string[] parts = message.Split(' ', 3);
						if (parts.Length == 3)
						{
							string targetName = parts[1];
							string whisperMessage = parts[2];
							var target = clients.Find(c => c.Name == targetName);
							var sender = client;

							if (target != null)
							{
								StreamUtil.Write(target.Stream, System.Text.Encoding.UTF8.GetBytes($"[{DateTime.Now:HH:mm}] (whisper from {sender.Name}): {whisperMessage}"));
								StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"[{DateTime.Now:HH:mm}] (you whispered to {target.Name}): {whisperMessage}"));
							}
							else StreamUtil.Write(sender.Stream, System.Text.Encoding.UTF8.GetBytes($"Target {targetName} does not exist"));
                        }

					continue;
					}

					if (message.StartsWith("/help"))
					{
						if (client != null) 
						{
                            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~Use '/setname' + Desired Name to change your name~"));
                            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~Use '/whisper' + target + message to whisper to a client~"));
                            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~Use '/help' to see all commands~"));
                            StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"~Use '/list' to see all connected clients~"));
                        }
						continue;
					}

                    if (message.StartsWith("/list"))
                    {
                        if (client != null)
                        {
                            foreach (var c in clients.ToArray())
                            {
                                StreamUtil.Write(client.Stream, System.Text.Encoding.UTF8.GetBytes($"{c.Name} \n"));
                            }
                        }
						continue;
                    }

                    foreach (var other in clients)
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
					clients.Remove(client);
				}
			}
			//Although technically not required, now that we are no longer blocking, 
			//it is good to cut your CPU some slack
			Thread.Sleep(100);
		}
	}
}