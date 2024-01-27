using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidbodyState
{

    public Vector3 velocity;
    public Vector3 angularVelocity;
    public float drag;
    public float angularDrag;
    public float mass;
    public bool useGravity;
    public bool freezeRotation;
    public Vector3 centerOfMass;
    public Quaternion inertiaTensorRotation;
    public Vector3 inertiaTensor;
    public bool detectCollisions;
    public Vector3 position;
    public Quaternion rotation;
    public RigidbodyInterpolation interpolation;

    public RigidbodyState(Rigidbody rb)
    {
        velocity = rb.velocity;
        angularVelocity = rb.angularVelocity;
        drag = rb.drag;
        angularDrag = rb.angularDrag;
        mass = rb.mass;
        useGravity = rb.useGravity;
        freezeRotation = rb.freezeRotation;
        centerOfMass = rb.centerOfMass;
        inertiaTensorRotation = rb.inertiaTensorRotation;
        inertiaTensor = rb.inertiaTensor;
        detectCollisions = rb.detectCollisions;
        position = rb.position;
        rotation = rb.rotation;
        interpolation = rb.interpolation;
    }

    public RigidbodyState()
    {
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;
        drag = 0f;
        angularDrag = 0.05f;
        mass = 1f;
        useGravity = true;
        freezeRotation = false;
        centerOfMass = Vector3.zero;
        detectCollisions = true;
        position = Vector3.zero;
        rotation = Quaternion.identity;
        interpolation = RigidbodyInterpolation.None;
    }

    public void SetRigidbody(Rigidbody rb)
    {
        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;
        rb.drag = drag;
        rb.angularDrag = angularDrag;
        rb.mass = mass;
        rb.useGravity = useGravity;
        rb.freezeRotation = freezeRotation;
        rb.centerOfMass = centerOfMass;
        // rb.inertiaTensorRotation = inertiaTensorRotation;
        // rb.inertiaTensor = inertiaTensor;
        rb.detectCollisions = detectCollisions;
        rb.position = position;
        rb.rotation = rotation;
        rb.interpolation = interpolation;
    }
}

public class Segment : MonoBehaviour
{
    public byte id { get; private set; }
    public Dictionary<byte, Segment> children { get; private set; }
    public System.Tuple<byte, Segment> parent { get; private set; }
    public List<Neuron> neurons;
    public Creature creature { get; private set; }

    private RigidbodyState storedState;

    public bool isTopEmpty;
    public bool isBottomEmpty;
    public bool isRightEmpty;
    public bool isLeftEmpty;
    public bool isFrontEmpty;
    public bool isBackEmpty;

    public FixedJoint fixedJoint;
    public new HingeJoint hingeJoint;
    public ConfigurableJoint sphericalJoint;
    public Rigidbody myRigidbody;
    public float jointAxisX;
    public float jointAxisY;
    public float jointAxisZ;

