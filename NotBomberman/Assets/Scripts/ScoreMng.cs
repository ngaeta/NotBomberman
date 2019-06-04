using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreMng : MonoBehaviour
{
    public Text[] PlayersName;
    public Image[] PlayersImage;
    public string SpritePath = "Textures/BombermanSprite";

    public Text Player1TextVal;
    public Text Player2TextVal;
    public Text Player3TextVal;
    public Text Player4TextVal;

    public int Player1Score;
    public int Player2Score;
    public int Player3Score;
    public int Player4Score;

    private int nextUIElement;

    void Start()
    {
        Client.OnSpawnPlayersPacketReceived += OnPlayerSpawn;
    }

    void Update()
    {
        //Player1TextVal.text = Player1Score.ToString("00");
        //Player2TextVal.text = Player2Score.ToString("00");
        //Player3TextVal.text = Player3Score.ToString("00");
        //Player4TextVal.text = Player4Score.ToString("00");
    }

    void OnPlayerSpawn(int id, Vector3 pos, byte textureToApply, string name)
    {
        SetNextPlayerUI(name, textureToApply);
    }

    public void SetNextPlayerUI(string name, byte spriteToApply)
    {
        Sprite sprite = Resources.Load<Sprite>(SpritePath + spriteToApply);
        PlayersImage[nextUIElement].sprite = sprite;
        PlayersImage[nextUIElement].gameObject.SetActive(true);
        PlayersName[nextUIElement].text = name;
        nextUIElement++;
    }
}
