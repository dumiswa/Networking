using System;
using System.Collections.Generic;
using System.Text;

namespace shared.src.protocol
{
    public class WorldCommand : ISerializable
    {
        public List<AvatarModel> Avatars;

        public WorldCommand() { }

        public WorldCommand(List<AvatarModel> avatars)
        {
            Avatars = avatars;
        }

        public void Serialize(Packet p)
        {
            p.Write(Avatars.Count);
            foreach (var a in Avatars)
                a.Serialize(p);
        }

        public void Deserialize(Packet p)
        {
            int count = p.ReadInt();
            Avatars = new List<AvatarModel>();
            for (int i = 0; i < count; i++)
            {
                var a = new AvatarModel();
                a.Deserialize(p);
                Avatars.Add(a);
            }
        }
    }
}
