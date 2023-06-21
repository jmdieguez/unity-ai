using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoalScript : MonoBehaviour
{
    [SerializeField] Text text;
    
    int goals;

    // Start is called before the first frame update
    void Start()
    {
        goals = 0;
        updateHUD();
    }
    
    public void Goal(){
        goals = goals+1;
        updateHUD();

    }
    // Update de goles
    void updateHUD()
    {
        text.text = goals.ToString();
    }
}
