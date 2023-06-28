using UnityEngine;

public class Floor : MonoBehaviour
{
    public BasketballAgent Agent;

    void OnCollisionEnter(Collision otherCollider)
    {
        if (otherCollider.collider.gameObject.tag == "ball") {
            Agent.EndEpisode();
            Debug.Log("Touched Floor");
        }
    }
}
