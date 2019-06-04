using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IJoinPacketHandler, IPositionPacketHandler, IDestroyPacketHandler
{
    public float Speed;
    public string Name = "Player 1";
    public string TexturesPath = "Textures/BombermanTexture";
    public Client Client;
    public ScoreMng Score;
    public Renderer Renderer;
    public GameObject DeathEffect;

    private int id;
    private bool isAlive;
    private bool joinSuccess;
    private Vector3 velocity;
    private Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
        isAlive = true;
        joinSuccess = false;

        Client.SendJoinPacket(Name, this);
        //Invoke("OnDestroyPacketReceived", 2f);
    }

    void Update()
    {
        if (joinSuccess)
        {
            if (isAlive)
            {
                if (Input.GetKey(KeyCode.W))
                {
                    velocity = new Vector3(0, 0, Speed);
                    anim.SetBool("Walk", true);
                    transform.rotation = Quaternion.Euler(Vector3.zero);
                    Client.SendVelocityPacket(velocity);
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    velocity = new Vector3(0, 0, -Speed);
                    anim.SetBool("Walk", true);
                    transform.rotation = Quaternion.Euler(0, -180, 0);
                    Client.SendVelocityPacket(velocity);
                }
                else if (Input.GetKey(KeyCode.A))
                {
                    velocity = new Vector3(-Speed, 0, 0);
                    anim.SetBool("Walk", true);
                    transform.rotation = Quaternion.Euler(0, -90, 0);
                    Client.SendVelocityPacket(velocity);
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    velocity = new Vector3(Speed, 0, 0);
                    anim.SetBool("Walk", true);
                    transform.rotation = Quaternion.Euler(0, 90, 0);
                    Client.SendVelocityPacket(velocity);
                }
                else
                {
                    velocity = Vector3.zero;
                    anim.SetBool("Walk", false);
                }

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Client.SendShootBombPacket(transform.position);
                }
            }
        }
    }

    public void OnJoinPacketSucces(int id, Vector3 pos, byte textureToApply)
    {
        Debug.Log("JOIN SUCCESS");
        this.id = id;
        joinSuccess = true;
        transform.position = pos;
        Texture tex = Resources.Load<Texture>(TexturesPath + textureToApply);
        Renderer.material.SetTexture("_MainTex", tex);
        Score.SetNextPlayerUI(Name, textureToApply);

        Client.RegisterObjPositionable(this.id, this);
        Client.RegisterObjDestroyable(this.id, this);
    }

    public void OnJoinPacketFailed()
    {
        Debug.Log("JOIN FAILED");
        joinSuccess = false;
        //Show on UI join failed and eventually recall join method 
    }

    public void OnPositionPacketReceived(float x, float y, float z)
    {
        transform.position = new Vector3(x, y, z);
    }

    public void OnDestroyPacketReceived()
    {
        isAlive = false;
        Instantiate(DeathEffect, transform.position, Quaternion.identity);
        Client.UnregisterObject(id);

        Destroy(gameObject);
    } 
}
