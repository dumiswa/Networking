using System;

[Serializable]
public class MoveCommand
{
    public float X;
    public float Y;

    public MoveCommand(float x, float z)
    {
        X = x;
        Y = z;
    }
}

