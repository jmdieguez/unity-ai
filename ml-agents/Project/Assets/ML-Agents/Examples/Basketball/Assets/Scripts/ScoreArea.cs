using UnityEngine;

public class ScoreArea : MonoBehaviour
{
    public BasketballAgent Agent;

    void OnTriggerEnter(Collider otherCollider)
    {
        if (otherCollider.CompareTag("ball")) {
            Agent.Scored();
        }
    }
}
