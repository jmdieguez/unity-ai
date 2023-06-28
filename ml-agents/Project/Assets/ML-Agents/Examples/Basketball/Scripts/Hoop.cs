using UnityEngine;

public class Hoop : MonoBehaviour
{
    public BasketballAgent Agent;

    void OnCollisionEnter(Collision otherCollider)
    {
        if (Agent.hoopTouched == true)
        {
            return;
        }
        
        // We turn off this reward at 500k episodes because it hurts in the long run
        if (Agent.CompletedEpisodes > 500000)
        {
            return;
        }

        if (otherCollider.collider.gameObject.tag == "ball" && otherCollider.collider.gameObject.transform.position.y > 4.75f) {
            Agent.AddReward(0.15f);
            Agent.hoopTouched = true;
            Debug.Log("Touched Hoop");
        }
    }
}