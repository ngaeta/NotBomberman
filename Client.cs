using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;

public class Client : MonoBehaviour
{
    enum Operation : byte { Join, SendBomb, BombSpawn, SendVelocity, ReceivePos, SpawnPlayers, BombTimer, Die, Ack }

    public delegate void PositionPacketReceived(int obj_ID, Vector3 pos);
    public event PositionPacketReceived OnPlayersPosReceived;
    public int Port = 9999;
    public String PlayerName = "Pippo";

    private delegate void ReceiveOperations();
    private Dictionary<Operation, ReceiveOperations> receiveCommands;
    private Socket socket;
    private EndPoint endPoint;
    private byte[] receivedData;
    private Dictionary<int, Packet> packetNeedAck;

    // Start is called before the first frame update
    void Start()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        endPoint = new IPEndPoint(IPAddress.Loopback, Port);

        packetNeedAck = new Dictionary<int, Packet>();

        receiveCommands = new Dictionary<Operation, ReceiveOperations>();
        receiveCommands[Operation.ReceivePos] = ParsePositionPacket;
        receiveCommands[Operation.Ack] = Ack;

        Join();
    }

    // Update is called once per frame
    void Update()
    {
        //Receive operations
        DequeuePackets();
        if (receivedData != null && receiveCommands.ContainsKey((Operation)receivedData[0]))
        {
            Debug.Log("Pacchetto arrivato: " + PrintPacket(receivedData));
            receiveCommands[(Operation)receivedData[0]]();
        }

        //Resend packet without ack
        if (packetNeedAck.Count > 0)
        {
            foreach (int id in packetNeedAck.Keys)
            {
                Packet packet = packetNeedAck[id];
                if (packet.TimeRemainingToResend <= 0)
                {
                    socket.SendTo(packet.GetData(), endPoint);
                    packet.ResetTimeRemaining();
                }
                else
                    packet.TimeRemainingToResend -= Time.deltaTime;
            }
        }
    }

    public void SendVelocityPacket(Vector3 velocity)
    {
        byte command = (byte)Operation.SendVelocity;
        float x = velocity.x;
        float y = velocity.y;
        float z = velocity.z;

        Packet setVelocityPacket = new Packet(command, x, y, z);
        socket.SendTo(setVelocityPacket.GetData(), endPoint);
    }

    private void Join()
    {
        byte command = (byte)Operation.Join;

        Packet joinPacket = new Packet(command, PlayerName);
        joinPacket.ResendAfter = 1f;
        socket.SendTo(joinPacket.GetData(), endPoint);  
        
        packetNeedAck.Add(joinPacket.Id, joinPacket);
        PrintPacket(joinPacket.GetData());
    }

    private void Ack()
    {
        if (receivedData.Length != 5)
            return;

        int idPacket = BitConverter.ToInt32(receivedData, 1);
        if (packetNeedAck.ContainsKey(idPacket))
            packetNeedAck.Remove(idPacket);
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
