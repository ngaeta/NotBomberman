using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IClientTimerable
{
    void OnTimerPacketRecevied(float newTimer);
}
