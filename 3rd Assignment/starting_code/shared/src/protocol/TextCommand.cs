using System;
using System.Collections.Generic;
using System.Text;

namespace shared.src.protocol
{
    public class TextCommand : ISerializable
    {
        public int Id;
        public string Message;

        public TextCommand() { }

        public TextCommand(int id, string message)
        {
            Id = id;
            Message = message;
        }

        public void Serialize(Packet p)
        {
            p.Write(Id);
            p.Write(Message);
        }

        public void Deserialize(Packet p)
        {
            Id = p.ReadInt();
            Message = p.ReadString();
        }
    }
}
