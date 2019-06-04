using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentPlayer : MonoBehaviour, ISpawnable, IPositionPacketHandler, IDestroyPacketHandler
{
    public Renderer Renderer;
    public string TexturesPath = "Textures/BombermanTexture";
    public GameObject DeathEffect;

    private int id;
    private Animator anim;
    private string playerName;

    void Start()
    {
        anim = GetComponent<Animator>();
        //Invoke("OnDestroyPacketReceived", 2f);
    }

    public void Spawn(int id, Vector3 pos, params object[] properties)
    {
        this.id = id;
        transform.position = pos;
        byte textureToApply = (byte)properties[0];
        Texture tex = Resources.Load<Texture>(TexturesPath + textureToApply);
        Renderer.material.SetTexture("_MainTex", tex);
        playerName = (string)properties[1];
        //Set name on UI

        Client.RegisterObjPositionable(id, this);
        Client.RegisterObjDestroyable(id, this);
    }

    public void OnPositionPacketReceived(float x, float y, float z)
    {
        Vector3 newPos = new Vector3(x, y, z);
        if (transform.position - newPos != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(newPos);
            transform.position = newPos;
            anim.SetBool("Walk", true);
        }
        else
        {
            anim.SetBool("Walk", false);
        }
    }

    public void OnDestroyPacketReceived()
    {
        Instantiate(DeathEffect, transform.position, Quaternion.identity);
        Client.UnregisterObject(id);

        Destroy(gameObject);
    }
}
