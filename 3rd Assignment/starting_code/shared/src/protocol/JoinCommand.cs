using System;
using System.Collections.Generic;
using System.Text;

namespace shared.src.protocol
{
    public class JoinCommand : ISerializable
    {
        public int Id;
        public int Skin;
        public int X;
        public int Z;

        public JoinCommand() { }
        public JoinCommand(int id, int skin, int x, int z)
        {
            Id = id; Skin = skin; X = x; Z = z;
        }

        public void Serialize(Packet p)
        {
            p.Write(Id);
            p.Write(Skin);
            p.Write(X);
            p.Write(Z);
        }

        public void Deserialize(Packet p)
        {
            Id = p.ReadInt();
            Skin = p.ReadInt();
            X = p.ReadInt();
            Z = p.ReadInt();
        }
    }

}
