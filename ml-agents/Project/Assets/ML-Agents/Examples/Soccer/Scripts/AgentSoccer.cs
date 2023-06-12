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
    public float fieldReward;

    // Add a new field to hold the penalty for going upfield
    public float fieldPenalty;
    
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


    public GameObject enemyGoal;

    [HideInInspector]
    public Rigidbody agentRb;
    public GameObject field;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;
    private bool inField = true;
    SoccerEnvController envController;

    private AgentSoccer lastTouched;
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
            m_LateralSpeed = 0.75f;
            m_ForwardSpeed = 0.5f;
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
                m_KickPower = 2.5f;
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
    //Sirve para saber si esta dentro de su area
    void OnTriggerEnter(Collider other)
    {   
         if (other.gameObject == this.field)
         {
            inField = true;
         }
    }
    //Sirve para saber si esta fuera de su area
    void OnTriggerExit(Collider other)
    {   
         if (other.gameObject == this.field)
         {
            inField = false;
         }
    }
    //Aplica la recompensa por estar dentro o fuera de su area
    void fieldRecompense(){
        if(position == Position.Goalie || position == Position.Defender){
             if (!inField)
            {
                // Aplica una penalización por salir del campo
                AddReward(fieldPenalty);
                
                //Debug.Log("PENALIZACION salir del area " + position + " " + team);
            }
            else{
                AddReward(fieldReward);
                //Debug.Log("Recompensa no salir del area");
            }
        }
        
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float posicionNum;
        if((position == Position.Midfielder)){
                posicionNum = 2;
        }
        if(position == Position.Striker){
                posicionNum = 3;
        }
        if(position == Position.Defender){
                posicionNum = 1;
        }
        else{
                posicionNum=0;
        }
        lastTouched = envController.ball.GetComponent<SoccerBallController>().lastTouch;
        sensor.AddObservation(inField); //Los jugadores conocen si estan o no en su campo
        sensor.AddObservation(posicionNum);    //Los jugadores conocen su rol en la cancha
        sensor.AddObservation(stepsWithoutTouchingBall);    // Los jugadores conocen cuantos pasos llevan sin tocar el balon
        // Direction is facing (1 Vector3 = 3 values)
        sensor.AddObservation(transform.forward);       //Los jugadores conocen cual es su parte "Delantera" (Donde le pegan con mas fuerza)
        sensor.AddObservation(hasTouchedBall);
        sensor.AddObservation(lastTouched);
        sensor.AddObservation(envController.isNearToBall(this));
        // 1 + 1 + 1 +  3  + 1 + 1 + 1  = 9 total values
    }


    public void makeGoal(){
        if((position != Position.Goalie)){ //Los arqueros deberian no hacer goles, su roll es quedarse en el arco
                AddReward(0.8f); 
                //Debug.Log("RECOMPENSA hacer gol");
        }
        if((position == Position.Striker)){ //Los delanteros tienen un extra por hacer un gol
                AddReward(0.2f); 
                //Debug.Log("RECOMPENSA bonus gol delantero");
        }
    }
    public void makeOwnGoal(){
        if((position != Position.Goalie)){
                AddReward(-1f);
                //Debug.Log("PENALIZACION gol en contra");
        }
        else{
            AddReward(-0.25f); //Un arquero no deberia ser tan penalizado por hacerlo en contra, se supone que quiso atajar
            //Debug.Log("PENALIZACION gol en contra arquero");
        }
    }

    public void nearToBall(){
        if(position != Position.Goalie){

            if(envController.isNearToBall(this)){ // El arquero no necesariamente tiene que estar cerca de la pelota, los defensores tampoco

                if((position == Position.Midfielder)){
                    AddReward(0.20f);
                }
                if(position == Position.Striker){
                    AddReward(0.15f);
                }
                else{
                    AddReward(0.05f);
                }
                //Debug.Log("RECOMPENSA estar cerca del balon");
            }
            else{
                if(position == Position.Defender){
                    AddReward(-0.055f);
                }
                else{
                   AddReward(-0.10f);
                }
                //Debug.Log("PENALIZACION estar lejos del balon");
            }
        }
    }

    public void touchingRecompense(){
        if(hasTouchedBall){
            hasTouchedBall = false;
            stepsWithoutTouchingBall = 0;
            return;
        }
        else{
            stepsWithoutTouchingBall ++;
        }
        if(position != Position.Goalie){
            if(stepsWithoutTouchingBall > 1750f){
                AddReward(-0.005f);
            }
        }

    }


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {   
        //Los arqueros no tiene bonus existencial, su bonus se basa en no salir del area
        if (position == Position.Goalie)
        {
            AddReward(-m_Existential);
        }
        else if (position == Position.Defender)
        {
            // Existential bonus for Defenders.
            AddReward(-m_Existential);
        }
        else if (position == Position.Midfielder)
        {
            // Existential bonus for Midfielders.
            AddReward(-m_Existential);

        }
        else if (position == Position.Striker)
        {
            // Existential penalty for Strikers
            AddReward(-m_Existential);
        }
        
        fieldRecompense();
        touchingRecompense();
        nearToBall();
        MoveAgent(actionBuffers.DiscreteActions);


    }


    public void kickToEnemyGoal(Vector3 dir){
        if (Vector3.Dot(dir, enemyGoal.transform.position - transform.position) > 0.9f)
    {
        AddReward(0.1f); // Recompensa por patear hacia el arco rival
        //Debug.Log("RECOMPENSA patear arco rival");
        if(position == Position.Striker){
            AddReward(0.30f);
        }
    }
    else
    {
        AddReward(-0.05f); // Penalización por patear en dirección opuesta al arco rival
        //Debug.Log("PENALIZACION patear a propio arco");
    }
    }

    void OnCollisionEnter(Collision c)
    {
        if((c.gameObject.CompareTag("blueAgent")) && (team == Team.Blue) || (c.gameObject.CompareTag("purpleAgent")) && (team == Team.Purple)){
            AddReward(-0.4f ); // Que no se choquen entre compañeros
            //Debug.Log("PENALIZACION chocar con compañero");
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            var force = 30000f;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }

        if(c.gameObject.CompareTag("wall")){
            AddReward(-0.75f ); // Que no hagan tiempo contra la pared
            //Debug.Log("PENALIZACION chocar con pared");
        }
        
        if(this.position == Position.Defender || this.position == Position.Midfielder){
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            var force = 70000f;
            
            if((c.gameObject.CompareTag("blueAgent") && team == Team.Purple) || 
                (c.gameObject.CompareTag("purpleAgent") && team == Team.Blue ) ){
                    AddReward(0.05f); 
                    c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
                    c.gameObject.GetComponent<AgentSoccer>().AddReward(-0.55f);

            }
        }

        if (c.gameObject.CompareTag("ball"))
        {
            var force = k_Power * m_KickPower;
            hasTouchedBall = true;
            if(lastTouched)
            {
                if(position == Position.Goalie)
                {
                force = k_Power;
                    if(!inField)
                    {
                         AddReward(-1f); //Si el arquero toca el balon fuera de su area, se lo castiga 
                    }
                    else{
                        if(this.team != lastTouched.team)
                        {
                            AddReward(0.8f);  // Si el arquero toca el balon cuando la habia tocado alguien del otro equipo se lo recompensa
                        }
                        else{
                            AddReward(0.2f);
                        }
                    }
                    
                }
            
                if(position == Position.Defender)
                {
                    if(this.team != lastTouched.team )
                    {
                        AddReward(0.5f);// Si el defensor toca el balon cuando la habia tocado alguien del otro equipo se lo recompensa
                    }
                }

                if(position == Position.Midfielder)
                {
                    if(this == lastTouched )
                    {
                        AddReward(0.5f);// Si el mediocampista Toca mas de una vez seguida la pelota se lo premia
                    }
                }
                if(lastTouched.team == this.team && (this != lastTouched)){
                    lastTouched.AddReward(0.05f); // Recompensa por hacer un pase
                    AddReward(0.05f);
                }
            }
            
            
            if(!lastTouched && position != Position.Goalie){
                AddReward(0.3f);
                //Debug.Log("RECOMPENSA first touch");
            }
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            if(position != Position.Goalie)
            {
                kickToEnemyGoal(dir);
                AddReward(0.50f); //Recompensa por tomar contacto con el balon
                //Debug.Log("RECOMPENSA tocar balon");
            }
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    public override void OnEpisodeBegin()
    {
        stepsWithoutTouchingBall = 0;
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 1);

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

}
