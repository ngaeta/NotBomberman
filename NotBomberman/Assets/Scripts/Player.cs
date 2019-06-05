using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IJoinPacketHandler, IPositionPacketHandler, IDestroyPacketHandler
{
    public float Speed;
    public string Name = "Nicola";
    public string TexturesPath = "Textures/BombermanTexture";
    public float InputRate = 0.25f;
    public float SendJoinAfter = 1f;  //to show graphics effect
    public Client Client;
    public ScoreMng Score;
    public Renderer Renderer;
    public GameObject DeathEffectPrefab;

    private int id;
    private bool isAlive;
    private bool joinSuccess;
    private float inputRate;
    private Vector3 velocity;
    private Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
        isAlive = true;
        joinSuccess = false;
        Renderer.gameObject.SetActive(false);
        Invoke("SendJoin", SendJoinAfter);
        //Invoke("OnDestroyPacketReceived", 2f);
    }

    void Update()
    {
        if (joinSuccess)
        {
            if (isAlive && inputRate <= 0)
            {
                if (Input.GetKey(KeyCode.W))
                {
                    inputRate = InputRate;
                    velocity = new Vector3(0, 0, Speed);
                    anim.SetBool("Walk", true);
                    transform.rotation = Quaternion.Euler(Vector3.zero);
                    Client.SendVelocityPacket(velocity);
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    inputRate = InputRate;
                    velocity = new Vector3(0, 0, -Speed);
                    anim.SetBool("Walk", true);
                    transform.rotation = Quaternion.Euler(0, -180, 0);
                    Client.SendVelocityPacket(velocity);
                }
                else if (Input.GetKey(KeyCode.A))
                {
                    inputRate = InputRate;
                    velocity = new Vector3(-Speed, 0, 0);
                    anim.SetBool("Walk", true);
                    transform.rotation = Quaternion.Euler(0, -90, 0);
                    Client.SendVelocityPacket(velocity);
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    inputRate = InputRate;
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
            else
                inputRate -= Time.deltaTime;
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
        Renderer.gameObject.SetActive(true);
        Instantiate(DeathEffectPrefab, transform.position, Quaternion.identity);

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

    public void OnDestroyPacketReceived(string playerKilledYou)
    {
        isAlive = false;
        Instantiate(DeathEffectPrefab, transform.position, Quaternion.identity);

        playerKilledYou = playerKilledYou.TrimEnd();
        Score.SetPlayerStatus(Name);
        string gameOverText = (playerKilledYou == Name) ? "You killed yourself" : playerKilledYou + " killed you";
        Score.SetGameOverText(gameOverText);

        Client.UnregisterObject(id);
        Destroy(gameObject);
        //Application.Quit();
    }

    private void SendJoin()
    {
        Client.SendJoinPacket(Name, this);
    }
}
