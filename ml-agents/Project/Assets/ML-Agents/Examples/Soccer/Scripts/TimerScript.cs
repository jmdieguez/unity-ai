using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerScript : MonoBehaviour
{
    [SerializeField] Text timerText;
    private float minutes,seconds;
    // Start is called before the first frame update
    void Start()
    {
        minutes = 0;
        seconds = 0;
    }

    // Update is called once per frame
    void Update()
    {
        minutes = (int)(Time.time/60f);
        seconds = (int)(Time.time%60f);

        timerText.text = minutes.ToString("0") + ":" + seconds.ToString("00");

    }
}
