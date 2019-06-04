using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TMPROAnim : MonoBehaviour
{
    public float AnimSpeed = 1f;
    public string[] TextAnim;

    private TMP_Text textPro;
    private int currIndexAnim;
    private float currTimer;

    // Start is called before the first frame update
    void Start()
    {
        textPro = GetComponent<TMP_Text>();
        currTimer = AnimSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if (currTimer <= 0)
        {
            currTimer = AnimSpeed;
            currIndexAnim = (currIndexAnim + 1) % TextAnim.Length;

            textPro.text = TextAnim[currIndexAnim];
        }
        else
            currTimer -= Time.deltaTime;
    }
}
