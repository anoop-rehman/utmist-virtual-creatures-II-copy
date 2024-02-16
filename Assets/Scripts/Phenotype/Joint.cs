using UnityEngine;

public class CreatureJoint : MonoBehaviour
{
	public JointType jointType;
	public Vector3 dimVector;
	public Vector3 spawnPos;
	public JointFace childJointFace, parentJointFace;

	public double rotateLimit;

	public void SpawnJoint(GameObject hingeJointPrefab, GameObject sphereJointPrefab, GameObject parentSegment, Transform creature, out GameObject jointObject)
	{
		// TO-DO: May explode on spawn due to Vector3.zero
		if (jointType == JointType.Spherical || jointType == JointType.Fixed)
			jointObject = Instantiate(sphereJointPrefab, Vector3.zero, Quaternion.identity);
		else
			jointObject = Instantiate(hingeJointPrefab, Vector3.zero, Quaternion.identity);

		jointObject.transform.localScale = dimVector;
		jointObject.transform.parent = creature;
		jointObject.transform.localPosition = spawnPos;

		if (jointType == JointType.HingeX)
			jointObject.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
		else if (jointType == JointType.HingeZ)
			jointObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
		Debug.Log(jointObject.transform.localRotation.eulerAngles);
		FixedJoint parentToJointConn = jointObject.AddComponent<FixedJoint>();
		parentToJointConn.connectedBody = parentSegment.GetComponent<Rigidbody>();
		parentToJointConn.autoConfigureConnectedAnchor = false;
	}

	public void SetSize(Vector3 parentSize, Vector3 childSize)
	{
		dimVector.x = Mathf.Min(parentSize.x, childSize.x);
		dimVector.y = Mathf.Min(parentSize.y, childSize.y);
		dimVector.z = Mathf.Min(parentSize.z, childSize.z);
		float maxFloat = float.MaxValue;

		fitDimVector(ref dimVector.x, ref dimVector.z, ref maxFloat);

		if (jointType == JointType.Spherical || jointType == JointType.Fixed)
			fitDimVector(ref dimVector.x, ref dimVector.z, ref dimVector.y);
	}

	public void SetSpawnPos(Vector3 dimVector, Vector3 segmentSpawnPos, Transform parentSegmentTransform)
	{
		spawnPos = segmentSpawnPos;

		int shiftUp = 0, shiftRight = 0, shiftForward = 0;

		switch (parentJointFace)
		{
			case (JointFace.Top):
				shiftUp = 1;
				break;
			case (JointFace.Bottom):
				shiftUp = -1;
				break;
			case (JointFace.Right):
				shiftRight = 1;
				break;
			case (JointFace.Left):
				shiftRight = -1;
				break;
			case (JointFace.Front):
				shiftForward = 1;
				break;
			case (JointFace.Back):
				shiftForward = -1;
				break;
		}

		float dimensionOfInterest = 0f;

		switch(jointType)
        {
			case (JointType.HingeY):
				if (parentJointFace == JointFace.Top)
					dimensionOfInterest = dimVector.y;
				else if (parentJointFace == JointFace.Bottom)
					dimensionOfInterest = dimVector.y;
				else if (parentJointFace == JointFace.Left || parentJointFace == JointFace.Back)
					dimensionOfInterest = dimVector.x;
				else if (parentJointFace == JointFace.Right || parentJointFace == JointFace.Front)
					dimensionOfInterest = dimVector.x;
				break;
			case (JointType.HingeX):
				if (parentJointFace == JointFace.Top || parentJointFace == JointFace.Front)
					dimensionOfInterest = dimVector.x;
				else if (parentJointFace == JointFace.Bottom || parentJointFace == JointFace.Back)
					dimensionOfInterest = dimVector.x;
				else if (parentJointFace == JointFace.Left)
					dimensionOfInterest = dimVector.y;
				else if (parentJointFace == JointFace.Right)
					dimensionOfInterest = dimVector.y;
				break;
			case (JointType.HingeZ):
				if (parentJointFace == JointFace.Top || parentJointFace == JointFace.Right)
					dimensionOfInterest = dimVector.x;
				else if (parentJointFace == JointFace.Bottom || parentJointFace == JointFace.Left)
					dimensionOfInterest = dimVector.x;
				else if (parentJointFace == JointFace.Front)
					dimensionOfInterest = dimVector.y;
				else if (parentJointFace == JointFace.Back)
					dimensionOfInterest = dimVector.y;
				break;

		}

		spawnPos += parentSegmentTransform.forward * shiftForward * (parentSegmentTransform.localScale.z + dimensionOfInterest);
		spawnPos += parentSegmentTransform.right * shiftRight * (parentSegmentTransform.localScale.x + dimensionOfInterest);
		spawnPos += parentSegmentTransform.up * shiftUp * (parentSegmentTransform.localScale.y + dimensionOfInterest);

	}

	public void fitDimVector(ref float dimVectorFirst, ref float dimVectorSecond, ref float dimVectorThird)
	{
		float val = Mathf.Min(dimVectorFirst, dimVectorSecond, dimVectorThird);
		dimVectorFirst = val;
		dimVectorSecond = val;
		dimVectorThird = val;
	}
}
