using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BasketballAgent : Agent
{
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    [Tooltip("Shooting force multiplier")] public float forceMultiplier = 2000;
    [Tooltip("Velocity of agent movement")] public float aimMultiplier = 120;

    Rigidbody rBody; // Declaring reference to agent Rigidbody for physics
    Rigidbody ballRBody; // Declaring reference to ball Rigidbody for physics
    BasketballSettings m_BasketballSettings;

    public GameObject ball;
    public Transform scoreArea; // The target, in this case the Basketball Hoop

    private bool holdingBall = true;
    private int m_ResetTimer;
    private Quaternion originalAgentRotationValue;
    private Quaternion originalBallRotationValue;

    void Start()
    {
        m_BasketballSettings = FindObjectOfType<BasketballSettings>();
        rBody = GetComponent<Rigidbody>();
        ballRBody = ball.GetComponent<Rigidbody>();
        originalBallRotationValue = ball.transform.rotation;
        originalAgentRotationValue = this.transform.rotation;
        GrabBall();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions, the force and angle of the shot, 11 observations in total
        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(scoreArea.localPosition);
        sensor.AddObservation(Mathf.Clamp(shootingForce, 0.45f, 1f));
        sensor.AddObservation(angle);
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0 && !(HasBall()))
        {
            Debug.Log("Episode Timed Out");
            EndEpisode();
        }
    }

    public override void OnEpisodeBegin()
    {
        // Add to episode count and reset timer and shooting force
        m_BasketballSettings.totalEpisodes += 1;
        m_ResetTimer = 0;
        shootingForce = 0f;

        // Reset agent and ball physics
        this.GrabBall();
        this.ResetAgentPhysics();
        ballRBody.angularVelocity = Vector3.zero;
        ballRBody.velocity = Vector3.zero;
        ball.transform.rotation = originalBallRotationValue;

        // Move both to a new random location
        this.ChangeAgentPosition();
        ball.transform.localPosition = this.transform.localPosition + this.transform.forward + this.transform.up;
        ball.transform.rotation = this.transform.rotation;
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
        holdingBall = false;
        ball.GetComponent<Rigidbody>().useGravity = true; // We need the ball to be afected by gravity once we shoot
        ball.GetComponent<Rigidbody>().AddForce(ball.transform.forward * force);
        ball.GetComponentInChildren<TrailRenderer>().emitting = true;
    }

    public void GrabBall()
    {
        // We reset the ball momentum
        ballRBody.angularVelocity = Vector3.zero;
        ballRBody.velocity = Vector3.zero;
        holdingBall = true;
        ball.GetComponent<Rigidbody>().useGravity = false; // When grabbing the ball we don't want it to be afected by gravity
        ball.GetComponentInChildren<TrailRenderer>().emitting = false;
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
                    rotateBallDir = transform.right * -1f;
                    angle = Vector3.Angle(ball.transform.forward, this.transform.forward);
                    Debug.Log("Angle:" + angle);
                    break;
                case 2:
                    rotateBallDir = transform.right * 1f;
                    angle = Vector3.Angle(ball.transform.forward, this.transform.forward);
                    Debug.Log("Angle:" + angle);
                    break;
            }

            switch (shootingPower)
            {
                case 1:
                    if (shootingForce < 0.45f) {
                        shootingForce = 0.45f;
                    }
                    shootingForce = shootingForce + 0.01f;
                    if (shootingForce > shootingForceMax) {
                        shootingForce = shootingForceMax;
                    }
                    Debug.Log("Shooting Force: " + shootingForce);
                    break;
                case 2:
                    shootingForce = shootingForce - 0.01f;
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
                        shootingForce = 0.45f;
                    }
                    break;
                case 2:
                    // Do nothing
                    break;
            }
        }

        // Fell off platform or touch ground
        if (ball.transform.localPosition.y < 0.1)
        {
            EndEpisode();
        }

        // Perform requested aiming
        ball.transform.RotateAround(this.transform.localPosition, rotateBallDir, Time.deltaTime * 100f);
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
