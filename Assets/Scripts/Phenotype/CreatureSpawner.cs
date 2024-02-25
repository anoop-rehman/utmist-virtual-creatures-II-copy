using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Newtonsoft.Json; // Make sure to include Newtonsoft.Json or another JSON library

public class SegmentInfo
{
    public int UniqueId { get; set; }
    public byte TypeId { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 LocalPosition { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 LocalRotation { get; set; }
    public Vector3 Size { get; set; }
    public int? ParentUniqueId { get; set; }
    public string? JointType { get; set; }
    public Vector3? JointAnchorPos { get; set; }
    public Vector3? JointAxis { get; set; }
    public Vector3 Color { get; set; }
}

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

        SpawnSegmentData ssd = new SpawnSegmentData();
        ssd.cg = cg;
        ssd.c = c;
        ssd.recursiveLimitValues = recursiveLimitInitial;
        ssd.isRoot = true;
        ssd.position = position;

        SpawnSegment(ssd);
        //SpawnSegment(cg, c, recursiveLimitInitial, position);
        c.InitializeCreature(fitness);
        //Debug.Log("Spawned creature " + c.name);

        //Debug.Log("Its size is: " + c.cg.GetSize().ToString());


        List<SegmentInfo> segmentsInfo = new List<SegmentInfo>();

        //foreach (Segment segment in c.segments)
        for (int i = 0; i < c.segments.Count; i++)
        {
            Segment segment = c.segments[i];
            SegmentInfo info = new SegmentInfo
            {
                UniqueId = i,
                TypeId = segment.id,
                Position = segment.myRigidbody.transform.position,
                LocalPosition = segment.myRigidbody.transform.localPosition,
                Rotation = segment.myRigidbody.transform.rotation.eulerAngles,
                LocalRotation = segment.myRigidbody.transform.localEulerAngles,
                Size = segment.myRigidbody.transform.localScale
            };

            if (segment.myRigidbody.GetComponent<HingeJoint>() != null && segment.myRigidbody.GetComponent<FixedJoint>() == null)
            {
                HingeJoint hingeJoint = segment.myRigidbody.GetComponent<HingeJoint>();
                Segment parentSegment = hingeJoint.connectedBody.gameObject.GetComponent<Segment>();
                info.ParentUniqueId = c.segments.FindIndex(s => s == parentSegment);
                info.JointType = "hinge";
                info.JointAnchorPos = hingeJoint.connectedAnchor;
                info.JointAxis = hingeJoint.axis;
            }
            else if (segment.myRigidbody.GetComponent<HingeJoint>() == null && segment.myRigidbody.GetComponent<FixedJoint>() != null)
            {
                FixedJoint fixedJoint = segment.myRigidbody.GetComponent<FixedJoint>();
                Segment parentSegment = fixedJoint.connectedBody.gameObject.GetComponent<Segment>();
                info.ParentUniqueId = c.segments.FindIndex(s => s == parentSegment);
                info.JointType = "fixed";
                info.JointAnchorPos = fixedJoint.connectedAnchor;
            }
            else if (segment.myRigidbody.GetComponent<HingeJoint>() == null && segment.myRigidbody.GetComponent<FixedJoint>() == null)
            {
                info.ParentUniqueId = null;
                info.JointType = null;
                info.JointAnchorPos = null;
            }

            byte r = c.cg.segments.Find(seg => seg.id == info.TypeId).r;
            byte g = c.cg.segments.Find(seg => seg.id == info.TypeId).g;
            byte b = c.cg.segments.Find(seg => seg.id == info.TypeId).b;
            info.Color = new Vector3(r, g, b);

            segmentsInfo.Add(info);


            //Debug.Log("------");
            //Debug.Log("the segment's unique id is: " + i); //prob remove
            //byte typeId = segment.id;
            //Debug.Log("(for debug)the segment's type id is: " + typeId); //prob remove
            //Debug.Log("the segment's position is " + segment.myRigidbody.transform.position);
            //Debug.Log("the segment's local position is " + segment.myRigidbody.transform.localPosition);

            //Debug.Log("the segment's rotation is " + segment.myRigidbody.transform.rotation.eulerAngles);
            //Debug.Log("the segment's local rotation is " + segment.myRigidbody.transform.localEulerAngles);
            //Debug.Log("the segment's size is " + segment.myRigidbody.transform.localScale);


            ////if (segment.parent != null)
            ////{
            ////    Debug.Log("the segment's parent's id is " + segment.parent.Item2.id); // prob remove
            ////}

            //// wait so then but how do we find the unique parent id..
            //// soln: the joint knows. yeah that works

            //if (segment.myRigidbody.GetComponent<HingeJoint>() != null && segment.myRigidbody.GetComponent<FixedJoint>() == null)
            //{
            //    HingeJoint hingeJoint = segment.myRigidbody.GetComponent<HingeJoint>();
            //    Segment parentSegment = hingeJoint.connectedBody.gameObject.GetComponent<Segment>();
            //    Debug.Log("the segment's parent's unique id is: " + c.segments.FindIndex(s => s == parentSegment));

            //    Debug.Log("the segment's joint type is: hinge");
            //    Debug.Log("the segment's joint's anchorpos is: " + segment.myRigidbody.GetComponent<HingeJoint>().connectedAnchor);
            //    Debug.Log("the segment's joint axis is: " + hingeJoint.axis);
            //}
            //if (segment.myRigidbody.GetComponent<HingeJoint>() == null && segment.myRigidbody.GetComponent<FixedJoint>() != null)
            //{
            //    FixedJoint fixedJoint = segment.myRigidbody.GetComponent<FixedJoint>();
            //    Segment parentSegment = fixedJoint.connectedBody.gameObject.GetComponent<Segment>();
            //    Debug.Log("the segment's parent's unique id is " + c.segments.FindIndex(s => s == parentSegment));

            //    Debug.Log("the segment's joint type is: fixed");
            //    Debug.Log("the segment's joint's anchorpos is " + segment.myRigidbody.GetComponent<FixedJoint>().connectedAnchor);
            //}


            ////Debug.Log("the segment's color is: " + segment.myRigidbody.transform.GetChild(0).GetComponent<Material>().color.ToString());
            //byte r = c.cg.segments.Find(segment => segment.id == typeId).r;
            //byte g = c.cg.segments.Find(segment => segment.id == typeId).g;
            //byte b = c.cg.segments.Find(segment => segment.id == typeId).b;

            //Debug.Log("the segment's color is: " + new Vector3(r, g, b));

        }


        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // Serialize to JSON
        string json = JsonConvert.SerializeObject(segmentsInfo, Formatting.Indented, settings);
        // Write to file
        File.WriteAllText("blueprint.json", json);

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

    private void SetupSegment(SegmentGrabData sgd, out Segment spawnedSegment,
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
        spawnedSegmentGameObject.name = $"Segment Added of Segment Type ID = {id}";
        spawnedSegment.SetId(id);
        spawnedSegment.SetCreature(sgd.c);
        sgd.c.segments.Add(spawnedSegment);

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
            if (id == 1) // if this segment is the root node
            {
                List<byte> ngNrIds = new List<byte>();
                foreach (NeuronGenotype ng in sgd.sg.neurons)
                {
                    ngNrIds.Add(ng.nr.id);
                }

                for (int i = 9; i < 12; i++)
                {
                    if (!ngNrIds.Contains((byte) i))
                    {
                        NeuronGenotype photoSensorNg = new NeuronGenotype(new NeuronReference());
                        photoSensorNg.nr.id = (byte) i;
                        photoSensorNg.nr.relativeLevelNullable = null;
                        photoSensorNg.nr.relativityNullable = null;

                        sgd.sg.neurons.Add(photoSensorNg);

                        ngNrIds.Add((byte) i);

                    }
                }
            }


            // Add neurons
            foreach (NeuronGenotype ng in sgd.sg.neurons)
            {
                ng.nr.connectionPath = sgd.connectionPath;
                ng.nr.relativityNullable = NeuronReferenceRelativity.TRACED;
                Neuron addedNeuron;
                if (ng.nr.id == 12)
                {
                    addedNeuron = sgd.c.AddNeuron(ng, spawnedSegmentGameObject.GetComponent<Joint>(), spawnedSegment, id);
                }
                else if (ng.nr.id <= 11)
                {
                    addedNeuron = sgd.c.AddNeuron(ng, null, spawnedSegment, id);
                }
                else
                {
                    addedNeuron = sgd.c.AddNeuron(ng, null, spawnedSegment, id);
                }
                spawnedSegment.AddNeuron(addedNeuron);
            }

            

        }
    }

