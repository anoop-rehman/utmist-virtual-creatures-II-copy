using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class TrainingSave {
    public string saveName = "Unnamed Save";
    public string savePath;
    public bool isNew;
    public TrainingSettings ts;

    public void SaveData(string path, bool isFullPath, bool overwrite)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string fullPath = isFullPath ? path : Application.persistentDataPath + path;
        if (!overwrite) fullPath = fullPath.GetNextFilename();

        FileStream stream = new FileStream(fullPath, FileMode.Create);

        formatter.Serialize(stream, this);
        stream.Close();
    }

    public static TrainingSave LoadData(string path, bool isFullPath)
    {
        string fullPath = isFullPath ? path : Application.persistentDataPath + path;

        if (File.Exists(fullPath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(fullPath, FileMode.Open);

            TrainingSave data = formatter.Deserialize(stream) as TrainingSave;

            stream.Close();

            return data;
        }
        else
        {
            Debug.LogError("Error: Save file not found in " + fullPath);
            return null;
        }
    }
}

[System.Serializable]
public class TrainingSettings {
    public OptimizationSettings optimizationSettings;
    public EnvironmentSettings envSettings;

    public TrainingSettings(OptimizationSettings os, EnvironmentSettings es){
        optimizationSettings = os;
        envSettings = es;
    }
}

[System.Serializable]
public abstract class OptimizationSettings {
    public int numEnvs = 1;
    public CreatureGenotype initialGenotype;
    public abstract TrainingStage stage { get; }
}

[System.Serializable]
public class RLSettings : OptimizationSettings {
    public override TrainingStage stage { get {return TrainingStage.RL; }}
}

[System.Serializable]
public class KSSSettings : OptimizationSettings {
    public override TrainingStage stage { get { return TrainingStage.KSS; } }
    public int populationSize;
    public int totalGenerations;
    public float survivalRatio;
    public MutateGenotype.MutationPreferenceSetting mp;

    public KSSSettings(int ps, int tg, float sr)
    {
        populationSize = ps;
        totalGenerations = tg;
        survivalRatio = sr;
    }

    public KSSSettings()
    {
        populationSize = 300;
        totalGenerations = 200;
        survivalRatio = 1f / 5f;
    }
}

/// <summary>
/// Generates Environments, tallies data, Starts and Stops training
/// </summary>
public class TrainingManager : MonoBehaviour
{
    public static TrainingManager instance;

    public List<Environment> environments { get; private set; }

    public TrainingSave save { get; private set; }
    [SerializeField]
    public TrainingSettings ts { get; private set; }
    [SerializeField]
    private TrainingStage stage;

    // test
    public CreatureGenotype creatureGenotype;

    // References to components
    public Text statsText;
    private TrainingAlgorithm algo;
    private Transform envHolder;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject); // Can't have two managers active at once
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        EvolutionSettingsPersist esp = EvolutionSettingsPersist.instance;
        if (esp == null)
        {
            throw new Exception("No EvolutionSettingsPersist instance found. Try launching from the Menu Scene!");
        }

        save = esp.save;
        Debug.Log(save.saveName);
        ts = save.ts;
        stage = ts.optimizationSettings.stage;
        creatureGenotype = ts.optimizationSettings.initialGenotype;
        envHolder = transform.Find("EnvHolder");
        if (envHolder == null){
            envHolder = new GameObject().transform;
            envHolder.transform.parent = transform;
            envHolder.transform.localPosition = Vector3.zero;
            envHolder.name = "EnvHolder";
        }

        GameObject envPrefab = Resources.Load<GameObject>("Prefabs/Envs/" + EnvironmentSettings.envString[ts.envSettings.envCode]);
        environments = new List<Environment>();

        if (ts.envSettings.envArrangeType == EnvArrangeType.LINEAR){
            float sizeX = ts.envSettings.sizeX;
            int l = ts.optimizationSettings.numEnvs;
            for (int i = 0; i < l; i++)
            {
                Environment instantiatedEnv = Instantiate(envPrefab, Vector3.right * i * sizeX, envPrefab.transform.rotation).GetComponent<Environment>();
                instantiatedEnv.Setup(ts.envSettings);
                instantiatedEnv.transform.parent = envHolder;
                instantiatedEnv.gameObject.name += (i + 1).ToString();
                // ATM, I take the current floorEnv (transform object) and add 'x' units in Z direction.
                // This makes it so each floor env will have its photosensor 5 units front of where the agent spawns
                Vector3 photosensorSpawnLocation = instantiatedEnv.transform.position;
                // Generate a random z value between -25 and +25
                // Randomizing the spawn location of goal photosensor to encourage better learning every generation (yet to delete and spawn new random photosensor per gen)
                // Randomly choose between if z is + or -
                float randomZ = 8f;
                //if (UnityEngine.Random.Range(0, 2) == 0)
                //{
                //    randomZ = UnityEngine.Random.Range(-12f, -7f);
                //}
                //else
                //{
                //    randomZ = UnityEngine.Random.Range(7f, 12f);
                //}
                //float randomX;
                //if (UnityEngine.Random.Range(0, 2) == 0)
                //{
                //    randomX = UnityEngine.Random.Range(-6f, -2f);
                //}
                //else
                //{
                //    randomX = UnityEngine.Random.Range(2f, 6f);
                //}
                photosensorSpawnLocation.z += randomZ;
                //photosensorSpawnLocation.x += randomX;
                // Not sure why the photosensor spawns in the sky so to bring it to ground, do -5f
                photosensorSpawnLocation.y -= 5f;
                instantiatedEnv.SpawnPhotosensorObject(photosensorSpawnLocation);
                environments.Add(instantiatedEnv);
                Transform oneOff = instantiatedEnv.transform.Find("OneOffHolder");
                if (oneOff != null && i != 0) {
                    Destroy(oneOff.gameObject);
                }
            }
        } else if (ts.envSettings.envArrangeType == EnvArrangeType.PLANAR){
            // TODO lol
        }

        if (stage == TrainingStage.KSS) {
            algo = (TrainingAlgorithm)gameObject.AddComponent(typeof(KSS.KSSAlgorithm));
            algo.Setup(this);
        }
    }

    public void SaveTraining(){
        algo.SaveTraining();
    }

    public Creature GetBestLivingCreature()
    {
        try
        {
            return environments.OrderByDescending(x => x.currentCreature.totalReward).First().currentCreature;
        }
        catch (Exception)
        {

            return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
