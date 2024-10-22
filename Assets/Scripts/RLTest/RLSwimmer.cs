using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

// code example by mxu
// REFERENCES:
// https://docs.unity3d.com/ScriptReference/HingeJoint-motor.html
// https://www2.gwu.edu/~phy21bio/Reading/Purcell_life_at_low_reynolds_number.pdf

public class RLSwimmer : Agent
{
    public GameObject leftSegment;
    public GameObject rightSegment;

    public Vector3 start_loc;

    public float motorForce = 200f;
    public float speed = 90f;

    public float t_left_angle_ = 0f;
    public float t_right_angle_ = 0f;

    public GameObject self_dup;
    public GameObject target;

    Vector3 lastpos_p = new Vector3();
    Vector3 lastpos_l = new Vector3();
    Vector3 lastpos_r = new Vector3();
    Vector3 lastpos_m = new Vector3();

    float timeout = 20f;
    private Stopwatch SW = new Stopwatch();

    void Start()
    {
        lastpos_p = transform.parent.transform.position;
        lastpos_l = leftSegment.transform.position;
        lastpos_r = rightSegment.transform.position;
        lastpos_m = transform.position;
    }

    public override void OnEpisodeBegin() 
    {
        //transform.parent.transform.position = lastpos_p;
        //leftSegment.transform.position = lastpos_l;
        //rightSegment.transform.position = lastpos_r;
        //transform.position = lastpos_m;

        Rigidbody rb = transform.GetComponent<Rigidbody>();
        //float mag_velocity = rb.velocity.magnitude;
        float temp_reward = 1f / (Vector3.Distance(target.transform.position, transform.position) + 1f);
        UnityEngine.Debug.Log(temp_reward);
        SW.Reset();
        SW.Start();
        // Instantiate(self_dup, lastpos_p, Quaternion.identity);
        return;
    }

    private void SetJointToTargetAngle(HingeJoint hingeJoint, float targetAngle)
    {
        float leftDirection = Mathf.Sign(targetAngle - hingeJoint.angle);
        JointMotor motor = hingeJoint.motor;
        motor.force = motorForce;
        motor.targetVelocity = speed * leftDirection;
        motor.freeSpin = false;
        hingeJoint.motor = motor;
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        HingeJoint leftJoint = leftSegment.GetComponent<HingeJoint>();
        HingeJoint rightJoint = rightSegment.GetComponent<HingeJoint>();
        Rigidbody rb = transform.GetComponent<Rigidbody>();
        float mag_velocity = rb.velocity.magnitude;
        Vector3 input = new Vector3(leftJoint.angle,rightJoint.angle,mag_velocity);
        // UnityEngine.Debug.Log(input);
        sensor.AddObservation(input);
        sensor.AddObservation(target.transform.position);
        sensor.AddObservation(transform.position);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        t_left_angle_ = actions.ContinuousActions[0] + 1;
        t_right_angle_ = actions.ContinuousActions[1] + 1;
        HingeJoint leftJoint = leftSegment.GetComponent<HingeJoint>();
        HingeJoint rightJoint = rightSegment.GetComponent<HingeJoint>();
        // UnityEngine.Debug.Log(t_left_angle_);
        // UnityEngine.Debug.Log(t_right_angle_);
        SetJointToTargetAngle(leftJoint, t_left_angle_*180);
        SetJointToTargetAngle(rightJoint, t_right_angle_*180);
        
    }

    // public override void Heuristic(in ActionBuffers actionsOut)
    // {
    //     ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
    //     continuousActions[0] = Random.Range(-1f,1f);
    //     continuousActions[1] = Random.Range(-1f,1f);
    // }


    private void FixedUpdate()
    {
        // UnityEngine.Debug.Log(SW.ElapsedMilliseconds);
        SW.Stop();
        Rigidbody rb = transform.GetComponent<Rigidbody>();
        float mag_velocity = rb.velocity.magnitude;
        float temp_reward = 1f / (Vector3.Distance(target.transform.position, transform.position) + 1f);
        AddReward(temp_reward);
        if (SW.ElapsedMilliseconds >= timeout * 1000f)
        {
            EndEpisode();
            Destroy(transform.parent.gameObject);
        }
        SW.Start();
    }
}