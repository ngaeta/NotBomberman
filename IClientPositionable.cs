using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IClientPositionable 
{
    void OnPositionPacketReceived(float x, float y, float z);
}
