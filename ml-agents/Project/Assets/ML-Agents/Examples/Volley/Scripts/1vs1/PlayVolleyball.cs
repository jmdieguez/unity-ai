using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class PlayVolleyball : Agent
{
    [SerializeField] private GameObject field;
    bool ballInMyField;

    [SerializeField] private Transform opponentTransform;
    [SerializeField] private GameObject ball;
    [SerializeField] private Transform netTransform;

    [SerializeField] private  Counter oppCounter;
    bool isJumping = false;
    private Rigidbody rb;

    private Vector3 initPosition;

    private int ballHitsCounter;
    private int collisionStayCounter;
    private bool firstEpCollision;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start() {
        initPosition = transform.localPosition;
        ballInMyField = field.tag == "field1" ? ball.transform.localPosition.x < 0f : ball.transform.localPosition.x > 0f;
        //lo invierto para que empiece bien en OnEpisodeBegin()
        ballInMyField = !ballInMyField;
    }


    public override void OnEpisodeBegin(){
        transform.localPosition = initPosition;
        isJumping = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float randPosX = Random.Range(-4f,0f);
        float randPosZ = Random.Range(-2f,2f);
        transform.Translate(new Vector3(randPosX,0f,randPosZ));

        // transform.localPosition = initPosition;

        //al comienzo del episodio cambio al valor opuesto (si el punto del ep anterior fue en mi cancha ahora la pelota aparece en el otro lado)
        ballInMyField = !ballInMyField;
        ballHitsCounter = 0;
        collisionStayCounter = 0;
        firstEpCollision = true;
    }

    public override void CollectObservations(VectorSensor sensor){
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(ball.transform.localPosition);
        sensor.AddObservation(ball.GetComponent<Rigidbody>().velocity);
        sensor.AddObservation(opponentTransform.localPosition);
        sensor.AddObservation(netTransform.localPosition.y+(netTransform.localScale.y/2));
        sensor.AddObservation(Mathf.Abs(netTransform.localPosition.x-transform.localPosition.x));

    }

    public override void OnActionReceived(ActionBuffers actions){
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        float jump = actions.ContinuousActions[2];


        float moveSpeed = 8f;
        float jumpForce = 6.5f;

        Vector3 movement = new Vector3(moveX, 0f, moveZ) * moveSpeed * Time.fixedDeltaTime;
        transform.Translate(movement);


        // transform.localPosition += new Vector3(moveX,0,moveZ) * Time.deltaTime * moveSpeed;
        // if (jump>0){print("Salto - " + isJumping + gameObject.name);}
        if (!isJumping && jump > 0){
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = true;
        }

        // Ball went over the net -> Reward 15
        bool prevBallField = ballInMyField;
        ballInMyField = field.tag == "field1" ? ball.transform.localPosition.x < 0f : ball.transform.localPosition.x > 0f;
        if ((ballInMyField != prevBallField) && !ballInMyField){
            // print("Pasa red " + gameObject.name);
            ballHitsCounter = 0;
            AddReward(40f);
        }

        // Distance to ball -> Reward [-2,2]
        if (ballInMyField){
            float MAX_DIST = 22f;
            float distanceToBall = Vector3.Distance(transform.localPosition, ball.transform.localPosition);
            float reward = 2*((-distanceToBall/MAX_DIST)+1)-1;
            // print("Distancia pelota " + distanceToBall + "rw: " + reward);
            AddReward(reward);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut){
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        // continuousActions[0] = Input.GetAxisRaw("Horizontal");
        // continuousActions[1] = Input.GetAxisRaw("Vertical");

        continuousActions[1] = -Input.GetAxisRaw("Horizontal");
        continuousActions[0] = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.Space))
        {
            continuousActions[2] = 1;
        }else{
            continuousActions[2] = 0;
        }

    }

    private void OnCollisionEnter(Collision other){
        string tag = other.gameObject.tag;
        if (tag == "field1" || tag == "field2"){
            // if (gameObject.name == "Player2"){print("Colision piso Player2");}
            if (!firstEpCollision){
                isJumping = false;
            }
            firstEpCollision = false;
            // if (gameObject.name == "Player1"){print("Colision isJumping: " + isJumping);}
        }

        else if (tag == "ball"){
            int hitBallRw = 1;
            // print("Golpeo pelota " + gameObject.name);
            AddReward(hitBallRw);
            ballHitsCounter += 1;
            //si toca mas 5 veces la pelota antes de pasarla, pierde punto (3 veces es poco)
            if (ballHitsCounter> 5){
                LossPoint();
                EndEpisode();
            }
        }

        else if (tag == "wall" || tag == "net"){
            float touchWallRw = -5f;
            // print("Toco pared");
            AddReward(touchWallRw);
        }
    }

    private void OnCollisionStay(Collision other) {
        if (other.gameObject.tag == "ball"){
            collisionStayCounter+=1;
            // si se queda reteniendo la pelota, pierde punto
            if (collisionStayCounter > 15){
                LossPoint();
                EndEpisode();
            }
        }
    }

    private void OnCollisionExit(Collision other){
        if (other.gameObject.tag == "ball"){
            collisionStayCounter=0;
        }
    }

    private void LossPoint(){
        oppCounter.Point();
        AddReward(-100);
        BallCollision ballCollision = ball.GetComponent<BallCollision>();
        ballCollision.setInitPosition(field.tag);
    }

}
