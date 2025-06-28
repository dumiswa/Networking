using System;
using System.Collections.Generic;
using System.Text;

namespace shared.src.protocol
{
    public class LeaveCommand : ISerializable
    {
        public int Id;

        public LeaveCommand() { }

        public LeaveCommand(int id)
        {
            Id = id;
        }

        public void Serialize(Packet p)
        {
            p.Write(Id);
        }

        public void Deserialize(Packet p)
        {
            Id = p.ReadInt();
        }
    }
}
