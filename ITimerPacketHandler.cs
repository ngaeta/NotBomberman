using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITimerPacketHandler
{
    void OnTimerPacketRecevied(float currTimer);
}
