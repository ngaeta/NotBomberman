using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISpawnable 
{
    void Spawn(int id, Vector3 pos, params object[] properties);
}
