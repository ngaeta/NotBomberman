using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDestroyPacketHandler 
{
    void OnDestroyPacketReceived(string playerKilledYou);
}
