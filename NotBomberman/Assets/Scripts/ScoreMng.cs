using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreMng : MonoBehaviour
{
    public GameObject[] PlayersPanel;
    public Text[] PlayersName;
    public Image[] PlayersImage;
    public Text[] PlayersStatus;
    public TMP_Text GameOverTMPro;
    public GameObject WaitingText;
    public string SpritePath = "Textures/BombermanSprite";

    private int nextUIElement;

    void Start()
    {
        Client.OnSpawnPlayersPacketReceived += OnPlayerSpawn;
    }

    void OnPlayerSpawn(int id, Vector3 pos, byte textureToApply, string name)
    {
        if (WaitingText.activeSelf)
            WaitingText.SetActive(false);

        SetNextPlayerUI(name.TrimEnd(), textureToApply);
    }

    public void SetNextPlayerUI(string name, byte spriteToApply)
    {
        Sprite sprite = Resources.Load<Sprite>(SpritePath + spriteToApply);
        PlayersPanel[nextUIElement].SetActive(true);
        PlayersImage[nextUIElement].sprite = sprite;
        PlayersImage[nextUIElement].gameObject.SetActive(true);
        PlayersName[nextUIElement].text = name;
        nextUIElement++;
    }

    public void SetGameOverText(string text)
    {
        GameOverTMPro.text = text;
        GameOverTMPro.gameObject.SetActive(true);
    }

    public void SetPlayerStatus(string playerName)
    {
        for(int i=0; i < PlayersName.Length; i++)
        {
            if(PlayersName[i].text == playerName)
            {
                PlayersStatus[i].text = "Dead";
                PlayersStatus[i].color = Color.red;
                return;
            }
        }
    }
}
