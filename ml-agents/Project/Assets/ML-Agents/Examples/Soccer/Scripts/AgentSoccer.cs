using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Extensions;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;
using System.Collections.Generic;
using System.Linq;
public enum Team
{
    Blue = 0,
    Purple = 1
}

public class AgentSoccer : Agent
{
    float m_KickPower;
    // The coefficient for the reward for colliding with a ball. Set using curriculum.
    float m_BallTouch;

    [HideInInspector]
    public Team team;

    public enum Position
    {
        Striker,
        Goalie,
        Generic,
        Defender
    }
    public GameObject field;

    public Position position;
    // Define the visual sensor component and its parameters
    public RayPerceptionSensorComponent3D rayPerceptionSensor;

    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    // Define the team tags
    // Team 1 tags
    public List<string> blueTags = new List<string> { "ball", "blueGoal", "purpleGoal", "wall", "blueAgent", "purpleAgent" };
    // Team 2 tags
    public List<string> purpleTags = new List<string> { "ball", "purpleGoal", "blueGoal", "wall", "purpleAgent", "blueAgent" };

    private Dictionary<string, int> tagToIntMap = new Dictionary<string, int>() {
        { "ball", 0 },
        { "blueGoal", 1 },
        { "purpleGoal", 2 },
        { "wall", 3 },
        { "blueAgent", 4 },
        { "purpleAgent", 5 }
    };

    const float k_Power = 2000f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();
        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            initialPos = new Vector3(transform.position.x, .5f, transform.position.z);
            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;
            initialPos = new Vector3(transform.position.x, .5f, transform.position.z);
            rotSign = -1f;
        }
        if (position == Position.Goalie)
        {
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
        }
        else if (position == Position.Striker)
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
        }
        else
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
        }
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        // Get the RayPerceptionSensor component
        rayPerceptionSensor = GetComponent<RayPerceptionSensorComponent3D>();

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                m_KickPower = 1f;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    void OnTriggerEnter(Collider other)
    {  
        if (other.gameObject == field.gameObject)
        {
           AddReward(m_Existential);
        }
        else
        {
           AddReward(-m_Existential);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {   
        if (position == Position.Defender)
        {
            // Existential bonus for Defenders.
            AddReward(m_Existential);
        }

        if (position == Position.Goalie)
        {
            // Existential bonus for Goalies.
            AddReward(m_Existential);

        }
        else if (position == Position.Striker)
        {
            // Existential penalty for Strikers
            AddReward(-m_Existential);
        }

        MoveAgent(actionBuffers.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.Reset();

        rayPerceptionSensor.DetectableTags = (team == Team.Blue) ? blueTags : purpleTags;

        int numRays = rayPerceptionSensor.RaysPerDirection * 2 + 1; // Assuming equal rays on both sides
        float startAngle = -rayPerceptionSensor.MaxRayDegrees;
        float angleIncrement = (rayPerceptionSensor.MaxRayDegrees * 2) / (numRays - 1);

        for (int i = 0; i < numRays; i++)
        {
            float rayAngle = startAngle + (i * angleIncrement);
            Vector3 rayDirection = Quaternion.Euler(0f, rayAngle, 0f) * transform.forward;
            float rayDistance = rayPerceptionSensor.RayLength;

            RaycastHit hit;
            bool hasHit = Physics.Raycast(rayPerceptionSensor.transform.position, rayDirection, out hit, rayDistance);

            if (hasHit)
            {
                float hitDistance = hit.distance;
                string hitTag = hit.collider.tag;
                Vector3 hitNormal = hit.normal;


                sensor.AddObservation(hitDistance);
                int tagIndex;
                if (tagToIntMap.TryGetValue(hitTag, out tagIndex))
                {
                    // Key exists in the dictionary, add the observation
                    sensor.AddObservation(tagIndex);
                }
                else
                {
                    // Key does not exist, add a placeholder value
                    sensor.AddObservation(-1);
                }
                
                sensor.AddObservation(hitNormal.x);
                sensor.AddObservation(hitNormal.y);
                sensor.AddObservation(hitNormal.z);
            }
            else
            {
                sensor.AddObservation(rayDistance); // No hit distance
                sensor.AddObservation(-1); // No tag
                sensor.AddObservation(0f); // No hit normal x
                sensor.AddObservation(0f); // No hit normal y
                sensor.AddObservation(0f); // No hit normal z
            }
        }

        // Collect visual observations using the RayPerceptionSensor
        int positionIndex = (int) position;
        sensor.AddOneHotObservation(positionIndex, 4);
        // Add the position of the field to the observations
        sensor.AddObservation(field.transform.position);
    }

    /// <summary>
    /// Used to provide a "kick" to the ball.
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower;
        if (position == Position.Goalie)
        {
            force = k_Power;
        }
        if (c.gameObject.CompareTag("ball"))
        {
            AddReward(.2f * m_BallTouch);
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }

}
