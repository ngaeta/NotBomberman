using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IClientJoinable
{
    void OnJoinSucces(int id, Vector3 pos);
    void OnJoinFailed();
}
