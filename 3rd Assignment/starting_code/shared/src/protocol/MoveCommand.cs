using shared;

namespace shared.src.protocol            
{
    public class MoveCommand : ISerializable
    {
        public int Id;
        public int X;
        public int Z;

        public MoveCommand() { }

        public MoveCommand(int id, int x, int z)
        {
            Id = id;
            X = x;
            Z = z;
        }

        public void Serialize(Packet p)
        {
            p.Write(Id);
            p.Write(X);
            p.Write(Z);
        }

        public void Deserialize(Packet p)
        {
            Id = p.ReadInt();
            X = p.ReadInt();
            Z = p.ReadInt();
        }
    }
}
