using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPositionPacketHandler 
{
    void OnPositionPacketReceived(float x, float y, float z);
}
