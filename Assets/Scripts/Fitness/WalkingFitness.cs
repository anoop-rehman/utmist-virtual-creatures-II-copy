using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingFitness : Fitness
{
    public float pushThreshold = 2f;
    //public float pushPenaltyDiscount = 0.9f;
    public float pushPenaltyDiscount = 0.2f;
    public float stillnessPenalty = 0.01f;
    Vector3 currCom;
    Vector3 prevCom;
    float distance, prevSpeed;
    float currSpeed = 0f;
    public Creature creature;

    // TODO: figure out what do with this lmao
    //public float creatureMass = 0f;
    // =====================================

    // Keep track of old position of the agent.
    // If its current position and old position is the same => It isn't moving so don't reward it for staying still
    public Vector3 oldCOM = Vector3.zero;
    //--------------------
    ////// Photosensor Spawn code moved to Environment.cs where we spawn individual creature.
    ////// Idea is to spawn a lightsource associated with each creature so its easy to delete and create new one every generation

    //public GameObject lightPhotosensor; // Assign this in the inspector
    //-------------
    List<Vector3> localTargetPositions = new List<Vector3>
    {
    new Vector3(0f, -5.5f, 8f),
    //new Vector3(1.7f, -5.5f, 5f + 6f),
    //new Vector3(-2.02f, -5.5f, 5f + 9.48f),
    //new Vector3(2.25f, -5.5f, 5f + 15.73f),
    };
    List<Vector3> worldTargetPositions = new List<Vector3>();

    // Start is called before the first frame update
    void Start()
    {
        creature = myEnvironment.currentCreature;
        //lightPhotosensor = Resources.Load<GameObject>("Prefabs/Light_Photosensor");
        //creature = myEnvironment.currentCreature;
        //currCom = creature.GetCentreOfMass();

        //Debug.Log("creature is null = " + (creature == null));
        //if (creature == null)
        //    return;
        //Reset();

        // TODO: figure out what do with this lmao
        //foreach (Segment seg in this.creature.segments)
        //{
        //    creatureMass += seg.myRigidbody.mass;
        //}
        // =====================================
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
        //Creature creature = myEnvironment.currentCreature;
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
            return 0f;
        }

        prevSpeed = currSpeed;
        //distance = Vector3.Distance(currCom,prevCom);
        distance = Vector3.Dot(currCom - prevCom, Vector3.forward);

        currSpeed = distance / Time.deltaTime;

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
        // Photosensor is the goal for the target as the creature should aim to reach it
        string tagName = "Photosensor";
        GameObject photosensor = null;
        foreach (Transform childTransform in this.gameObject.transform)
        {
            GameObject childObj = childTransform.gameObject;
            if (childObj.CompareTag(tagName))
            {
                photosensor = childObj;
            }
        }
        if (photosensor == null)
        {
            return 0f;
        }

        Vector3 photosensorWorldPos = photosensor.transform.TransformVector(photosensor.transform.position);
        Vector2 distance_away = (new Vector2(currCom.x, currCom.z)) - (new Vector2(photosensorWorldPos.x, photosensorWorldPos.z));
        if (distance_away.magnitude <= 3f)
        {
            // Generate new location for photosensor and return 5f for very good job baby
            float randomX, randomZ;
            if (UnityEngine.Random.Range(0, 1) == 0)
            {
                randomX = UnityEngine.Random.Range(-9f, -4.5f);
                randomZ = UnityEngine.Random.Range(4.5f, 9f);
            }
            else
            {
                randomX = UnityEngine.Random.Range(4.5f, 9f);
                randomZ = UnityEngine.Random.Range(-9f, -4.5f);
            }

            Vector3 newPhotosensorLoc = myEnvironment.transform.position;
            newPhotosensorLoc.x += randomX;
            newPhotosensorLoc.y -= 5f;  // To aovid light spawning 5 units above ground
            newPhotosensorLoc.z += randomZ;
            photosensor.transform.position = newPhotosensorLoc;

            return 5f;
        }
        //reward += currSpeed;

        // TODO: add the energy thing here
        float e1 = 0f;     // kinetic energy (mv^2)/2

        Debug.Log(creature);
        //Debug.Log("effectors list size: " + this.creature.effectors.Count);

        foreach (Neuron n in this.creature.effectors)
        {
            if (n != null)
            {
                HingeJoint ejoint = (HingeJoint)n.effectorJoint;
                JointMotor motor = ejoint.motor;
                //Debug.Log(motor.targetVelocity);
                e1 += Mathf.Abs(motor.targetVelocity);
            }
        }
        Debug.Log("e1 = " + e1);

        Debug.Log("===============================");

        //List<HingeJoint> motors = this.creature.actionMotors;
        //JointMotor motor = motors[0].motor;
        //float v = motor.targetVelocity;
        //Debug.Log("motor 0 targetvelocity: " + v);


        reward = 1 / (Mathf.Pow((distance_away).magnitude, 2));

        oldCOM = currCom;
        //Debug.Log("photosensorWorldPos = " + photosensorWorldPos + ", Center of Mass = " + currCom + ", reward = " + reward);

        // Continuing movement is rewarded over that from a single initial push, by giving the velocities during the final phase of the test period a stronger relative weight in the total fitness value
        // We do not implement this because I am lazy
        // Initial push <=> curr speed would be way slower than prev speed => apply discount to reward

        //   if (pushThreshold * currSpeed < prevSpeed)
        //{

        // reward *= pushPenaltyDiscount;
        //}

        //if (currSpeed < 0.01f) // If the creature has stopped moving, penalize it
        //{

        //    reward *= stillnessPenalty;
        //    //Debug.Log("penalized!");

        //}

        return reward;
    }

    public override void Reset()
    {
        creature = myEnvironment.currentCreature;
        if (creature == null) return;
        currCom = creature.GetCentreOfMass();
        //Instantiate(targetSphere, transform.position, transform.rotation);

        foreach (Vector3 localTargetPosition in localTargetPositions)
        {
            worldTargetPositions.Add(localTargetPosition + transform.position);
        }

        //foreach (Vector3 worldTargetPosition in worldTargetPositions)
        //{
        //    // Create a lightsource at each designated posiiton for creatures during training
        //    Instantiate(lightPhotosensor, worldTargetPosition, transform.rotation);
        //}
        //Debug.Log("spawned target at" + lightPhotosensor.transform.position);
    }
}
