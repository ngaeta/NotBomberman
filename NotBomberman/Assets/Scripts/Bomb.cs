using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour, ITimerPacketHandler, ISpawnable
{
    public GameObject Explosion;

    public bool DebugStartCountDown = false;

    private Animator anim;
    private int id;
    private float radius = 1f;
    private float currTimer = 3f;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (DebugStartCountDown)
        {
            currTimer -= Time.deltaTime;
            OnTimerPacketRecevied(currTimer);
            if (currTimer <= 0)
            {
                DebugStartCountDown = false;
            }
        }      
    }

    public void Spawn(int id, Vector3 pos, params object[] properties)
    {
        this.id = id;
        transform.position = pos;
        radius = (float)properties[0];
        currTimer = (float)properties[1];

        anim = GetComponent<Animator>(); //in start does not work???
        anim.SetBool("IsActive", true);
        
        Client.RegisterObjTimerable(id, this);
    }

    public void OnTimerPacketRecevied(float currTimer)
    {
        this.currTimer = currTimer;
        if (currTimer <= 0)
        {
            GameObject explosion = Instantiate(Explosion, transform.position, Quaternion.identity);
            explosion.transform.localScale = new Vector3(radius, radius, radius);
            Destroy(gameObject);
        }
        else
        {
            //Set UI with Mathf.Ceil(currTimer) value
        }
    }
}
