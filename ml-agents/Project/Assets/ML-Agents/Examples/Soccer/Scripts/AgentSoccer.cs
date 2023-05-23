using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Extensions;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;

public enum Team
{
    Blue = 0,
    Purple = 1
}

public class AgentSoccer : Agent
{
    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    // Add a new field to hold the reward for staying in the field
    public float fieldReward = 0.01f;

    // Add a new field to hold the penalty for going upfield
    public float fieldPenalty = -0.01f;
    
    public enum Position
    {
        Striker,
        Goalie,
        Generic,
        Defender,
        Midfielder
    }

    [HideInInspector]
    public Team team;
    float m_KickPower;
    // The coefficient for the reward for colliding with a ball. Set using curriculum.
    float m_BallTouch;
    
    [Observable]
    public Position position;

    const float k_Power = 2000f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    private int stepsWithoutTouchingBall; // Variable para realizar un seguimiento de los pasos sin tocar el balón
    private bool hasTouchedBall = false;




    [HideInInspector]
    public Rigidbody agentRb;
    public GameObject field;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;
    private bool inField;
    SoccerEnvController envController;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        envController = GetComponentInParent<SoccerEnvController>();
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
            m_ForwardSpeed = 1.2f;
        }
        else if (position == Position.Midfielder)
        {
            m_LateralSpeed = 0.4f;
            m_ForwardSpeed = 1.5f;
        }
        else
        {
            m_LateralSpeed = 0.4f;
            m_ForwardSpeed = 1.0f;
        }
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

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
                m_KickPower = 2f;
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

    void OnTriggerStay(Collider other)
    {   
         if (other.gameObject == field.gameObject)
         {
            inField = true;
         }
    }
    void OnTriggerExit(Collider other)
    {   
         if (other.gameObject == field.gameObject)
         {
            inField = false;
         }
    }
    
    void fieldRecompense(){
         if (inField)
        {
            // Aplica una recompensa por estar dentro del campo
            AddReward(fieldReward);
        }
        else
        {
            // Aplica una penalización por salir del campo
            AddReward(fieldPenalty);
        }
    }

    public void nearToBall(){
        if(envController.isNearToBall(this)){
            AddReward(0.1f);
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
        if (position == Position.Midfielder)
        {
            // Existential bonus for Midfielders.
            AddReward(-m_Existential/2);

        }
        else if (position == Position.Striker)
        {
            // Existential penalty for Strikers
            AddReward(-m_Existential);
        }
        /*
        fieldRecompense();*/

        if (!hasTouchedBall)
        {
            stepsWithoutTouchingBall++;
        }
        else
        {
            stepsWithoutTouchingBall = 0;
            hasTouchedBall = false;
        }

        // Penaliza si el agente pasa demasiados pasos sin tocar el balón
        if (stepsWithoutTouchingBall >= 7000) // Ajusta el número de pasos sin tocar el balón
        {
            AddReward(-0.01f); // Aplica una penalización por falta de interacción con el balón
        }
        nearToBall();
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

    /// <summary>
    /// Used to provide a "kick" to the ball.
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower;
        if (c.gameObject.CompareTag("ball"))
        {
            hasTouchedBall = true;
            var lastTouched = c.gameObject.GetComponent<SoccerBallController>().lastTouch;
            if(lastTouched)
            {
                if(position == Position.Goalie)
                {
                force = k_Power;
                if(team != lastTouched.team )
                {
                    AddReward(2f * m_BallTouch);
                    Debug.Log("Atajada");  // Si el arquero toca el balon cuando la habia tocado alguien del otro equipo se lo recompensa
                }
                }
                if(position == Position.Defender)
                {
                if(team != lastTouched.team )
                {
                    AddReward(.5f * m_BallTouch);
                    Debug.Log("Balon Recuperado");  // Si el defensor toca el balon cuando la habia tocado alguien del otro equipo se lo recompensa
                }
                
                }
                if(position == Position.Midfielder)
                {
                if(this == lastTouched )
                {
                    AddReward(1f * m_BallTouch);
                    Debug.Log("Regate");  // Si el Toca mas de una vez seguida la pelota se lo premia
                }
                }
            }
            else{
                AddReward(3f); //Recompensa extra por tocar el balon antes que nadie
            }
            //AddReward(1f * m_BallTouch);
            AddReward(2f);
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
            c.gameObject.GetComponent<SoccerBallController>().touchedBy(this);
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 1);
        stepsWithoutTouchingBall = 0;
    }

}
