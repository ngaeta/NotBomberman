using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;

public class Client : MonoBehaviour
{
    enum Operation : byte
    {
        Join, SendBomb, SpawnObj, SendVelocity, ReceivePos, BombTimer, Die, JoinAck, Ack
    }

    public delegate void SpawnObject(int id, float x, float y, float z);
    public static event SpawnObject OnSpawnPacketReceived;
    public int Port = 9999;

    private delegate void ReceiveOperations();
    private Dictionary<Operation, ReceiveOperations> receiveCommands;

    private Dictionary<int, Packet> packetNeedAck;
    private Dictionary<int, IClientPositionable> positionableObj;
    private Dictionary<string, IClientJoinable> joinablePlayers;  //There could be more players
    private IClientJoinable clientJoin;
    private Socket socket;
    private EndPoint endPoint;
    private byte[] receivedData;  

    // Start is called before the first frame update
    void Start()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        endPoint = new IPEndPoint(IPAddress.Loopback, Port);

        packetNeedAck = new Dictionary<int, Packet>();

        positionableObj = new Dictionary<int, IClientPositionable>();
        joinablePlayers = new Dictionary<string, IClientJoinable>();

        receiveCommands = new Dictionary<Operation, ReceiveOperations>();
        receiveCommands[Operation.ReceivePos] = PositionPacketCallback;
        receiveCommands[Operation.SpawnObj] = SpawnPacketCallback;
        receiveCommands[Operation.JoinAck] = JoinAckReceived;
        receiveCommands[Operation.Ack] = AckReceived;
    }

    // Update is called once per frame
    void Update()
    {
        //Receive operations
        DequeuePackets();
        if (receivedData != null && receiveCommands.ContainsKey((Operation)receivedData[0]))
        {
            Debug.Log("Pacchetto arrivato: ");
            PrintPacket(receivedData);

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

    public void Join(String playerName, IClientJoinable clientJoin)
    {
        this.clientJoin = clientJoin;

        byte command = (byte)Operation.Join;

        Packet joinPacket = new Packet(command, playerName);
        joinPacket.ResendAfter = 1f;
        socket.SendTo(joinPacket.GetData(), endPoint);

        packetNeedAck.Add(joinPacket.Id, joinPacket);
        PrintPacket(joinPacket.GetData());
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

    public bool RegisterObjPositionable(int id, IClientPositionable positionable)
    {
        if(!positionableObj.ContainsKey(id))
        {
            positionableObj.Add(id, positionable);
            return true;
        }
        return false;
    }

    private void SpawnPacketCallback()
    {
        if (receivedData.Length != 17)
            return;
    
        int idObjToSpawn = BitConverter.ToInt32(receivedData, 1);
        float x = BitConverter.ToSingle(receivedData, 5);
        float y = BitConverter.ToSingle(receivedData, 9);
        float z = BitConverter.ToSingle(receivedData, 13);

        if(OnSpawnPacketReceived != null)
        {
            OnSpawnPacketReceived(idObjToSpawn, x, y, z);
        }
    }

    private void PositionPacketCallback()
    {
        if (receivedData.Length != 17)
            return;

        int id = BitConverter.ToInt32(receivedData, 1);
        float x = BitConverter.ToSingle(receivedData, 5);
        float y = BitConverter.ToSingle(receivedData, 9);
        float z = BitConverter.ToSingle(receivedData, 13);

        positionableObj[id].OnPositionPacketReceived(x, y, z);
    }

    private void JoinAckReceived()
    {
        //join ack packet only with boolean byte [idPacket, byte(1=true, 0=false)]
        if (receivedData.Length == 2)
        {
            clientJoin.OnJoinFailed();
            return;
        }

        if (receivedData.Length != 18)
            return;

        bool isJoined = receivedData[1] == 1;
        if (isJoined)
        {
            int id = BitConverter.ToInt32(receivedData, 1);
            float x = BitConverter.ToSingle(receivedData, 5);
            float y = BitConverter.ToSingle(receivedData, 9);
            float z = BitConverter.ToSingle(receivedData, 13);
            clientJoin.OnJoinSucces(id, new Vector3(x, y, z));
        }
        else
            clientJoin.OnJoinFailed(); 
    }

    private void AckReceived()
    {
        if (receivedData.Length != 5)
            return;

        int idPacket = BitConverter.ToInt32(receivedData, 1);
        if (packetNeedAck.ContainsKey(idPacket))
            packetNeedAck.Remove(idPacket);
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

    private void PrintPacket(byte[] packet)
    {
        String stringToPrint = "[";
        for (int i = 0; i < packet.Length; i++)
        {
            stringToPrint += packet[i] + ", ";
        }
        stringToPrint += "]";

        Debug.Log(stringToPrint);
    }
}
