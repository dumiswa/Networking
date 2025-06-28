using shared;
namespace shared.src.protocol
{
    public class ChangeSkinCommand : ISerializable
    {
        public int Id;
        public int NewSkin;

        public ChangeSkinCommand() { }
        public ChangeSkinCommand(int id, int newSkin) 
        {
            Id = id; 
            NewSkin = newSkin;
        }
        public void Serialize(Packet p)
        {
            p.Write(Id);
            p.Write(NewSkin);
        }

        public void Deserialize(Packet p)
        {
            Id = p.ReadInt();
            NewSkin = p.ReadInt();
        }
    }
}