    private struct SpawnSegmentData
    {
        public CreatureGenotype cg;
        public Creature c;
        public Dictionary<byte, byte> recursiveLimitValues;
        public bool isRoot;
        public Vector3 position;
        public SegmentConnectionGenotype myConnection;
        public GameObject parentSegment;
        public float? parentGlobalScale;
        public bool? parentReflect;
        public List<byte> connectionPath;
    }
    Segment SpawnSegment(SpawnSegmentData ssd)
    {
        // Debug.Log(counter);
        if (ssd.isRoot) counter = 0;
        if (counter++ == 80) ssd.cg.SaveDebug();

        // Find SegmentGenotype
        byte id = ssd.isRoot ? (byte)1 : ssd.myConnection.destination;
        SegmentGenotype currentSegmentGenotype = ssd.cg.GetSegment(id);
        if (currentSegmentGenotype == null) return null;

        // Calculate required transform properties
        Vector3 spawnPos; Quaternion spawnAngle; int otherReflectInt;
        float parentScale; bool otherReflectBool;
        if (ssd.isRoot)
        {
            ssd.cg.EulerToQuat(); // Debug, remove later (this changes internal rotation storage stuff to make inspector editing easier.)
            spawnPos = ssd.position;
            spawnAngle = ssd.cg.orientation;
            otherReflectInt = 1;
            otherReflectBool = false;
            List<byte> connectionPath = new List<byte>();

            parentScale = 1f;
        }
        else
        {
            ssd.myConnection.EulerToQuat(); // Debug, remove later (this changes internal rotation storage stuff to make inspector editing easier.)

            Transform parentTransform = ssd.parentSegment.transform;

            int reflectInt = ssd.myConnection.reflected ? -1 : 1;
            int parentReflectInt = ssd.parentReflect.Value ? -1 : 1;
            otherReflectBool = ssd.myConnection.reflected ^ ssd.parentReflect.Value;
            otherReflectInt = otherReflectBool ? -1 : 1;

            spawnPos = parentTransform.position +
                parentTransform.right * parentTransform.localScale.x * ssd.myConnection.anchorX * reflectInt * parentReflectInt +
                parentTransform.up * parentTransform.localScale.y * (ssd.myConnection.anchorY + 0.5f) +
                parentTransform.forward * parentTransform.localScale.z * ssd.myConnection.anchorZ;

            spawnAngle = Quaternion.identity;
            spawnAngle *= parentTransform.rotation;
            spawnAngle *= ssd.myConnection.orientation;
            if (otherReflectBool)
            {
                spawnAngle = Quaternion.LookRotation(Vector3.Reflect(spawnAngle * Vector3.forward, Vector3.right), Vector3.Reflect(spawnAngle * Vector3.up, Vector3.right));
            }

            parentScale = ssd.parentGlobalScale.Value * ssd.myConnection.scale;
        }

        // Package the data
        SegmentGrabData sgd = new SegmentGrabData();
        sgd.cg = ssd.cg;
        sgd.sg = currentSegmentGenotype;
        sgd.pos = spawnPos;
        sgd.rot = spawnAngle;
        sgd.c = ssd.c;
        sgd.parentScale = parentScale;
        sgd.recursiveLimitValues = ssd.recursiveLimitValues;
        sgd.connectionPath = ssd.connectionPath;
        sgd.parentSegmentRigidbody = ssd.parentSegment?.GetComponent<Rigidbody>();
        sgd.isRoot = ssd.isRoot;
        sgd.otherReflectInt = otherReflectInt;

        // Spawn the segment
        if (segmentPool == null) InitializeSegmentObjectPool();
        SetupSegment(sgd, out Segment spawnedSegment, out GameObject spawnedSegmentGameObject, out bool runTerminalOnly);

        // Check if self-intersecting TODO

        // Trace outward
        foreach (SegmentConnectionGenotype connection in currentSegmentGenotype.connections)
        {
            if (ssd.recursiveLimitValues[connection.destination] > 0)
            {
                if (!runTerminalOnly && connection.terminalOnly)
                {
                    continue;
                }
                var recursiveLimitClone = ssd.recursiveLimitValues.ToDictionary(entry => entry.Key, entry => entry.Value);
                
                // Clone the connection path for the child segments, create new path if root
                var connectionPathClone = ssd.isRoot ? new List<byte>() : ssd.connectionPath.Select(item => item).ToList();
                connectionPathClone.Add(connection.id);

                // Create new data packet for recursion
                SpawnSegmentData ssd2 = new SpawnSegmentData();
                ssd2.cg = ssd.cg;
                ssd2.c = ssd.c;
                ssd2.recursiveLimitValues = recursiveLimitClone;
                ssd2.myConnection = connection;
                ssd2.parentSegment = spawnedSegmentGameObject;

                ssd2.parentGlobalScale = parentScale;
                ssd2.parentReflect = otherReflectBool;
                ssd2.connectionPath = connectionPathClone;

                // Recurse to child segment
                Segment childSegment = SpawnSegment(ssd2);
                childSegment.SetParent(connection.id, spawnedSegment);
                spawnedSegment.AddChild(connection.id, childSegment);
            }
        }


        //Debug.Log("spawnedSegment.name: " + spawnedSegment.name);
        //if (spawnedSegment.parent != null)
        //{
        //    Debug.Log("spawnedSegment.parent.Item2.name: " + spawnedSegment.parent.Item2.name);
        //    Debug.Log("spawnedSegment.parent.Item1: " + spawnedSegment.parent.Item1.ToString());
        //}
     
        return spawnedSegment;
    }

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + spawnPos, 0.1f);
    }
}
