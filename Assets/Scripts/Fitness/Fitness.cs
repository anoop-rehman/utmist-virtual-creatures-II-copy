using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Stores internal data about the Creature in order to compute a fitness
// Should not do anything more than return a frame reward. Will not hold a total reward.
public abstract class Fitness : MonoBehaviour
{
    public bool firstFrame = true;
    public Environment myEnvironment;
    public abstract float UpdateFrameReward();
    public abstract void SetCreatureObject(Creature creature);
    public virtual void Reset(){
        firstFrame = true;
    }
}
