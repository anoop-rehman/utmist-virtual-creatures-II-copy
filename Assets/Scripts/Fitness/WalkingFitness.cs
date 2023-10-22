using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingFitness : Fitness
{
    //public float pushThreshold = 2f;
    public float pushThreshold = 2f;
    //public float pushPenaltyDiscount = 0.9f;
    public float pushPenaltyDiscount = 0.2f;
    public float stillnessPenalty = 0.01f;
    Vector3 currCom;
    Vector3 prevCom;
    float distance, prevSpeed;
    float currSpeed = 0f;
    Creature creature;

    // Start is called before the first frame update
    void Start()
    {
        //creature = myEnvironment.currentCreature;
        //currCom = creature.GetCentreOfMass();
        Reset();
    }

    // Update is called once per frame
    void Update()
    {

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

        reward += currSpeed;

        // Continuing movement is rewarded over that from a single initial push, by giving the velocities during the final phase of the test period a stronger relative weight in the total fitness value
        // We do not implement this because I am lazy
        // Initial push <=> curr speed would be way slower than prev speed => apply discount to reward

     //   if (pushThreshold * currSpeed < prevSpeed)
	    //{
            
		   // reward *= pushPenaltyDiscount;
	    //}

        if (currSpeed < 0.01f) // If the creature has stopped moving, penalize it
        {

            reward *= stillnessPenalty;
            //Debug.Log("penalized!");

        }

        return reward;
    }

    public override void Reset()
    {
        //throw new System.NotImplementedException();
        creature = myEnvironment.currentCreature;
        if (creature == null) return;
        currCom = creature.GetCentreOfMass();
    }
}
