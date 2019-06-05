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
    private Dictionary<int, string> playersName;

    void Start()
    {
        playersName = new Dictionary<int, string>();

        Client.OnSpawnPlayersPacketReceived += OnPlayerSpawn;
        Client.OnDestroyPacketReceived += OnDestroyPackReceived;
    }

    void OnPlayerSpawn(int id, Vector3 pos, byte textureToApply, string name)
    {
        if (WaitingText.activeSelf)
            WaitingText.SetActive(false);

        SetNextPlayerUI(name.TrimEnd(), textureToApply);
        playersName[id] = name.TrimEnd();
    }

    void OnDestroyPackReceived(int id, string name)
    {
        if(playersName.ContainsKey(id))
        {
            SetPlayerStatus(playersName[id]);
        }
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

    public void SetPlayerStatus(string playerName, string status = "Dead")
    {
        for(int i=0; i < PlayersName.Length; i++)
        {
            if(PlayersName[i].text == playerName)
            {
                PlayersStatus[i].text = status;
                PlayersStatus[i].color = Color.red;
                return;
            }
        }
    }
}
