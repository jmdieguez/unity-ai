using UnityEngine;

public class SoccerBallController : MonoBehaviour
{
    public AgentSoccer lastTouch;
    public GameObject area;
    [HideInInspector]
    public SoccerEnvController envController;
    public string purpleGoalTag; //will be used to check if collided with purple goal
    public string blueGoalTag; //will be used to check if collided with blue goal

    void Start()
    {
        envController = area.GetComponent<SoccerEnvController>();
        lastTouch = null;

    }
    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag(purpleGoalTag)) //ball touched purple goal
        {
            envController.GoalTouched(Team.Blue, lastTouch);
            //reseting();
        }
        if (col.gameObject.CompareTag(blueGoalTag)) //ball touched blue goal
        {
            envController.GoalTouched(Team.Purple, lastTouch);
            //reseting();
        }
    }
    public void reseting(){
        lastTouch = null;
    }
    public void touchedBy(AgentSoccer agentLastTouch){
        if(!lastTouch){
            Debug.Log("No toca nadie previamente");
        }
        else{
             Debug.Log("Toco Previamente: " + lastTouch.team + " " + lastTouch.position);
        }
        lastTouch = agentLastTouch;

        Debug.Log("Toco Despues: " + lastTouch.team + " " + lastTouch.position);

    }
}
