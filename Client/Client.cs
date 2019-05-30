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
        Join, ShootBomb, SpawnObj, SendVelocity, ReceivePos, ReceiveTimer, ReceiveDestroy, JoinAck, Ack
    }

    const float DefaultTimeBeforeDeletePacket = 10f;

    public string Address = "192.168.3.194";
    public int Port = 9999;

    public delegate void SpawnObject(byte objType, int idObjSpawned, Vector3 pos);
    public static event SpawnObject OnSpawnPacketReceived;

    private delegate void PacketOperation();
    private Dictionary<Operation, PacketOperation> packetOperationHandler;
    private Dictionary<int, Packet> packetsNeedAck;
    private Dictionary<int, float> serverPacketsAlreadyArrived;

    private Dictionary<int, IPositionPacketHandler> positionableObj;
    private Dictionary<int, ITimerPacketHandler> countdownableObj;
    private Dictionary<int, IDestroyPacketHandler> destroyableObj;
    private IJoinPacketHandler clientJoin;

    private Socket socket;
    private EndPoint endPoint;
    private byte[] receivedData;

    void Awake()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        endPoint = new IPEndPoint(IPAddress.Parse(Address), Port);

        packetsNeedAck = new Dictionary<int, Packet>();
        serverPacketsAlreadyArrived = new Dictionary<int, float>();
        positionableObj = new Dictionary<int, IPositionPacketHandler>();
        countdownableObj = new Dictionary<int, ITimerPacketHandler>();
        destroyableObj = new Dictionary<int, IDestroyPacketHandler>();

        packetOperationHandler = new Dictionary<Operation, PacketOperation>();
        packetOperationHandler[Operation.SpawnObj] = ProcessSpawnPacket;
        packetOperationHandler[Operation.ReceivePos] = ProcessPositionPacket;
        packetOperationHandler[Operation.ReceiveTimer] = ProcessTimerPacket;
        packetOperationHandler[Operation.ReceiveDestroy] = ProcessDestroyPacket;
        packetOperationHandler[Operation.JoinAck] = JoinAckReceived;
        packetOperationHandler[Operation.Ack] = AckReceived;
    }

    void Update()
    {
        //Receive operations
        DequeuePackets();
        if (receivedData != null && packetOperationHandler.ContainsKey((Operation)receivedData[0]))
        {
            Debug.Log("Pacchetto arrivato: ");
            PrintPacket(receivedData);

            packetOperationHandler[(Operation)receivedData[0]]();
        }

        //Resend packet without ack
        foreach (int id in packetsNeedAck.Keys)
        {
            Packet packet = packetsNeedAck[id];
            if (packet.RemainingTimeToResend <= 0)
            {
                socket.SendTo(packet.GetData(), endPoint);
                packet.ResetRemainingTimeResend();
            }
            else
                packet.RemainingTimeToResend -= Time.deltaTime;
        }

        //Keep server packet in memory for n seconds before remove it, the ack could be not arrived.
        int[] packetsId = new int[serverPacketsAlreadyArrived.Count];
        serverPacketsAlreadyArrived.Keys.CopyTo(packetsId, 0);
        foreach (int id in packetsId)
        {
            float remainingTimeToDelete = serverPacketsAlreadyArrived[id] - Time.deltaTime;
            if (remainingTimeToDelete <= 0)
            {
                serverPacketsAlreadyArrived.Remove(id);
            }
            else
                serverPacketsAlreadyArrived[id] = remainingTimeToDelete;
        }
    }

    public void SendJoinPacket(string playerName, IJoinPacketHandler clientJoin)
    {
        this.clientJoin = clientJoin;

        byte command = (byte)Operation.Join;
        Packet joinPacket = new Packet(command, playerName);
        joinPacket.ResendAfter = 1f;
        socket.SendTo(joinPacket.GetData(), endPoint);

        packetsNeedAck.Add(joinPacket.Id, joinPacket);
        PrintPacket(joinPacket.GetData());
    }

    public void SendShootBombPacket(Vector3 position)
    {
        byte command = (byte)Operation.ShootBomb;
        Packet shootBombPacket = new Packet(command, position.x, position.y, position.z);
        shootBombPacket.ResendAfter = .03f;
        socket.SendTo(shootBombPacket.GetData(), endPoint);

        packetsNeedAck.Add(shootBombPacket.Id, shootBombPacket);
        PrintPacket(shootBombPacket.GetData());
    }

    public void SendVelocityPacket(Vector3 velocity)
    {
        byte command = (byte)Operation.SendVelocity;
        float x = velocity.x;
        float y = velocity.y;
        float z = velocity.z;

        Packet setVelocityPacket = new Packet(command, x, y, z);
        socket.SendTo(setVelocityPacket.GetData(), endPoint);
        PrintPacket(setVelocityPacket.GetData());
    }

    public bool RegisterObjPositionable(int id, IPositionPacketHandler positionable)
    {
        if (!positionableObj.ContainsKey(id))
        {
            positionableObj.Add(id, positionable);
            return true;
        }
        return false;
    }

    public bool RegisterObjTimerable(int id, ITimerPacketHandler timerable)
    {
        if (!countdownableObj.ContainsKey(id))
        {
            countdownableObj.Add(id, timerable);
            return true;
        }
        return false;
    }

    public bool RegisterObjDestroyable(int id, IDestroyPacketHandler destroyable)
    {
        if (!destroyableObj.ContainsKey(id))
        {
            destroyableObj.Add(id, destroyable);
            return true;
        }
        return false;
    }

    public void UnregisterObject(int id)
    {
        if(positionableObj.ContainsKey(id)) positionableObj.Remove(id);
        if(countdownableObj.ContainsKey(id)) countdownableObj.Remove(id);
        if(destroyableObj.ContainsKey(id)) destroyableObj.Remove(id);
    }

    private void ProcessSpawnPacket()
    {
        if (receivedData.Length != 22)
            return;

        int idPacket = BitConverter.ToInt32(receivedData, 18);

        if (!serverPacketsAlreadyArrived.ContainsKey(idPacket))
        {
            byte idObjToSpawn = receivedData[1];
            int idObj = BitConverter.ToInt32(receivedData, 2);
            float x = BitConverter.ToSingle(receivedData, 6);
            float y = BitConverter.ToSingle(receivedData, 10);
            float z = BitConverter.ToSingle(receivedData, 14);
            Debug.Log("Obj to spawn " + idObjToSpawn + " Id obj: " + idObj + " X: " + x + " Y: " + y + " Z: " + z);

            if (OnSpawnPacketReceived != null)
            {
                OnSpawnPacketReceived(idObjToSpawn, idObj, new Vector3(x, y, z));
            }
        }

        serverPacketsAlreadyArrived[idPacket] = DefaultTimeBeforeDeletePacket;     
        SendAck(idPacket);
    }

    private void ProcessPositionPacket()
    {
        if (receivedData.Length != 21)
            return;

        int id = BitConverter.ToInt32(receivedData, 1);
        float x = BitConverter.ToSingle(receivedData, 5);
        float y = BitConverter.ToSingle(receivedData, 9);
        float z = BitConverter.ToSingle(receivedData, 13);
        Debug.Log("Id obj: " + id + " X: " + x + " Y: " + y + " Z: " + z);

        //Send ack does not necessary
        positionableObj[id].OnPositionPacketReceived(x, y, z);
    }

    private void ProcessTimerPacket()
    {
        if (receivedData.Length != 13)
            return;

        int idObj = BitConverter.ToInt32(receivedData, 1);
        float currTimer = BitConverter.ToSingle(receivedData, 5);
        Debug.Log("Id obj: " + idObj + " Timer: " + currTimer);

        //Send ack does not necessary
        countdownableObj[idObj].OnTimerPacketRecevied(currTimer);
    }

    private void ProcessDestroyPacket()
    {
        if (receivedData.Length != 9)
            return;

        int idPacket = BitConverter.ToInt32(receivedData, 5);

        if (!serverPacketsAlreadyArrived.ContainsKey(idPacket))
        {
            int playerId = BitConverter.ToInt32(receivedData, 1);
            destroyableObj[playerId].OnDestroyPacketReceived();
            Debug.Log("Id player: " + playerId);
        }

        serverPacketsAlreadyArrived[idPacket] = DefaultTimeBeforeDeletePacket;
        SendAck(idPacket);
    }

    private void JoinAckReceived()
    {
        //join failed packet [command, 0, idPacket]
        if (receivedData.Length == 6)
        {
            int idPacket = BitConverter.ToInt32(receivedData, 2);
            if (!serverPacketsAlreadyArrived.ContainsKey(idPacket))
            {
                packetsNeedAck.Remove(idPacket);
                serverPacketsAlreadyArrived.Add(idPacket, DefaultTimeBeforeDeletePacket);
                clientJoin.OnJoinPacketFailed();              
            }
            return;
        }

        //join succes packet [command, 1, idPlayer, x, y, z, idPacket]
        if (receivedData.Length == 22)
        {
            int idPacket = BitConverter.ToInt32(receivedData, 18);
            if (!serverPacketsAlreadyArrived.ContainsKey(idPacket))
            {
                bool isJoined = receivedData[1] == 1;
                if (isJoined)
                {
                    int idPlayer = BitConverter.ToInt32(receivedData, 2);
                    float x = BitConverter.ToSingle(receivedData, 6);
                    float y = BitConverter.ToSingle(receivedData, 10);
                    float z = BitConverter.ToSingle(receivedData, 14);

                    Debug.Log("Id player: " + idPlayer + " X: " + x + " Y: " + y + " Z: " + z);
                    clientJoin.OnJoinPacketSucces(idPlayer, new Vector3(x, y, z));
                }
                else
                    clientJoin.OnJoinPacketFailed();

                packetsNeedAck.Remove(idPacket);
                serverPacketsAlreadyArrived.Add(idPacket, DefaultTimeBeforeDeletePacket);
            }       
        }
    }

    private void AckReceived()
    {
        if (receivedData.Length != 5)
            return;

        int idPacket = BitConverter.ToInt32(receivedData, 1);
        if (packetsNeedAck.ContainsKey(idPacket))
            packetsNeedAck.Remove(idPacket);
    }

    private void SendAck(int packetId)
    {
        byte command = (byte)Operation.Ack;
        Packet ackPacket = new Packet(command, packetId);

        socket.SendTo(ackPacket.GetData(), endPoint);
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