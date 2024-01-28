using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

public enum EnvArrangeType { LINEAR, PLANAR };
public enum GraphicsLevel { LOW, MEDIUM, HIGH };
public enum EnvCode { OCEAN, FLOOR };

[Serializable]
public abstract class EnvironmentSettings {
    public abstract EnvCode envCode { get; }
    public readonly static Dictionary<EnvCode, string> envString = new Dictionary<EnvCode, string>() { 
    { EnvCode.OCEAN, "OceanEnv" },
    { EnvCode.FLOOR, "FloorEnv" },
    };

    public abstract EnvArrangeType envArrangeType { get; }
    public GraphicsLevel graphicsLevel { get; set;  }

    public virtual float sizeX { get { return 5; } }
    public virtual float sizeZ { get { return 5; } }
    public virtual float maxTime { get { return 3; } }
    public static EnvironmentSettings GetDefault(EnvCode code){
        if (code == EnvCode.OCEAN) {
            return new OceanEnvSettings();
        }
        else if (code == EnvCode.FLOOR)
        {
            return new FloorEnvSettings();
        }
        return null;
    }
}

/// <summary>
/// Base class to control a single environment
/// </summary>
public abstract class Environment : MonoBehaviour
{

    [Header("Stats")]
    public float timePassed;
    public abstract EnvCode envCode { get; }
    public bool busy { get { return currentCreature != null; } }

    private bool updatedFrameReward;
    private float frameReward;
    private bool isDQ = false;
    private bool hasDoneKillCheck = false;
    private bool isStandalone; // true when just testing one
    private Vector3 lastCom;

    // References to other Components
    public Creature currentCreature;
    [SerializeField]
    protected Fitness fitness;
    [SerializeField]
    private TrainingManager tm;
    [SerializeField]
    private CreatureSpawner creatureSpawner;
    [SerializeField]
    private Transform spawnTransform;
    [SerializeField]
    private Transform creatureHolder;

    private EnvironmentSettings es;
    public List<TrainingAlgorithm> tas;

    // Photosensor Related Variables
    public GameObject lightPhotosensor;
    private List<Vector3> photosensorSpawnLocations = new List<Vector3>
    {
    //new Vector3(0f, -5.5f, 8f),
    //new Vector3(1.7f, -5.5f, 5f + 6f),
    //new Vector3(-2.02f, -5.5f, 5f + 9.48f),
    //new Vector3(2.25f, -5.5f, 5f + 15.73f),
    };

    public void Start()
    {
        tm = TrainingManager.instance;
        isStandalone = tm == null;
        if (isStandalone) {
            Setup(EnvironmentSettings.GetDefault(envCode));
        }
    }

    public virtual void Setup(EnvironmentSettings es)
    {
        this.es = es;
        fitness = GetComponent<Fitness>();
        //fitness.firstFrame = true;
        tm = TrainingManager.instance;
        creatureSpawner = CreatureSpawner.instance;
        hasDoneKillCheck = false;
        spawnTransform = transform.Find("SpawnTransform");
        creatureHolder = transform.Find("CreatureHolder");
    }

    public virtual void FixedUpdate()
    {
        if (!busy) return;
        if (tm == null) tm = TrainingManager.instance;

        timePassed += Time.fixedDeltaTime;
        bool isOutOfTime;
        try
        {
            isOutOfTime = timePassed >= es.maxTime && es.maxTime > 0;
        }
        catch (Exception)
        {
            if (isStandalone) {
                es = EnvironmentSettings.GetDefault(envCode);
            } else {
                es = tm.ts.envSettings;
            }

            isOutOfTime = timePassed >= es.maxTime && es.maxTime > 0;
        }
        Vector3 currentCom = currentCreature.GetCentreOfMass();
        bool isExtremelyFar = (transform.position - currentCom).sqrMagnitude >= 1000;
        if (lastCom == null) {
            lastCom = currentCom;
        }



        bool isTooFast = ((currentCom - lastCom).magnitude / Time.fixedDeltaTime) > 10f && timePassed > 0.2f;
        bool isNan = !float.IsNaN(currentCom.x) || !float.IsNaN(currentCom.y) || !float.IsNaN(currentCom.z);
        bool isDQActivate = isExtremelyFar || isTooFast;

        bool isTooSlow = false;
        if (fitness is JumpingFitness) {
            if (timePassed > 2f && !hasDoneKillCheck)
            {
                hasDoneKillCheck = true;
                isTooSlow = Mathf.Abs(currentCreature.totalReward) < 0.00005f;
            }
        } else if (fitness is SwimmingFitness) {
            if (timePassed > 0.8f && !hasDoneKillCheck)
            {
                hasDoneKillCheck = true;
                isTooSlow = Mathf.Abs(currentCreature.totalReward) < 0.000005f;
            }
        }
        
        if (isOutOfTime || isDQActivate || isTooSlow)
        {
            if (isDQActivate)
            {
                isDQ = true;
            }
            //m_BlueAgentGroup.GroupEpisodeInterrupted();
            //m_RedAgentGroup.GroupEpisodeInterrupted();

            //m_blueAgent.agent.EpisodeInterrupted();
            //m_redAgent.agent.EpisodeInterrupted();
            ResetEnv();
        }
        //Debug.Log(currentCom + " " + lastCom);
        lastCom = currentCom;
    }

    // Create a photosensor gameobject for the environment (i.e the creature since each creature part of 1 floorEnv object)
    public void SpawnPhotosensorObject(Vector3 spawnLocation)
    {
        lightPhotosensor = Resources.Load<GameObject>("Prefabs/Light_Photosensor");
        // 4th param transform makes it so the new gameobject created spawns as a child of the FloorEnv.
        // This way each creature effectively has its own photosensor.
        Instantiate(lightPhotosensor, spawnLocation, transform.rotation, transform);
    }
    // Spawn creature by passing transform params to Scene CreatureSpawner
    public virtual void StartEnv(CreatureGenotype cg)
    {
        if (busy)
        {
            ResetEnv();
        }
        // Destory existing photsensor gameobjects before creating a new one for new scene.
        // This solves the issue of multiple photosensor per agent when infact only 1 should ever exist
        //DestroyExistingPhotosensors();
        fitness.Reset();
        currentCreature = creatureSpawner.SpawnCreature(cg, spawnTransform.position, fitness);
        currentCreature.transform.parent = creatureHolder;

    }

    public void DestroyExistingPhotosensors()
    {
        GameObject[] existingPhotosensors = GameObject.FindGameObjectsWithTag("Photosensor");
        foreach (GameObject photosensor in existingPhotosensors)
        {
            if (null != photosensor)
            {
                Destroy(photosensor);
            }
        }
    }

    public virtual void ResetEnv()
    {
        if (tas != null){
            foreach (TrainingAlgorithm ta in tas)
            {
                //Debug.Log("Pinging training algorithm.");
                float totalReward = busy ? currentCreature.totalReward : 0;
                ta.ResetPing(this, totalReward, isDQ);
            }
        }

        tas = new List<TrainingAlgorithm>();

        if (busy)
        {
            creatureSpawner.ReleaseCreature(currentCreature);
            currentCreature = null;
        }

        isDQ = false;
        hasDoneKillCheck = false;
        timePassed = 0;
    }

    public void PingReset(TrainingAlgorithm ta){
        tas.Add(ta);
    }

    void OnDrawGizmosSelected()
    {
        // Draw a yellow cube at the transform position
        if (es == null)
        {
            return;
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(es.sizeX, 10, es.sizeZ));
    }
}
