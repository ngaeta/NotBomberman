using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreMng : MonoBehaviour
{
    public Text Player1TextVal;
    public Text Player2TextVal;
    public Text Player3TextVal;
    public Text Player4TextVal;

    public Text Player1Name;
    public Text Player2Name;
    public Text Player3Name;
    public Text Player4Name;

    public int Player1Score;
    public int Player2Score;
    public int Player3Score;
    public int Player4Score;


    void Update()
    {
        Player1TextVal.text = Player1Score.ToString("00");
        Player2TextVal.text = Player2Score.ToString("00");
        Player3TextVal.text = Player3Score.ToString("00");
        Player4TextVal.text = Player4Score.ToString("00");
    }
}
