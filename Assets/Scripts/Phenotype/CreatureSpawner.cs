using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreatureSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public Creature creaturePrefab;
    public GameObject segmentPrefab;
    public GameObject characterJointPrefab;
    public GameObject hingeJointPrefab;

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

        Creature c = Instantiate(creaturePrefab, position, Quaternion.identity);
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

        myConnection.EulerToQuat(); //Debug, remove later (this changes internal rotation storage stuff to make inspector editing easier.)


        byte id = myConnection.destination;
        //Debug.Log($"S: {myConnection.destination} ({recursiveLimitValues[id]})");

        // Find segmentGenotype
        SegmentGenotype currentSegmentGenotype = cg.GetSegment(id);

        if (currentSegmentGenotype == null)
            return null;

        Transform parentTransform = parentSegment.transform;

        int reflectInt = myConnection.reflected ? -1 : 1;
        //bool otherReflectBool = myConnection.reflected ^ (Mathf.Sign(parentTransform.localScale.x)) == -1;
        int parentReflectInt = parentReflect ? -1 : 1;
        bool otherReflectBool = myConnection.reflected ^ parentReflect;
        int otherReflectInt = otherReflectBool ? -1 : 1;


        Vector3 spawnPos = parentTransform.position +
            parentTransform.right * parentTransform.localScale.x * myConnection.anchorX * reflectInt * parentReflectInt*1f +
            parentTransform.up * parentTransform.localScale.y * (myConnection.anchorY + 0.5f)* 1f +
            parentTransform.forward * parentTransform.localScale.z * myConnection.anchorZ* 1f;

        Vector3 dimVector = new Vector3(currentSegmentGenotype.dimensionX /* * otherReflectInt*/, currentSegmentGenotype.dimensionY, currentSegmentGenotype.dimensionZ);
        dimVector *= parentGlobalScale * myConnection.scale;

        Vector3 jointPosition = Vector3.zero;
        if (Math.Round(Math.Abs(myConnection.anchorX), 1) == 0.5f) {
            jointPosition += parentTransform.right * Math.Sign(myConnection.anchorX);
          
        } else if (Math.Round(Math.Abs(myConnection.anchorY), 1) == 0.5f) {
            jointPosition += parentTransform.up * Math.Sign(myConnection.anchorY);

        } else if (Math.Round(Math.Abs(myConnection.anchorY), 1) == 0.5f) {
            jointPosition += parentTransform.forward * Math.Sign(myConnection.anchorZ);

        }

        Quaternion spawnAngle = Quaternion.identity;
        spawnAngle *= parentTransform.rotation;
        spawnAngle *= new Quaternion(myConnection.orientationX, myConnection.orientationY, myConnection.orientationZ, myConnection.orientationW);

        int maxSegments;
        OptimizationSettings os = EvolutionSettingsPersist.instance.save.ts.optimizationSettings;
        if (os.stage == TrainingStage.KSS)
        {
            KSSSettings kss = (KSSSettings)os;
            maxSegments = kss.mp.maxSegments;
        }
        //spawnAngle *= parentTransform.rotation;
        GameObject spawnedSegmentGameObject = Instantiate(segmentPrefab, spawnPos, spawnAngle);
        spawnedSegmentGameObject.transform.parent = c.transform;
        spawnedSegmentGameObject.name = $"Segment {currentSegmentGenotype.id}";

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
        
        spawnedSegmentGameObject.transform.localScale = dimVector;
        spawnedSegmentGameObject.transform.parent = sgd.c.transform;
        spawnedSegmentGameObject.name = $"Segment {id}";
        spawnedSegment.SetId(id);
        spawnedSegment.SetCreature(sgd.c);
        sgd.c.segments.Add(spawnedSegment);

        Rigidbody rb = spawnedSegmentGameObject.GetComponent<Rigidbody>();
        rb.mass *= dimVector.x * dimVector.y * dimVector.z;

        switch (currentSegmentGenotype.jointType)
        {
            case (JointType.Fixed):
                {
                    FixedJoint j = spawnedSegmentGameObject.AddComponent<FixedJoint>();
                    j.connectedBody = parentSegment.GetComponent<Rigidbody>();
                    j.autoConfigureConnectedAnchor = false;
                    //j.anchor = jointPosition;
                }
                break;

            case (JointType.HingeX):
                {
                    GameObject jointObject = Instantiate(hingeJointPrefab, parentTransform.position, spawnAngle);

                    jointObject.transform.parent = c.transform;
                    jointObject.transform.localPosition = Vector3.zero;
                    FixedJoint jointObjectj = jointObject.AddComponent<FixedJoint>();
                    jointObjectj.autoConfigureConnectedAnchor = false;
                    jointObjectj.connectedBody = parentSegment.GetComponent<Rigidbody>();
                    jointPosition.x *= -1;
                    jointObjectj.anchor = jointPosition;
                    jointObject.transform.localRotation = Quaternion.Euler(0, 0, 90);

                    HingeJoint j = spawnedSegmentGameObject.AddComponent<HingeJoint>();
                    j.connectedBody = jointObject.GetComponent<Rigidbody>();
                    j.axis = new Vector3(1 * otherReflectInt, 0, 0);
                    j.anchor = jointObject.transform.forward * (0.5f + dimVector.z / 2) / dimVector.z;
                    j.useMotor = true;
                    JointMotor motor = j.motor;
                    motor.targetVelocity = 0;
                    motor.force = 400;
                    j.motor = motor;

                    JointLimits limits = j.limits;
                    limits.min = -180f;
                    limits.bounciness = 0;
                    limits.bounceMinVelocity = 0;
                    limits.max = 180f;
                    j.limits = limits;
                    j.useLimits = true;
                    j.autoConfigureConnectedAnchor = false;
                }
                break;

            case (JointType.HingeY):
                {

                    GameObject jointObject = Instantiate(hingeJointPrefab, parentTransform.position, spawnAngle);

                    jointObject.transform.parent = c.transform;
                    jointObject.transform.localPosition = Vector3.zero;
                    FixedJoint jointObjectj = jointObject.AddComponent<FixedJoint>();
                    jointObjectj.autoConfigureConnectedAnchor = false;
                    jointObjectj.connectedBody = parentSegment.GetComponent<Rigidbody>();
                    jointPosition.x *= -1;
                    jointObjectj.anchor = jointPosition;

                    HingeJoint j = spawnedSegmentGameObject.AddComponent<HingeJoint>();
                    j.connectedBody = jointObject.GetComponent<Rigidbody>();
                    j.axis = new Vector3(0, 1 * otherReflectInt, 0);
                    j.anchor = jointObject.transform.forward * (0.5f + dimVector.z / 2) / dimVector.z;
                    j.useMotor = true;
                    JointMotor motor = j.motor;
                    motor.targetVelocity = 0;
                    motor.force = 400;
                    j.motor = motor;

                    JointLimits limits = j.limits;
                    limits.min = -180f;
                    limits.bounciness = 0;
                    limits.bounceMinVelocity = 0;
                    limits.max = 180f;
                    j.limits = limits;
                    j.useLimits = true;
                    j.autoConfigureConnectedAnchor = false;
                }
                break;

            case (JointType.HingeZ):
                {
                    GameObject jointObject = Instantiate(hingeJointPrefab, parentTransform.position, spawnAngle);

                    jointObject.transform.parent = c.transform;
                    jointObject.transform.localPosition = Vector3.zero;
                    FixedJoint jointObjectj = jointObject.AddComponent<FixedJoint>();
                    jointObjectj.autoConfigureConnectedAnchor = false;
                    jointObjectj.connectedBody = parentSegment.GetComponent<Rigidbody>();
                    jointPosition.z *= -1;
                    jointObjectj.anchor = jointPosition;
                    jointObject.transform.localRotation = Quaternion.Euler(90, 0, 0);

                    HingeJoint j = spawnedSegmentGameObject.AddComponent<HingeJoint>();
                    j.connectedBody = jointObject.GetComponent<Rigidbody>();
                    j.axis = new Vector3(0, 0, 1 * otherReflectInt);
                    j.anchor = jointObject.transform.right * (0.5f + dimVector.x / 2) / dimVector.x;
                    j.useMotor = true;
                    JointMotor motor = j.motor;
                    motor.targetVelocity = 0;
                    motor.force = 400;
                    j.motor = motor;

                    JointLimits limits = j.limits;
                    limits.min = -180f;
                    limits.bounciness = 0;
                    limits.bounceMinVelocity = 0;
                    limits.max = 180f;
                    j.limits = limits;
                    j.useLimits = true;
                    j.autoConfigureConnectedAnchor = false;
                }
                break;

            case (JointType.Configurable):
                {
                    ConfigurableJoint j = spawnedSegmentGameObject.AddComponent<ConfigurableJoint>();
                    j.connectedBody = parentSegment.GetComponent<Rigidbody>();
                    j.xMotion = ConfigurableJointMotion.Locked;
                    j.yMotion = ConfigurableJointMotion.Locked;
                    j.zMotion = ConfigurableJointMotion.Locked;
                    JointDrive jdx = j.angularXDrive;
                    jdx.positionSpring = 99999;
                    jdx.positionDamper = 99999;
                    j.angularXDrive = jdx;
                    JointDrive jdyz = j.angularYZDrive;
                    jdyz.positionSpring = 99999;
                    jdyz.positionDamper = 99999;
                    j.angularYZDrive = jdyz;
                    j.targetAngularVelocity = new Vector3(0, 0, 0);
                }
                break;

            case (JointType.Spherical):
				{
                    /*
                    GameObject spawnedJointGameObject = Instantiate(jointPrefab, spawnPos, spawnAngle);
                    spawnedJointGameObject.transform.parent = c.transform;

                    CharacterJoint j = spawnedJointGameObject.AddComponent<CharacterJoint>();
                    j.connectedBody = parentSegment.GetComponent<Rigidbody>();

                    FixedJoint j_parent = spawnedSegmentGameObject.AddComponent<FixedJoint>();
                    j_parent.connectedBody = spawnedJointGameObject.GetComponent<Rigidbody>();
                    */
                    CharacterJoint j = spawnedSegmentGameObject.AddComponent<CharacterJoint>();
                    j.connectedBody = parentSegment.GetComponent<Rigidbody>();
                    j.autoConfigureConnectedAnchor = false;
                    j.anchor = new Vector3(myConnection.anchorX, myConnection.anchorY, myConnection.anchorZ);

                }
                break;

            default:
                break;
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
    }

    private struct SpawnSegmentData
    {
        //Debug.Log("S: ROOT");

        // Find segmentGenotype
        SegmentGenotype currentSegmentGenotype = cg.GetSegment(1);
        if (currentSegmentGenotype == null)
            return;

        //Debug.Log(cg.name);
        //Debug.Log(cg.eulerY);
        cg.EulerToQuat(); //Debug, remove later (this changes internal rotation storage stuff to make inspector editing easier.)
        Quaternion spawnAngle = new Quaternion(cg.orientationX, cg.orientationY, cg.orientationZ, cg.orientationW);
        //Debug.Log(spawnAngle);
        GameObject spawnedSegmentGameObject = Instantiate(segmentPrefab, position, spawnAngle);
        spawnedSegmentGameObject.transform.parent = c.transform;
        spawnedSegmentGameObject.transform.localPosition = new Vector3(0, 2, 0);
        spawnedSegmentGameObject.name = $"Segment {currentSegmentGenotype.id}";

        Segment spawnedSegment = spawnedSegmentGameObject.GetComponent<Segment>();
        spawnedSegment.SetId(1);
        spawnedSegment.SetCreature(c);

        Vector3 dimVector = new Vector3(currentSegmentGenotype.dimensionX, currentSegmentGenotype.dimensionY, currentSegmentGenotype.dimensionZ);
        //spawnedSegment.GetComponent<BoxCollider>().size = dimVector;
        spawnedSegmentGameObject.transform.localScale = dimVector;
        Transform spawnedGraphic = spawnedSegmentGameObject.transform.Find("Graphic");
        //spawnedGraphic.localScale = dimVector;
        spawnedGraphic.GetComponent<Renderer>().material.color = new Color(currentSegmentGenotype.r / 255f, currentSegmentGenotype.g / 255f, currentSegmentGenotype.b / 255f);

        Rigidbody rb = spawnedSegmentGameObject.GetComponent<Rigidbody>();
        rb.mass *= dimVector.x * dimVector.y * dimVector.z;

        // Change recursiveLimit stuff
        bool runTerminalOnly = false;
        recursiveLimitValues[1]--;
        if (recursiveLimitValues[1] == 0 || !currentSegmentGenotype.connections.Any(scg => scg.destination == currentSegmentGenotype.id))
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
        return spawnedSegment;
    }

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + spawnPos, 0.1f);
    }
}