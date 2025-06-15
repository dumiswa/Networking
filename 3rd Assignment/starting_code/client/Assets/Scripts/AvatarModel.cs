using UnityEngine;
using shared;

public class AvatarModel : ISerializable
{
    public int Id;
    public int Skin;
    public float X;
    public float Y;

    public void Serialize(Packet p)
    {
        p.Write(Id);
        p.Write(Skin);
        p.Write((int)(X * 10000));
        p.Write((int)(Y * 1000));
    }

    public void Deserialize(Packet p)
    {
        Id = p.ReadInt();
        Skin = p.ReadInt();
        X = p.ReadInt() / 1000f;
        Y = p.ReadInt() / 1000f;
    }

    public Vector3 GetPosition() => new Vector3(X, 0, Y);

}
