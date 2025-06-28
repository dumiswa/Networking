using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using shared;
using shared.src.protocol;

class TCPServerSample
{
    public static void Main()
    {
        TCPServerSample server = new TCPServerSample();
        server.run();
    }

    private TcpListener _listener;
    private readonly List<TcpClient> _clients = new List<TcpClient>();

    // authoritative avatar store: key = socket, value = (id,skin,x,z)
    private readonly Dictionary<TcpClient, (int id, int skin, int x, int z)> _avatars = new();

    private readonly Random _rnd = new Random();

    private void run()
    {
        Console.WriteLine("Server started on port 55555");

        _listener = new TcpListener(IPAddress.Any, 55555);
        _listener.Start();

        while (true)
        {
            processNewClients();
            processExistingClients();
            cleanupFaultyClients();

            Thread.Sleep(100);   // be kind to the CPU
        }
    }

    private void processNewClients()
    {
        while (_listener.Pending()) //polling
        {
            TcpClient c = null;

            try
            {
                c = _listener.AcceptTcpClient();
                _clients.Add(c);

                var avatar = (
                    id: _rnd.Next(1000, 9999),
                    skin: _rnd.Next(0, 4),
                    x: _rnd.Next(-3000, 3000),
                    z: _rnd.Next(-3000, 3000)
                );
                _avatars[c] = avatar; //store data

                // full world to new client
                var worldPacket = new Packet(new WorldCommand(buildAvatarModels()));
                StreamUtil.Write(c.GetStream(), worldPacket.GetBytes());

                // announcement to everyone else
                var joinPacket = new Packet(new JoinCommand(avatar.id, avatar.skin, avatar.x, avatar.z));
                Broadcast(joinPacket, except: c);

                Console.WriteLine($"Accepted new client #{avatar.id}.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while processing new client {e.Message}");
            }
        }
    }

    private void processExistingClients()
    {
        foreach (TcpClient sender in _clients.ToArray())
        {
            if (sender.Available == 0) continue; //polling

            byte[] data;
            try { data = StreamUtil.Read(sender.GetStream()); }
            catch
            {
                // skip failed read (will be cleaned later)
                continue;
            }

            if (!_avatars.ContainsKey(sender)) continue; // safety net
            var me = _avatars[sender];

            ISerializable obj;
            try { obj = new Packet(data).ReadObject(); }
            catch (Exception e)
            {
                Console.WriteLine($"Packet parse failed: {e.Message}");
                continue;
            }

            switch (obj)
            {
                case MoveCommand move:
                    me.x = move.X;
                    me.z = move.Z;
                    _avatars[sender] = me;
                    Broadcast(new Packet(new MoveCommand(me.id, me.x, me.z)));
                    break;

                case TextCommand text:
                    Broadcast(new Packet(new TextCommand(me.id, text.Message)));
                    break;

                case WhisperCommand whisper:
                    handleWhisper(sender, me, whisper.Message);
                    break;
            }
        }
    }

    private void handleWhisper(TcpClient sender, (int id, int skin, int x, int z) me, string msg)
    {
        const int WHISPER_RANGE = 2000;

        Packet whisperPacket = new Packet(new TextCommand(me.id, $"(whisper) {msg}"));
        byte[] bytes = whisperPacket.GetBytes();

        foreach (var avatar in _avatars)
        {
            int deltaX = me.x - avatar.Value.x;
            int deltaZ = me.z - avatar.Value.z;

            if (deltaX * deltaX + deltaZ * deltaZ < WHISPER_RANGE * WHISPER_RANGE)
            {
                try { StreamUtil.Write(avatar.Key.GetStream(), bytes); }
                catch { /*closeAndRemove (avatar.Key);*/ }
            }
        }
    }

    private void cleanupFaultyClients()
    {
        foreach (var client in _clients.ToArray())
        {
            if (!client.Connected || (client.Client.Poll(0, SelectMode.SelectRead) && client.Available == 0))
            {
                Console.WriteLine($"Client #{_avatars[client].id} removed");
                if (_avatars.Remove(client, out var gone))
                    Broadcast(new Packet(new LeaveCommand(gone.id)), except: client);

                try { client.Close(); } catch { }
                _clients.Remove(client);
            }
        }
    }

    private void Broadcast(Packet p, TcpClient except = null)
    {
        byte[] bytes = p.GetBytes(); //convert p to b
        foreach (TcpClient c in _clients.ToArray())
        {
            if (c == except) continue;
            try { StreamUtil.Write(c.GetStream(), bytes); }
            catch { /*closeAndRemove(c);*/ }
        }
    }

    private List<AvatarModel> buildAvatarModels()
    {
        var list = new List<AvatarModel>();
        foreach (var a in _avatars.Values)
            list.Add(new AvatarModel(a.id, a.skin, a.x, a.z));
        return list;
    }
}
