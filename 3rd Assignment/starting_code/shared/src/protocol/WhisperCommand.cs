using System;
using System.Collections.Generic;
using System.Text;

namespace shared.src.protocol
{
    public class WhisperCommand : ISerializable
    {
        public string Message;

        public WhisperCommand() { }

        public WhisperCommand(string message)
        {
            Message = message;
        }

        public void Serialize(Packet p)
        {
            p.Write(Message);
        }

        public void Deserialize(Packet p)
        {
            Message = p.ReadString();
        }
    }
}
