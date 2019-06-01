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
        Join, ShootBomb, SpawnObj, SendVelocity, ReceivePos, ReceiveTimer, ReceiveDestroy, JoinAck, Ack, Alive
    }

    const float DefaultTimeBeforeDeletePacket = 15f;
    const float SendTimePacketAlive = 4f;

    public string Address = "79.37.15.73";
    public int Port = 9999;

    public delegate void SpawnObject(byte objType, int idObjSpawned, Vector3 pos);
    public static event SpawnObject OnSpawnPacketReceived;

    private static Dictionary<int, IPositionPacketHandler> positionableObj;
    private static Dictionary<int, ITimerPacketHandler> countdownableObj;
    private static Dictionary<int, IDestroyPacketHandler> destroyableObj;
    private static IJoinPacketHandler clientJoin;

    private delegate void PacketOperation();
    private Dictionary<Operation, PacketOperation> packetOperationHandler;
    private Dictionary<int, Packet> packetsNeedAck;
    private Dictionary<int, float> serverPacketsAlreadyArrived;
    private Packet packetAlive;
    private Socket socket;
    private EndPoint endPoint;
    private byte[] receivedData;

    void Awake()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        endPoint = new IPEndPoint(IPAddress.Parse(Address), Port);
        packetAlive = new Packet((byte) Operation.Alive);
        packetAlive.ResendAfter = SendTimePacketAlive;

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

        //Sending packet every n seconds otherwise the server will think that the connection has fallen.
        if (packetAlive.RemainingTimeToResend <= 0)
        {
            socket.SendTo(packetAlive.GetData(), endPoint);
            packetAlive.ResetRemainingTimeResend();
        }
        else
            packetAlive.RemainingTimeToResend -= Time.deltaTime;
    }

    public void SendJoinPacket(string playerName, IJoinPacketHandler joinHandler)
    {
        clientJoin = joinHandler;

        byte command = (byte)Operation.Join;
        Packet joinPacket = new Packet(command, playerName);
        joinPacket.ResendAfter = 3f;
        socket.SendTo(joinPacket.GetData(), endPoint);

        packetsNeedAck.Add(joinPacket.Id, joinPacket);
    }

    public void SendShootBombPacket(Vector3 position)
    {
        byte command = (byte)Operation.ShootBomb;
        Packet shootBombPacket = new Packet(command, position.x, position.y, position.z);
        shootBombPacket.ResendAfter = 0.2f;
        socket.SendTo(shootBombPacket.GetData(), endPoint);
        Debug.Log("shoot bomb packet id: " + shootBombPacket.Id);

        packetsNeedAck.Add(shootBombPacket.Id, shootBombPacket);
    }

    public void SendVelocityPacket(Vector3 velocity)
    {
        byte command = (byte)Operation.SendVelocity;
        float x = velocity.x;
        float y = velocity.y;
        float z = velocity.z;
        Packet setVelocityPacket = new Packet(command, x, y, z);
        Debug.Log("Velocity packet with id: " + setVelocityPacket.Id);
        socket.SendTo(setVelocityPacket.GetData(), endPoint);
    }

    public static bool RegisterObjPositionable(int id, IPositionPacketHandler positionable)
    {
        if (!positionableObj.ContainsKey(id))
        {
            positionableObj.Add(id, positionable);
            return true;
        }
        return false;
    }

    public static bool RegisterObjTimerable(int id, ITimerPacketHandler timerable)
    {
        if (!countdownableObj.ContainsKey(id))
        {
            countdownableObj.Add(id, timerable);
            return true;
        }
        return false;
    }

    public static bool RegisterObjDestroyable(int id, IDestroyPacketHandler destroyable)
    {
        if (!destroyableObj.ContainsKey(id))
        {
            destroyableObj.Add(id, destroyable);
            return true;
        }
        return false;
    }

    public static void UnregisterObject(int id)
    {
        if(positionableObj.ContainsKey(id)) positionableObj.Remove(id);
        if(countdownableObj.ContainsKey(id)) countdownableObj.Remove(id);
        if(destroyableObj.ContainsKey(id)) destroyableObj.Remove(id);
    }

    private void ProcessSpawnPacket()
    {
        Debug.Log("SPAWN PACKET");
        if (receivedData.Length != 22)
        {
            Debug.Log("lUNGHEZZA SBAGLIATA");
            return;
        }

        int idPacket = BitConverter.ToInt32(receivedData, 18);
        Debug.Log("SPAWN id PACKET " + idPacket);

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
        Debug.Log("Position packet received with id: " + BitConverter.ToSingle(receivedData, 14));

        //Send ack does not necessary
        positionableObj[id].OnPositionPacketReceived(x, y, z);
    }

    private void ProcessTimerPacket()
    {
        if (receivedData.Length != 13)
            return;

        int idObj = BitConverter.ToInt32(receivedData, 1);
        float currTimer = BitConverter.ToSingle(receivedData, 5);

        Debug.Log("Timer packet with id: " + BitConverter.ToInt32(receivedData, 9));
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

        //join succes packet [command, 1, idPlayer, x, y, z, textureToUse, idPacket]
        if (receivedData.Length == 23)
        {
            int idPacket = BitConverter.ToInt32(receivedData, 19);
            if (!serverPacketsAlreadyArrived.ContainsKey(idPacket))
            {
                bool isJoined = receivedData[1] == 1;
                if (isJoined)
                {
                    int idPlayer = BitConverter.ToInt32(receivedData, 2);
                    float x = BitConverter.ToSingle(receivedData, 6);
                    float y = BitConverter.ToSingle(receivedData, 10);
                    float z = BitConverter.ToSingle(receivedData, 14);

                    clientJoin.OnJoinPacketSucces(idPlayer, new Vector3(x, y, z), receivedData[18]);
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
        Debug.Log("Ack received for packet: " + idPacket);
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