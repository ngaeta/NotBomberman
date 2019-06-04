using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentPlayer : MonoBehaviour, ISpawnable, IPositionPacketHandler, IDestroyPacketHandler
{
    public Renderer Renderer;
    public string TexturesPath = "Textures/BombermanTexture";
    public float TimeSinceLastPosPcktToReturnIdle = 0.35f;
    public GameObject DeathEffect;

    private int id;
    private Animator anim;
    private string playerName;
    private float timeSinceLastPositionPacket;

    void Start()
    {
        anim = GetComponent<Animator>();
        //Invoke("OnDestroyPacketReceived", 2f);
    }

    void Update()
    {
        if (anim.GetBool("Walk"))
        {
            if (timeSinceLastPositionPacket <= 0)
            {
                anim.SetBool("Walk", false);
            }
            else
                timeSinceLastPositionPacket -= Time.deltaTime;
        }
    }

    public void Spawn(int id, Vector3 pos, params object[] properties)
    {
        this.id = id;
        transform.position = pos;
        byte textureToApply = (byte)properties[0];
        Texture tex = Resources.Load<Texture>(TexturesPath + textureToApply);
        Renderer.material.SetTexture("_MainTex", tex);
        playerName = (string)properties[1];

        Client.RegisterObjPositionable(id, this);
        Client.RegisterObjDestroyable(id, this);
    }

    public void OnPositionPacketReceived(float x, float y, float z)
    {
        Vector3 newPos = new Vector3(x, y, z);
        SetRotation(newPos);
        transform.position = newPos;
        anim.SetBool("Walk", true);
        timeSinceLastPositionPacket = TimeSinceLastPosPcktToReturnIdle;
    }

    public void OnDestroyPacketReceived(string name)
    {
        Instantiate(DeathEffect, transform.position, Quaternion.identity);
        Client.UnregisterObject(id);

        Destroy(gameObject);
    }

    private void SetRotation(Vector3 newPos)
    {
        if (newPos.z < transform.position.z)
        {
            transform.rotation = Quaternion.Euler(0, -180, 0);
        }
        else if (newPos.z > transform.position.z)
        {
            transform.rotation = Quaternion.Euler(Vector3.zero);
        }
        else if (newPos.x < transform.position.x)
        {
            transform.rotation = Quaternion.Euler(0, -90, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 90, 0);
        }
    }
}
