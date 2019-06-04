using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    //public int BombsPoolingSize = 20;
    public GameObject BombPrefab;
    public GameObject OpponentPrefab;

    void Start()
    {
        Client.OnSpawnBombPacketReceived += SpawnBomb;
        Client.OnSpawnPlayersPacketReceived += SpawnPlayerOpponent;
    }

    private void SpawnBomb(int id, Vector3 pos, float radius, float startTimer)
    {
        //use pooling???
        ISpawnable spawnable = Instantiate(BombPrefab).GetComponent<ISpawnable>();
        spawnable.Spawn(id, pos, radius, startTimer);
    }

    private void SpawnPlayerOpponent(int id, Vector3 pos, byte textureToApply, string name)
    {
        Instantiate(OpponentPrefab).GetComponent<ISpawnable>().Spawn(id, pos, textureToApply, name);
    }
}
