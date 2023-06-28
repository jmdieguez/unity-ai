using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BasketballAgent : Agent
{
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 15000;
    [Tooltip("Shooting force multiplier")] public float forceMultiplier = 2000;
    [Tooltip("Velocity of agent movement")] public float aimMultiplier = 120;

    Rigidbody rBody; // Declaring reference to agent Rigidbody for physics
    Rigidbody ballRBody; // Declaring reference to basketball Rigidbody for physics
    BasketballSettings m_BasketballSettings;

    public GameObject basketball;
    public Transform scoreArea; // The target, in this case the Basketball Hoop
    [HideInInspector]
    public bool hoopTouched = false;
    private bool holdingBall = true;
    private int m_ResetTimer;
    private float distance;
    private Vector3 hoopPos = Vector3.zero;
    private Quaternion originalAgentRotationValue;
    private Quaternion originalBallRotationValue;

    public override void Initialize()
    {
        m_BasketballSettings = FindObjectOfType<BasketballSettings>();
        rBody = GetComponent<Rigidbody>();
        ballRBody = basketball.GetComponent<Rigidbody>();
        originalBallRotationValue = basketball.transform.rotation;
        originalAgentRotationValue = this.transform.rotation;
        hoopPos.Set(scoreArea.localPosition.x, 0.5f, scoreArea.localPosition.z);
        GrabBall();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Distance to target, the force and angle of the shot, and wether or not we have the ball 4 observations in total
        sensor.AddObservation(distance);
        sensor.AddObservation(Mathf.Clamp(shootingForce, 0.45f, 1f));
        sensor.AddObservation(angle);
        sensor.AddObservation(holdingBall);
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            Debug.Log("Episode Timed Out");
            EndEpisode();
        }

        // Fell off platform
        if (basketball.transform.localPosition.y < -0.5f)
        {
            Debug.Log("Ball fell off the platform");
            AddReward(-0.25f);
            EndEpisode();
        }

        // We request decision only when we have the ball
        if (HasBall() && ((m_ResetTimer % 5) == 0)) {
            RequestDecision();
        }
    }

    public override void OnEpisodeBegin()
    {
        m_ResetTimer = 0;
        ResetScene();
        Debug.Log("Distance to target: " + distance);
    }

    private void ResetAgentPhysics()
    {
        // Reset agents momentum and rotation
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;
        this.transform.rotation = originalAgentRotationValue;
    }

    private void ChangeAgentPosition()
    {
        // Move agent to a new random spot and automatically look at the Hoop
        this.transform.localPosition = new Vector3(Random.value * 13 - 8, 0.5f, Random.value * 16 - 8);
        this.transform.LookAt(new Vector3(scoreArea.transform.position.x, 0.5f, scoreArea.transform.position.z));
    }

    private bool HasBall()
    {
        return holdingBall;
    }
    private void ShootBall(float force)
    {
        m_BasketballSettings.totalEpisodes += 1;
        holdingBall = false;
        basketball.GetComponent<Rigidbody>().useGravity = true; // We need the basketball to be afected by gravity once we shoot
        basketball.GetComponent<Rigidbody>().AddForce(basketball.transform.forward * force);
        basketball.GetComponentInChildren<TrailRenderer>().emitting = true;
    }

    public void GrabBall()
    {
        // We reset the basketball momentum
        ballRBody.angularVelocity = Vector3.zero;
        ballRBody.velocity = Vector3.zero;
        holdingBall = true;
        basketball.GetComponent<Rigidbody>().useGravity = false; // When grabbing the basketball we don't want it to be afected by gravity
        basketball.GetComponentInChildren<TrailRenderer>().emitting = false;
    }

    public void ResetScene()
    {
        basketball.GetComponentInChildren<TrailRenderer>().Clear();
        // Add to episode count and reset timer and shooting force
        shootingForce = 0.45f;
        this.hoopTouched = false;

        // Reset agent and basketball physics
        this.GrabBall();
        this.ResetAgentPhysics();
        ballRBody.angularVelocity = Vector3.zero;
        ballRBody.velocity = Vector3.zero;
        basketball.transform.rotation = originalBallRotationValue;

        // Move both to a new random location
        this.ChangeAgentPosition();
        basketball.transform.localPosition = this.transform.localPosition + this.transform.forward + this.transform.up;
        basketball.transform.rotation = this.transform.rotation;

        distance = Vector3.Distance(this.transform.localPosition, hoopPos);
    }


    private float shootingForce = 0.45f;
    private float shootingForceMax = 1.0f;
    private float angle = 0.0f;

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var rotateBallDir = Vector3.zero;

        var rotateBallAxis = actionBuffers.DiscreteActions[0];
        var shootingPower = actionBuffers.DiscreteActions[1];
        var shoot = actionBuffers.DiscreteActions[2];

        if (HasBall())
        {
            switch (rotateBallAxis)
            {
                case 1:
                    rotateBallDir = transform.right * -0.5f;
                    angle = Vector3.Angle(basketball.transform.forward, this.transform.forward);
                    Debug.Log("Angle:" + angle);
                    break;
                case 2:
                    rotateBallDir = transform.right * 0.5f;
                    angle = Vector3.Angle(basketball.transform.forward, this.transform.forward);
                    Debug.Log("Angle:" + angle);
                    break;
            }

            switch (shootingPower)
            {
                case 1:
                    if (shootingForce < 0.45f) {
                        shootingForce = 0.45f;
                    }
                    shootingForce = shootingForce + 0.005f;
                    if (shootingForce > shootingForceMax) {
                        shootingForce = shootingForceMax;
                    }
                    Debug.Log("Shooting Force: " + shootingForce);
                    break;
                case 2:
                    shootingForce = shootingForce - 0.005f;
                    if (shootingForce < 0.45f) {
                        shootingForce = 0.45f;
                    }
                    Debug.Log("Shooting Force: " + shootingForce);
                    break;
            }

            switch (shoot) {
                case 1:
                    if (HasBall()) {
                        // Shoot
                        ShootBall(shootingForce * forceMultiplier);
                    }
                    break;
                case 2:
                    // Do nothing
                    break;
            }
        }

        // Perform requested aiming
        basketball.transform.RotateAround(this.transform.localPosition, rotateBallDir, Time.deltaTime * 100f);
    }

    public void Scored()
    {
        Debug.Log("Scored!");
        m_BasketballSettings.totalScore += 1;
        SetReward(2.0f);
        EndEpisode();
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        this.MoveAgent(actionBuffers);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }

        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 2;
        }
        if (Input.GetKey(KeyCode.K))
        {
            discreteActionsOut[2] = 1;
        }
        if (!(Input.GetKey(KeyCode.K)))
        {
            discreteActionsOut[2] = 2;
        }
    }
}
