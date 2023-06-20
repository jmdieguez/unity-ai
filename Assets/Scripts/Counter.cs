using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Counter : MonoBehaviour
{
    [SerializeField] Text text;
    
    int points;

    // Start is called before the first frame update
    void Start()
    {
        points = 0;
        updateHUD();
    }
    
    public void Point(){
        points += 1;
        updateHUD();

    }
    // Update de goles
    void updateHUD()
    {
        text.text = points.ToString();
    }
}
