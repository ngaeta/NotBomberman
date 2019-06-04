using System.Collections;
using System.Collections.Generic;
using System.IO;

public class Packet
{
    public int Id { get; private set; }
    public float RemainingTimeToResend { get; set; }

    private float timeToResend;
    public float ResendAfter
    {
        set
        {
            timeToResend = value;
            RemainingTimeToResend = value;
        }
    }

    private static int packetCounter;
    private MemoryStream stream;
    private BinaryWriter writer;

    public Packet()
    {
        stream = new MemoryStream();
        writer = new BinaryWriter(stream);
        Id = ++packetCounter;
        ResendAfter = 0.05f;
    }

    public Packet(byte command, params object[] elements) : this()
    {
        writer.Write(command);
        foreach (object element in elements)
        {
            if (element is int)
            {
                writer.Write((int)element);
            }
            else if (element is float)
            {
                writer.Write((float)element);
            }
            else if (element is byte)
            {
                writer.Write((byte)element);
            }
            else if (element is char)
            {
                writer.Write((char)element);
            }
            else if (element is uint)
            {
                writer.Write((uint)element);
            }
            else if (element is string)
            {
                writer.Write((string)element);
            }
            else
            {
                throw new System.Exception("unknown type");
            }
        }
        writer.Write(Id);
    }

    public byte[] GetData()
    {
        return stream.ToArray();
    }

    public void ResetRemainingTimeResend()
    {
        RemainingTimeToResend = timeToResend;
    }
}