    public List<byte> path { get; private set; }

    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody>();
        Initialize();
    }

    /// <summary>
    /// Called to initialize the segment when it is called from the object pool.
    /// </summary>
    public void Initialize()
    {
        // Reset transform
        Transform t = transform;
        t.position = Vector3.zero;
        t.rotation = Quaternion.identity;
        t.localScale = Vector3.one;

        hingeJoint = GetComponent<HingeJoint>();
        jointAxisX = 0;
        jointAxisY = 0;
        jointAxisZ = 0;
        storedState = new RigidbodyState();
        
        
        RestoreState();
        myRigidbody.ResetCenterOfMass();
        myRigidbody.ResetInertiaTensor();

        // Reset creature data
        id = 0;
        creature = null;
        parent = null;
        children = new Dictionary<byte, Segment>();
        neurons = new List<Neuron>();
        path = new List<byte>();
    }

    public void Release()
    {
        DetachJoints();
    }

    /// <summary>
    /// Removes all joints. Used in Release().
    /// </summary>
    private void DetachJoints()
    {
        if (hingeJoint != null)
        {
            Destroy(hingeJoint);
        }
        if (fixedJoint != null)
        {
            Destroy(fixedJoint);
        }
    }

    public void AttachFixedJoint(Rigidbody parentRigidbody)
    {
        fixedJoint = gameObject.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = parentRigidbody;
    }

    public void AttachHingeJoint(Vector3 axis, Rigidbody parentRigidbody)
    {
        if (hingeJoint != null) Destroy(hingeJoint);
        hingeJoint = gameObject.AddComponent<HingeJoint>();
        hingeJoint.connectedBody = parentRigidbody;
        hingeJoint.axis = axis;
        hingeJoint.useMotor = true;
        JointMotor motor = hingeJoint.motor;
        motor.targetVelocity = 0;
        motor.force = 400;
        hingeJoint.motor = motor;

        JointLimits limits = hingeJoint.limits;
        limits.min = -60f;
        limits.bounciness = 0;
        limits.bounceMinVelocity = 0;
        limits.max = 60f;
        hingeJoint.limits = limits;
        hingeJoint.useLimits = true;
    }

    public void AttachSphericalJoint(Rigidbody parentRigidbody)
    {
        sphericalJoint = gameObject.AddComponent<ConfigurableJoint>();
        sphericalJoint.connectedBody = parentRigidbody;
        sphericalJoint.xMotion = ConfigurableJointMotion.Locked;
        sphericalJoint.yMotion = ConfigurableJointMotion.Locked;
        sphericalJoint.zMotion = ConfigurableJointMotion.Locked;
        JointDrive jdx = sphericalJoint.angularXDrive;
        jdx.positionSpring = 99999;
        jdx.positionDamper = 99999;
        sphericalJoint.angularXDrive = jdx;
        JointDrive jdyz = sphericalJoint.angularYZDrive;
        jdyz.positionSpring = 99999;
        jdyz.positionDamper = 99999;
        sphericalJoint.angularYZDrive = jdyz;
        sphericalJoint.targetAngularVelocity = new Vector3(0, 0, 0);
    }

    private void FixedUpdate()
    {
        isTopEmpty = true;
        isBottomEmpty = true;
        isRightEmpty = true;
        isLeftEmpty = true;
        isFrontEmpty = true;
        isBackEmpty = true;

        if (hingeJoint != null)
        {
            Vector3 angles = (transform.localRotation * Quaternion.Inverse(hingeJoint.connectedBody.transform.localRotation)).eulerAngles;
            //Vector3 angles = Quaternion.FromToRotation(joint.connectedBody.transform.rotation.eulerAngles, transform.rotation.).eulerAngles;
            jointAxisX = angles.x;
            jointAxisY = angles.y;
            jointAxisZ = angles.z;

        }
    }

    public void StoreState(){
        storedState = new RigidbodyState(myRigidbody);
    }

    public void RestoreState(){
        storedState.SetRigidbody(myRigidbody);
    }

    public void SetId(byte id){
        this.id = id;
    }

    public void SetPath(List<byte> path){
        this.path = path;
    }

    public void SetParent(byte connectionId, Segment s){
        parent = new System.Tuple<byte, Segment>(connectionId, s);
    }

    public void SetCreature(Creature c){
        creature = c;
    }

    public void AddChild(byte connectionId, Segment s){
        children.Add(connectionId, s);
    }

    public void AddNeuron(Neuron n){
        neurons.Add(n);
    }

    public List<float> GetObservations(){
        // add 12 observations of the sensor to list, and return
        List<float> obs = new List<float>();
        obs.Add(GetContact("Right"));
        obs.Add(GetContact("Left"));
        obs.Add(GetContact("Top"));
        obs.Add(GetContact("Bottom"));
        obs.Add(GetContact("Front"));
        obs.Add(GetContact("Back"));
        obs.Add(jointAxisX);
        obs.Add(jointAxisY);
        obs.Add(jointAxisZ);
        obs.Add(GetPhotosensor(0));
        obs.Add(GetPhotosensor(1));
        obs.Add(GetPhotosensor(2));

        return obs;
    }

    public sbyte GetContact(string name)
    {
        bool value = name switch
        {
            "Top" => isTopEmpty,
            "Bottom" => isBottomEmpty,
            "Right" => isRightEmpty,
            "Left" => isLeftEmpty,
            "Front" => isFrontEmpty,
            "Back" => isBackEmpty,
            _ => true
        };
        return (sbyte)(value ? -1 : 1);
    }

    public float GetPhotosensor(int varNumber)
    {
        
        string name = creature.gameObject.transform.parent.parent.gameObject.name;
        GameObject environmentObj = creature.gameObject.transform.parent.parent.gameObject;
        GameObject photosensorObj = null;
        foreach (Transform childTransform in environmentObj.transform)
        {
            GameObject childObj = childTransform.gameObject;
            if (childObj.CompareTag("Photosensor"))
            {
                photosensorObj = childObj;
            }
        }
        Light lightsource = photosensorObj.GetComponent<Light>();
        if (lightsource == null)
        {
            return 0;
        }
        else
        {
            Vector3 position = lightsource.transform.position;
            Vector3 normalVector = (position - transform.position).normalized;
            return varNumber switch
            {
                0 => normalVector.x,
                1 => normalVector.y,
                2 => normalVector.z,
                _ => 0,
            };
        }
    }

    public void HandleStay(Collider other, string name)
    {
        if (other.gameObject.layer != 6)
        {
            switch (name)
            {
                case ("Top"):
                    isTopEmpty = false;
                    break;
                case ("Bottom"):
                    isBottomEmpty = false;
                    break;
                case ("Right"):
                    isRightEmpty = false;
                    break;
                case ("Left"):
                    isLeftEmpty = false;
                    break;
                case ("Front"):
                    isFrontEmpty = false;
                    break;
                case ("Back"):
                    isBackEmpty = false;
                    break;
            }
        }
    }
}
