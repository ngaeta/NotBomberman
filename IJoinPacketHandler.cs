using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IJoinPacketHandler
{
    void OnJoinPacketSucces(int id, Vector3 pos);
    void OnJoinPacketFailed();
}
