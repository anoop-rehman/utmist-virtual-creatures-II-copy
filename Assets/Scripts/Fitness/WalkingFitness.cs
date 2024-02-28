using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingFitness : Fitness
{
    public float startTime;
    public float endTime;
    public float pushThreshold = 2f;
    //public float pushPenaltyDiscount = 0.9f;
    public float pushPenaltyDiscount = 0.2f;
    public float stillnessPenalty = 0.01f;
    Vector3 currCom;
    Vector3 prevCom;
    float distance, prevSpeed;
    float currSpeed = 0f;
    public Creature creature;
    // Keep track of old position of the agent.
    // If its current position and old position is the same => It isn't moving so don't reward it for staying still
    public Vector3 oldCOM = Vector3.zero;

    float EPSILON = 0.00001f;
    //--------------------
    ////// Lightsensor Spawn code moved to Environment.cs where we spawn individual creature.
    ////// Idea is to spawn a lightsource associated with each creature so its easy to delete and create new one every generation

    //public GameObject lightSource; // Assign this in the inspector
    //-------------

        List<Vector3> localTargetPositions = new List<Vector3>
    {
    new Vector3(0f, -5.5f, 8f),
    new Vector3(1.7f, -5.5f, 5f + 6f),
    new Vector3(-2.02f, -5.5f, 5f + 9.48f),
    new Vector3(2.25f, -5.5f, 5f + 15.73f),
    };
    List<Vector3> worldTargetPositions = new List<Vector3>();
    int currTargetIndex = 0;

    Vector3 lightsourceWorldPos;

    private void Awake()
    {
        foreach (Vector3 localTargetPosition in localTargetPositions)
        {
            worldTargetPositions.Add(localTargetPosition + transform.position);
        }
        lightsourceWorldPos = worldTargetPositions[currTargetIndex];

    }

    // Start is called before the first frame update
    void Start()
    {
        creature = myEnvironment.currentCreature;
        //lightSource = Resources.Load<GameObject>("Prefabs/Light_Source");
        //creature = myEnvironment.currentCreature;
        //currCom = creature.GetCentreOfMass();

        Debug.Log("creature is null = " + (creature == null));
        if (creature == null)
            return;


        Reset();

    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void SetCreatureObject(Creature creature)
    {
        this.creature = creature;
    }

    public override float UpdateFrameReward()
    {
        float reward = 0f;
	
	    prevCom = currCom;
       	currCom = creature.GetCentreOfMass();

        if (firstFrame)
        {
            firstFrame = false;
            return 0f;
        }
        float distanceMoved = Vector3.Distance(oldCOM, currCom);
        if (distanceMoved < 0.5f)
        {
            // return 0f;
            return reward;
        }

        prevSpeed = currSpeed;
        //distance = Vector3.Distance(currCom,prevCom);
        distance = Vector3.Dot(currCom - prevCom, Vector3.forward);

        currSpeed = distance/Time.deltaTime;

        if (float.IsNaN(distance))
        {
            Debug.Log("sus");
            Debug.Log(currCom);
            Debug.Log(prevCom);
        }

        if (float.IsNaN(currSpeed))
        {
            Debug.Log("sus!!!");
        }
        // Lightsource is the goal for the target as the creature should aim to reach it
        string tagName = "Lightsource";
        GameObject lightsource = null;
        foreach (Transform childTransform in this.gameObject.transform)
        {
            GameObject childObj = childTransform.gameObject;
            if (childObj.CompareTag(tagName))
            {
                lightsource = childObj;
            }
        }
        if (lightsource == null)
        { 
            return 0f;
        }

        //Vector3 lightsourceWorldPos = lightsource.transform.TransformVector(lightsource.transform.position);
        Vector2 distance_away = (new Vector2(currCom.x, currCom.z)) - (new Vector2(lightsourceWorldPos.x, lightsourceWorldPos.z));
        if (distance_away.magnitude <= 3f)
        //if (distance_away.magnitude <= 1.5f)
        {
            Debug.Log("creature position = " + creature.transform.position);
            Debug.Log("lightsourceWorldPos = " + lightsourceWorldPos);
            Debug.Log("distance_away = " + distance_away);


            currTargetIndex += 1;
            if (currTargetIndex >= worldTargetPositions.Count) // Check if the index exceeds the bounds
            {
                currTargetIndex = 0; // Reset index to loop through the targets again
            }
            Debug.Log(creature.cg.name + " REACHED TARGET!!!");
            Vector3 nextLightsourcePosition = worldTargetPositions[currTargetIndex];
            // You may also need to update the lightsourceWorldPos here to reflect the new target position
            lightsourceWorldPos = nextLightsourcePosition;


            Vector3 newLightsourceLoc = myEnvironment.transform.position;
            newLightsourceLoc = lightsourceWorldPos;
            lightsource.transform.position = newLightsourceLoc;



            reward += 5f;
        }
        reward += 1 / (EPSILON + Mathf.Pow((distance_away).magnitude, 2));


        oldCOM = currCom;
     

        return reward;
    }

    public override void Reset()
    {
        creature = myEnvironment.currentCreature;
        if (creature == null) return;
        currCom = creature.GetCentreOfMass();

  


        ////foreach (Vector3 worldTargetPosition in worldTargetPositions)
        ////{
        ////    // Create a lightsource at each designated posiiton for creatures during training
        ////    Instantiate(lightSource, worldTargetPosition, transform.rotation);
        ////}
        ////Debug.Log("spawned target at" + lightSource.transform.position);
    }
}

