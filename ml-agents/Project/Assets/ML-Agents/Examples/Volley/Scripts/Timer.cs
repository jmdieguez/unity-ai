using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    Text timerText;
    private float minutes,seconds;
    // Start is called before the first frame update
    void Start()
    {
        timerText = GetComponent<Text>();
        // timerText = "0:00";
    }

    // Update is called once per frame
    void Update()
    {
        minutes = (int)(Time.time/60f);
        seconds = (int)(Time.time%60f);

        timerText.text = minutes.ToString("0") + ":" + seconds.ToString("00");

    }
}
