using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;

public class Client : MonoBehaviour
{
    enum Operation : byte { Join, SendBomb, BombSpawn, SendVelocity, ReceivePos, SpawnPlayers, BombTimer, Die }

    public delegate void PositionPacketReceived(int obj_ID, Vector3 pos);
    public event PositionPacketReceived OnPlayersPosReceived;
    public int Port = 9999;
    public String PlayerName = "Pippo";

    private delegate void ReceiveOperations();
    private Dictionary<Operation, ReceiveOperations> operations;
    private Socket socket;
    private EndPoint endPoint;
    private byte[] receivedData;

    // Start is called before the first frame update
    void Start()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        endPoint = new IPEndPoint(IPAddress.Loopback, Port);

        operations = new Dictionary<Operation, ReceiveOperations>();
        operations[Operation.ReceivePos] = ParsePositionPacket;
    }

    // Update is called once per frame
    void Update()
    {
        //Receive operations
        DequeuePackets();
        if (receivedData != null && operations.ContainsKey((Operation)receivedData[0]))
        {
            Debug.Log("Pacchetto arrivato: " + PrintPacket(receivedData));
            operations[(Operation)receivedData[0]]();
        }
    }

    public void SendVelocityPacket(Vector3 velocity)
    {
        byte[] setVelocityPacket = new byte[13];
        setVelocityPacket[0] = (byte)Operation.SendVelocity;

        byte[] velocityX;
        byte[] velocityY;
        byte[] velocityZ;
        velocityX = BitConverter.GetBytes(velocity.x);
        velocityY = BitConverter.GetBytes(velocity.y);
        velocityZ = BitConverter.GetBytes(velocity.z);

        Buffer.BlockCopy(velocityX, 0, setVelocityPacket, 1, 4);
        Buffer.BlockCopy(velocityY, 0, setVelocityPacket, 5, 4);
        Buffer.BlockCopy(velocityZ, 0, setVelocityPacket, 9, 4);

        socket.SendTo(setVelocityPacket, endPoint);
    }

    private void Join()
    {
        byte[] byteNames = Encoding.ASCII.GetBytes(PlayerName);
        byte[] dataToSend = new byte[byteNames.Length + 1];

        dataToSend[0] = (byte)Operation.Join;
        Buffer.BlockCopy(byteNames, 0, dataToSend, 1, byteNames.Length);
        socket.SendTo(dataToSend, endPoint);

        PrintPacket(dataToSend);
    }

    private void ParsePositionPacket()
    {
        if (receivedData.Length != 14)
            return;

        float x = BitConverter.ToSingle(receivedData, 2);
        float y = BitConverter.ToSingle(receivedData, 6);
        float z = BitConverter.ToSingle(receivedData, 10);
        if (OnPlayersPosReceived != null)
        {
            OnPlayersPosReceived(receivedData[1], new Vector3(x, y, z));
        }
    }

    private void DequeuePackets()
    {
        receivedData = null;
        byte[] receivedPacket = new byte[256];
        int rlen = 0;
        try
        {
            rlen = socket.Receive(receivedPacket);
            receivedData = new byte[rlen];
            Buffer.BlockCopy(receivedPacket, 0, receivedData, 0, rlen);
        }
        catch
        {
            return;
        }
    }

    private string PrintPacket(byte[] packet)
    {
        String returnString = "[";
        for (int i = 0; i < packet.Length; i++)
        {
            returnString += packet[i] + ", ";
        }
        returnString = "]";
        return returnString;
    }
}
