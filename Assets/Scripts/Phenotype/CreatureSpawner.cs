using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class CreatureSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public Creature creaturePrefab;
    public GameObject segmentPrefab;

    [Header("Settings")]
    [SerializeField]
    private Vector3 spawnPos;

    
    public CreatureGenotype creatureGenotype;
    public List<CreatureGenotype> creatureGenotypeHistory;

    private static ObjectPool<Segment> segmentPool;
    public static CreatureSpawner instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject); // Can't have two spawners active at once
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        CreatureGenotype testCreature = new CreatureGenotype
        {
            name = "Test Creature"
        };

        SegmentGenotype testSegment = new SegmentGenotype
        {
            id = 1,
            jointType = JointType.Fixed,
            r = 255,
            g = 0,
            b = 0
        };

        SegmentConnectionGenotype testConnection1 = new SegmentConnectionGenotype
        {
            destination = 1,
            anchorX = 0.5f,
            anchorY = 0.0f,
            anchorZ = 0.0f,
            scale = 0.5f
        };

        //SpawnCreature(creatureGenotype, (Vector3.up + Vector3.right), null);
        /*creatureGenotypeHistory.Add(creatureGenotype.Clone());
        for (int i = 0; i < 16; i++)
        {
            GetComponent<MutateGenotype>().MutateCreatureGenotype(creatureGenotype, new MutationPreferenceSetting());
            creatureGenotypeHistory.Add(creatureGenotype.Clone());
        }
        SpawnCreature(creatureGenotype, (Vector3.up + Vector3.right) * 2);*/
    }

    bool VerifyCreatureGenotypeIntegrity(CreatureGenotype cg)
    {
        return true;
    }

    public Creature SpawnCreature(CreatureGenotype cg, Vector3 position){

        //test_cg = 

        return SpawnCreature(cg, position, null);
    }

    // Creature & GHOST (ID 0)
    public Creature SpawnCreature(CreatureGenotype cg, Vector3 position, Fitness fitness)
    {
        // Verify
        counter = 0;
        if (!VerifyCreatureGenotypeIntegrity(cg))
        {
            return null;
        }

        // Create recursive limit dict
        Dictionary<byte, byte> recursiveLimitInitial = new Dictionary<byte, byte>();
        foreach (SegmentGenotype segment in cg.segments)
        {
            recursiveLimitInitial[segment.id] = segment.recursiveLimit;
        }

        Creature c = Instantiate(creaturePrefab, Vector3.zero, Quaternion.identity);
        c.name = $"Creature ({cg.name})";
        c.cg = cg.Clone();
        c.transform.parent = transform;
        
        // Add neurons
        SegmentGenotype ghost = cg.GetSegment(0);
        if (ghost != null)
        {
            foreach (NeuronGenotype nm in ghost.neurons)
            {
                nm.nr.relativityNullable = NeuronReferenceRelativity.GHOST;
                c.AddNeuron(nm, null, null, 0);
            }
        }

        SpawnSegment(cg, c, recursiveLimitInitial, position);
        c.InitializeCreature(fitness);
        return c;
    }

    // Keeps track of segment count, used for auto-flagging strange creatures.
    [SerializeField] private int counter = 0;

    public void ReleaseCreature(Creature c){
        Segment[] segments = c.transform.GetComponentsInChildren<Segment>();
        if (segments != null)
        {
            foreach (Segment segment in segments)
            {
                segmentPool.Release(segment);
            }
        }
        Destroy(c.gameObject);
    }

    private void InitializeSegmentObjectPool()
    {
        Segment segmentPrefab = Resources.Load<Segment>("Pool Prefabs/Segment");

        int maxSegments;
        OptimizationSettings os = EvolutionSettingsPersist.instance.save.ts.optimizationSettings;
        if (os.stage == TrainingStage.KSS)
        {
            KSSSettings kss = (KSSSettings)os;
            maxSegments = kss.mp.maxSegments;
        }
        else
        {
            RLSettings rl = (RLSettings)os;
            // maxSegments = rl.initialGenotype.GetSegmentCount(); // This method doesn't exist.
            maxSegments = 20;
        }
        int envCount = os.numEnvs;

        segmentPool = new ObjectPool<Segment>(() =>
        {
            Segment segment = Instantiate(segmentPrefab);
            DontDestroyOnLoad(segment.gameObject);
            return segment;
        }, segment =>
        {
            SceneManager.MoveGameObjectToScene(segment.gameObject, SceneManager.GetActiveScene());
            segment.Initialize();
            segment.gameObject.SetActive(true);
        }, segment =>
        {
            segment.gameObject.SetActive(false);
            segment.transform.parent = null;
            segment.Release();
            DontDestroyOnLoad(segment.gameObject);
        }, segment =>
        {
            Destroy(segment.gameObject);
        }, true, envCount * Mathf.Min(5, maxSegments), maxSegments * envCount);
    }

    private struct SegmentGrabData
    {
        public CreatureGenotype cg;
        public Vector3 pos;
        public Quaternion rot;
        public Creature c;
        public SegmentGenotype sg;
        public float parentScale;
        public Dictionary<byte, byte> recursiveLimitValues;
        public List<byte> connectionPath;
        public Rigidbody parentSegmentRigidbody;
        public bool isRoot;
        public int otherReflectInt;
    }

    private void InitialSegmentGrab(SegmentGrabData sgd, out Segment spawnedSegment,
        out GameObject spawnedSegmentGameObject,
        out bool runTerminalOnly)
    {
        // Calculate required values
        Vector3 dimVector = new Vector3(sgd.sg.dimensionX, sgd.sg.dimensionY, sgd.sg.dimensionZ) * sgd.parentScale;
        byte id = sgd.sg.id;

        // Pull Segment from pool
        spawnedSegment = segmentPool.Get();
        spawnedSegmentGameObject = spawnedSegment.gameObject;
        
        // Initialize transform, name, and data
        spawnedSegmentGameObject.transform.position = sgd.pos;
        spawnedSegmentGameObject.transform.rotation = sgd.rot;
        spawnedSegmentGameObject.transform.localScale = dimVector;
        spawnedSegmentGameObject.transform.parent = sgd.c.transform;
        spawnedSegmentGameObject.name = $"Segment {id}";
        spawnedSegment.SetId(id);
        spawnedSegment.SetCreature(sgd.c);

        if (!sgd.isRoot)
        {
            spawnedSegment.SetPath(sgd.connectionPath);
            FluidDrag fluidDrag = spawnedSegmentGameObject.GetComponent<FluidDrag>();
            fluidDrag.negYCovered = true;
        }

        // Handle other components
        Rigidbody rb = spawnedSegmentGameObject.GetComponent<Rigidbody>();
        rb.mass *= dimVector.x * dimVector.y * dimVector.z;

        Transform spawnedGraphic = spawnedSegmentGameObject.transform.Find("Graphic");
        spawnedGraphic.GetComponent<Renderer>().material.color = sgd.sg.color;

        // Check terminal only
        runTerminalOnly = false;
        sgd.recursiveLimitValues[id]--;
        if (sgd.recursiveLimitValues[id] == 0 || !sgd.sg.connections.Any(scg => scg.destination == sgd.sg.id))
        {
            runTerminalOnly = true;
        }

        // Add joints if non-root
        if (!sgd.isRoot)
        {
            sgd.c.actionMotors.Add(spawnedSegmentGameObject.GetComponent<HingeJoint>());
            switch (sgd.sg.jointType)
            {
                case (JointType.Fixed):
                    {
                        spawnedSegment.AttachFixedJoint(sgd.parentSegmentRigidbody);
                    }
                    break;

                case (JointType.HingeX):
                    {
                        spawnedSegment.AttachHingeJoint(new Vector3(1, 0, 0), sgd.parentSegmentRigidbody);
                    }
                    break;

                case (JointType.HingeY):
                    {
                        spawnedSegment.AttachHingeJoint(new Vector3(0, 1 * sgd.otherReflectInt, 0), sgd.parentSegmentRigidbody);
                    }
                    break;

                case (JointType.HingeZ):
                    {
                        spawnedSegment.AttachHingeJoint(new Vector3(0, 0, 1 * sgd.otherReflectInt), sgd.parentSegmentRigidbody);
                    }
                    break;

                case (JointType.Spherical):
                    {
                        spawnedSegment.AttachSphericalJoint(sgd.parentSegmentRigidbody);
                    }
                    break;

                default:
                    break;
            }
        }

        // Add neurons
        if (sgd.cg.stage == TrainingStage.KSS)
        {
            // Add neurons
            foreach (NeuronGenotype nm in sgd.sg.neurons)
            {
                nm.nr.connectionPath = sgd.connectionPath;
                nm.nr.relativityNullable = NeuronReferenceRelativity.TRACED;
                Neuron addedNeuron;
                if (nm.nr.id == 12)
                {
                    addedNeuron = sgd.c.AddNeuron(nm, spawnedSegmentGameObject.GetComponent<Joint>(), spawnedSegment, id);
                }
                else if (nm.nr.id <= 11)
                {
                    addedNeuron = sgd.c.AddNeuron(nm, null, spawnedSegment, id);
                }
                else
                {
                    addedNeuron = sgd.c.AddNeuron(nm, null, spawnedSegment, id);
                }
                spawnedSegment.AddNeuron(addedNeuron);
            }
        }

        // Add segment to creature
        sgd.c.segments.Add(spawnedSegment);
    }

    // Non-root (ID 2>)
    Segment SpawnSegment(CreatureGenotype cg, Creature c, Dictionary<byte, byte> recursiveLimitValues, SegmentConnectionGenotype myConnection, GameObject parentSegment, float parentGlobalScale, bool parentReflect, List<byte> connectionPath)
    {
        // Debug.Log(counter);
        if (counter++ == 80) cg.SaveDebug();


        // Find SegmentGenotype
        byte id = myConnection.destination;
        SegmentGenotype currentSegmentGenotype = cg.GetSegment(id);
        if (currentSegmentGenotype == null) return null;

        myConnection.EulerToQuat(); // Debug, remove later (this changes internal rotation storage stuff to make inspector editing easier.)

        // Calculate required transform properties
        Transform parentTransform = parentSegment.transform;

        int reflectInt = myConnection.reflected ? -1 : 1;
        int parentReflectInt = parentReflect ? -1 : 1;
        bool otherReflectBool = myConnection.reflected ^ parentReflect;
        int otherReflectInt = otherReflectBool ? -1 : 1;

        Vector3 spawnPos = parentTransform.position +
            parentTransform.right * parentTransform.localScale.x * myConnection.anchorX * reflectInt * parentReflectInt +
            parentTransform.up * parentTransform.localScale.y * (myConnection.anchorY + 0.5f) +
            parentTransform.forward * parentTransform.localScale.z * myConnection.anchorZ;

        Quaternion spawnAngle = Quaternion.identity;
        spawnAngle *= parentTransform.rotation;
        spawnAngle *= new Quaternion(myConnection.orientationX, myConnection.orientationY, myConnection.orientationZ, myConnection.orientationW);

        if (otherReflectBool)
        {
            spawnAngle = Quaternion.LookRotation(Vector3.Reflect(spawnAngle * Vector3.forward, Vector3.right), Vector3.Reflect(spawnAngle * Vector3.up, Vector3.right));
        }

        // Package the data
        SegmentGrabData sgd = new SegmentGrabData();
        sgd.cg = cg;
        sgd.sg = currentSegmentGenotype;
        sgd.pos = spawnPos;
        sgd.rot = spawnAngle;
        sgd.c = c;
        sgd.parentScale = parentGlobalScale * myConnection.scale;
        sgd.recursiveLimitValues = recursiveLimitValues;
        sgd.connectionPath = connectionPath;
        sgd.parentSegmentRigidbody = parentSegment.GetComponent<Rigidbody>();
        sgd.isRoot = false;
        sgd.otherReflectInt = otherReflectInt;

        // Spawn the segment
        InitialSegmentGrab(sgd, out Segment spawnedSegment, out GameObject spawnedSegmentGameObject, out bool runTerminalOnly);

        // Check if self-intersecting TODO

        // Trace outward
        foreach (SegmentConnectionGenotype connection in currentSegmentGenotype.connections)
        {
            if (recursiveLimitValues[connection.destination] > 0)
            {
                if (!runTerminalOnly && connection.terminalOnly)
                {
                    continue;
                }
                var recursiveLimitClone = recursiveLimitValues.ToDictionary(entry => entry.Key, entry => entry.Value);
                var connectionPathClone = connectionPath.Select(item => (byte)item).ToList();
                connectionPathClone.Add(connection.id);
                Segment childSegment = SpawnSegment(cg, c, recursiveLimitClone, connection, spawnedSegmentGameObject, parentGlobalScale * myConnection.scale, otherReflectBool, connectionPathClone);
                childSegment.SetParent(connection.id, spawnedSegment);
                spawnedSegment.AddChild(connection.id, childSegment);
            }
        }

        return spawnedSegment;
    }

    // Root (ID 1)
    void SpawnSegment(CreatureGenotype cg, Creature c, Dictionary<byte, byte> recursiveLimitValues, Vector3 position)
    {
        //Debug.Log("S: ROOT");

        // Find SegmentGenotype
        byte id = 1;
        SegmentGenotype currentSegmentGenotype = cg.GetSegment(id);
        if (currentSegmentGenotype == null) return;

        cg.EulerToQuat(); //Debug, remove later (this changes internal rotation storage stuff to make inspector editing easier.)

        // Calculate required transform properties
        Quaternion spawnAngle = new Quaternion(cg.orientationX, cg.orientationY, cg.orientationZ, cg.orientationW);
        List<byte> connectionPath = new List<byte>();

        // Package the data
        SegmentGrabData sgd = new SegmentGrabData();
        sgd.cg = cg;
        sgd.sg = currentSegmentGenotype;
        sgd.pos = position;
        sgd.rot = spawnAngle;
        sgd.c = c;
        sgd.parentScale = 1f;
        sgd.recursiveLimitValues = recursiveLimitValues;
        sgd.connectionPath = connectionPath;
        sgd.parentSegmentRigidbody = null;
        sgd.isRoot = true;
        sgd.otherReflectInt = 1;

        // Spawn the segment
        if (segmentPool == null) InitializeSegmentObjectPool(); // Check object pool status
        InitialSegmentGrab(sgd, out Segment spawnedSegment, out GameObject spawnedSegmentGameObject, out bool runTerminalOnly);

        // Trace outward
        foreach (SegmentConnectionGenotype connection in currentSegmentGenotype.connections)
        {
            if (recursiveLimitValues[connection.destination] > 0)
            {
                if (!runTerminalOnly && connection.terminalOnly)
                {
                    continue;
                }
                var recursiveLimitClone = recursiveLimitValues.ToDictionary(entry => entry.Key, entry => entry.Value);
                Segment childSegment = SpawnSegment(cg, c, recursiveLimitClone, connection, spawnedSegmentGameObject, 1, false, new List<byte>() { connection.id });
                childSegment.SetCreature(c);
                childSegment.SetParent(connection.id, spawnedSegment);
                spawnedSegment.AddChild(connection.id, childSegment);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + spawnPos, 0.1f);
    }

}