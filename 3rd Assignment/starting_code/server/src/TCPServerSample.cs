using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;


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
    private readonly Dictionary<TcpClient, (int id, int skin, int x, int z)> _avatars
        = new Dictionary<TcpClient, (int, int, int, int)>();

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

            Thread.Sleep(100);   // be kind to the CPU
        }
    }


    private void processNewClients()
    {
        while (_listener.Pending())
        {
            TcpClient c = _listener.AcceptTcpClient();
            _clients.Add(c);


            var avatar = (
                id: _rnd.Next(1000, 9999),
                skin: _rnd.Next(0, 4),
                x: _rnd.Next(-3000, 3000),
                z: _rnd.Next(-3000, 3000)
            );
            _avatars[c] = avatar;

            //  full world to newcomer
            StreamUtil.Write(c.GetStream(), BuildWorldPacket().GetBytes());

            //  newcomer announcement to everyone else
            Broadcast(BuildJoinPacket(avatar), except: c);

            Console.WriteLine($"Accepted new client #{avatar.id}.");
        }
    }


    private void processExistingClients()
    {
        foreach (TcpClient sender in _clients.ToArray())
        {
            if (sender.Available == 0) continue;

            byte[] data;
            try { data = StreamUtil.Read(sender.GetStream()); }
            catch { closeAndRemove(sender); continue; }

            Packet p = new Packet(data);
            string type = p.ReadString();

            if (!_avatars.ContainsKey(sender)) continue;   // safety net
            var me = _avatars[sender];

            switch (type)
            {
                case "moveReq":
                    me.x = p.ReadInt();
                    me.z = p.ReadInt();
                    _avatars[sender] = me;                 // store back
                    Broadcast(BuildMovePacket(me));
                break;

                case "textReq":
                    string msg = p.ReadString();
                    Broadcast(BuildTextPacket(me.id, msg));
                break;

                case "whisperReq":
                    handleWhisper(sender, me, p.ReadString());
                break;
            }
        }
    }


    private Packet BuildWorldPacket()
    {
        Packet p = new Packet();
        p.Write("world");
        p.Write(_avatars.Count);
        foreach (var a in _avatars.Values)
        {
            p.Write(a.id);
            p.Write(a.skin);
            p.Write(a.x); 
            p.Write(a.z);
        }
        return p;
    }
    private Packet BuildJoinPacket((int id, int skin, int x, int z) a)
    {
        Packet p = new Packet();
        p.Write("pJoin");
        p.Write(a.id); 
        p.Write(a.skin);
        p.Write(a.x);
        p.Write(a.z);
        return p;
    }
    private Packet BuildMovePacket((int id, int skin, int x, int z) a)
    {
        Packet p = new Packet();
        p.Write("pMove");
        p.Write(a.id);
        p.Write(a.x);
        p.Write(a.z);
        return p;
    }
    private Packet BuildTextPacket(int id, string msg)
    {
        Packet p = new Packet();
        p.Write("pText");
        p.Write(id); 
        p.Write(msg);
        return p;
    }
    private Packet BuildWhisperPacket(int id, string msg)
    {
        Packet p = new Packet();
        p.Write("pWhisper");
        p.Write(id);
        p.Write(msg);
        return p;
    }
    private Packet BuildLeavePacket(int id)
    {
        Packet p = new Packet();
        p.Write("pLeave");
        p.Write(id);
        return p;
    }

    private void Broadcast(Packet p, TcpClient except = null)
    {
        byte[] bytes = p.GetBytes();
        foreach (TcpClient c in _clients.ToArray())
        {
            if (c == except) continue;
            try { StreamUtil.Write(c.GetStream(), bytes); }
            catch { closeAndRemove(c); }
        }
    }

    private void handleWhisper(TcpClient sender, (int id, int skin, int x, int z) me, string msg)
    {
        const int WHISPER_RANGE = 2000;

        Packet whisperPacket = BuildWhisperPacket(me.id, msg);
        byte[] bytes = whisperPacket.GetBytes();

        foreach (var avatar in _avatars)
        {
            int deltaX = me.x - avatar.Value.x;
            int deltaY = me.z - avatar.Value.z;

            if (deltaX * deltaX + deltaY * deltaY < WHISPER_RANGE * WHISPER_RANGE)
            {
                try { StreamUtil.Write(avatar.Key.GetStream(), bytes); }
                catch {  closeAndRemove (avatar.Key); }
            }
        }
    }


    private void closeAndRemove(TcpClient client)
    {
        // notify others first
        if (_avatars.Remove(client, out var gone))
            Broadcast(BuildLeavePacket(gone.id), except: client);

        try { client.Close(); } catch { }
        _clients.Remove(client);
    }
}
