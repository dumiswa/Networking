using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;

class TCPServerSample
{
	/**
	 * This class implements a simple concurrent TCP Echo server.
	 * Read carefully through the comments below.
	 */
	public static void Main (string[] args)
	{
		Console.WriteLine("Server started on port 55555");

		TcpListener listener = new TcpListener (IPAddress.Any, 55555);
		listener.Start ();

		List<TcpClient> clients = new List<TcpClient>();

		while (true)
		{
			//First big change with respect to example 001
			//We no longer block waiting for a client to connect, but we only block if we know
			//a client is actually waiting (in other words, we will not block)
			//In order to serve multiple clients, we add that client to a list
			while (listener.Pending()) { 
				clients.Add(listener.AcceptTcpClient());
				Console.WriteLine("Accepted new client.");
			}

			//Second big change, instead of blocking on one client, 
			//we now process all clients IF they have data available
			foreach (TcpClient client in clients)
			{
				if (client.Available == 0) continue;
				NetworkStream stream = client.GetStream();
				StreamUtil.Write(stream, StreamUtil.Read(stream));
			}

			//Although technically not required, now that we are no longer blocking, 
			//it is good to cut your CPU some slack
			Thread.Sleep(100);
		}
	}
}


