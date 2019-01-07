using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassInverseKinematicsBehaviour : MonoBehaviour {

	
	public GameObject playerGO;
	public float influence;
	public string influenceParamName;
	// Store bones transforms
	public float baseArmatureScale;
	public List <Transform> armatureTransform = new List <Transform>();
	private List <IKbone> boneChain = new List <IKbone>();

	// angles to apply
	private List <Vector3> boneAngles = new List <Vector3>();
	public List <Vector2> minMaxAngleX = new List <Vector2>();
	public List <Vector2> minMaxAngleY = new List <Vector2>();
	public List <Vector2> minMaxAngleZ = new List <Vector2>();
	private List <Quaternion> baseRotation = new List <Quaternion>();
	private Quaternion firstBoneWorldRotation;
	private float maxReachLength;

	public float samplingDistance;
	public float learningRate;
	public float distanceThreshold;
	public int maxIterationsNb;
	public int maxIterationsOutOfRange;
	[HideInInspector] public Vector3 handAnimWorldPos;

	// Use this for initialization
	void Start () {

		for(int i=0; i <= armatureTransform.Count-1; i++)
		{
			// Assign the joint transform with the class IKbone
			IKbone bone = armatureTransform[i].gameObject.AddComponent<IKbone>();
			bone.EditValues(armatureTransform[i], true, true, true, minMaxAngleX[i], minMaxAngleY[i], minMaxAngleZ[i]);
			boneChain.Add(bone);

			if(i != 0)
			{
				maxReachLength += Vector3.Distance(boneChain[i-1].boneTransform.position, boneChain[i].boneTransform.position);
			}

		}

		// Get base rotations of bone chain
		for(int i=0; i<boneChain.Count; i++)
		{
			baseRotation.Add(boneChain[i].boneTransform.localRotation);

			//Full List <Vector3> boneAngles with empty Vector3
			boneAngles.Add(Vector3.zero);
		}
		firstBoneWorldRotation = boneChain[0].boneTransform.rotation;
		//Debug.Log(boneChain.Count);
		
	}
	



	// Update is called once per frame
	void LateUpdate () {

		Debug.DrawLine(boneChain[0].boneTransform.position, ForwardKinematics(boneAngles), Color.red);

		handAnimWorldPos = armatureTransform[armatureTransform.Count-1].position;

		InverseKinematics(this.transform.position, boneAngles);
		ApplyAngles(boneAngles);
		
	}




	public Vector3 PartialGradient (Vector3 target, List <Vector3> angles, int i) {

		Vector3 gradient = Vector3.zero;

		if(boneChain[i].xAxis)
		{
			Vector3 angle = angles[i];

			float f_x = DistanceFromTarget(target, angles);

			angles[i] += new Vector3(samplingDistance,0,0);
			float f_x_plus_d = DistanceFromTarget(target, angles);

			gradient.x = (f_x_plus_d - f_x) / samplingDistance;

			angles[i] = angle;
		}

		if(boneChain[i].yAxis)
		{
			Vector3 angle = angles[i];

			float f_x = DistanceFromTarget(target, angles);

			angles[i] += new Vector3(0,samplingDistance,0);
			float f_x_plus_d = DistanceFromTarget(target, angles);

			gradient.y = (f_x_plus_d - f_x) / samplingDistance;

			angles[i] = angle;
		}

		if(boneChain[i].zAxis)
		{
			Vector3 angle = angles[i];

			float f_x = DistanceFromTarget(target, angles);

			angles[i] += new Vector3(0,0,samplingDistance);
			float f_x_plus_d = DistanceFromTarget(target, angles);

			gradient.z = (f_x_plus_d - f_x) / samplingDistance;

			angles[i] = angle;
		}

		return gradient;
	}




	public void InverseKinematics (Vector3 target, List <Vector3> angles) {
		int itCount = 0;
		int maxItThisFrame;

		if(DistanceFromTarget(transform.position, boneAngles) <= distanceThreshold)
		{
			return;
		}

		if(CheckOutOfRange(transform.position, boneChain[0].boneTransform.position, maxReachLength))
		{
			maxItThisFrame = maxIterationsOutOfRange;
		} else maxItThisFrame = maxIterationsNb;


		for(int a=0; a< maxItThisFrame; a++)
		{
			for(int i = boneChain.Count-1; i >= 0f; i --)
			{
				Vector3 gradient = PartialGradient(target, angles, i);
				angles[i] -= new Vector3(learningRate * gradient.x, learningRate * gradient.y, learningRate * gradient.z);

				// clamp angles
				Vector3 angleList = angles[i];
				angleList.x = Mathf.Clamp(angles[i].x, boneChain[i].minMaxAngleX.x, boneChain[i].minMaxAngleX.y);
				angleList.y = Mathf.Clamp(angles[i].y, boneChain[i].minMaxAngleY.x, boneChain[i].minMaxAngleY.y);
				angleList.z = Mathf.Clamp(angles[i].z, boneChain[i].minMaxAngleZ.x, boneChain[i].minMaxAngleZ.y);
				angles[i] = angleList;

				//Early termination
				if(DistanceFromTarget(transform.position, boneAngles) <= distanceThreshold) {
					return;
				}
			}
			itCount ++;
		}
	}




	public Vector3 ForwardKinematics (List <Vector3> angles) {

		Vector3 prevPoint = boneChain[0].boneTransform.position;
		Quaternion rotation = firstBoneWorldRotation;

		for (int i = 1; i < boneChain.Count; i++)
		{
			// Rotates around a new axis
			if(boneChain[i-1].xAxis)
				rotation *= Quaternion.AngleAxis(angles[i - 1].x, new Vector3(1,0,0));
			if(boneChain[i-1].yAxis)
				rotation *= Quaternion.AngleAxis(angles[i - 1].y, new Vector3(0,1,0));
			if(boneChain[i-1].zAxis)
				rotation *= Quaternion.AngleAxis(angles[i - 1].z, new Vector3(0,0,1));

			Vector3 nextPoint = prevPoint + rotation * boneChain[i].boneTransform.localPosition * baseArmatureScale;
			
			prevPoint = nextPoint;
		}

		return prevPoint;
	}




	public float DistanceFromTarget(Vector3 target, List <Vector3> angles) {
		Vector3 point = ForwardKinematics(angles);
		return Vector3.Distance(target, point);
	}



	// returns true when target is out of range
	public bool CheckOutOfRange (Vector3 target, Vector3 rootBone, float boneChainMaxLength) {

		float distRootTarget = Vector3.Distance(rootBone, target);

		if(maxReachLength < distRootTarget)
		{
			return true;
		} else return false;
	}

	public void ApplyAngles(List <Vector3> angles) {
		for(int i=0; i<=boneChain.Count-1; i++)
		{
			Quaternion initialRot = boneChain[i].boneTransform.localRotation;
			Quaternion initialFBwRot = boneChain[0].boneTransform.rotation;

			Quaternion addRot = Quaternion.identity;


			if(boneChain[i].xAxis)
				addRot *= Quaternion.AngleAxis(angles[i].x, playerGO.transform.rotation * new Vector3(1,0,0));
			if(boneChain[i].yAxis)
				addRot *= Quaternion.AngleAxis(angles[i].y, playerGO.transform.rotation * new Vector3(0,1,0));
			if(boneChain[i].zAxis)
				addRot *= Quaternion.AngleAxis(angles[i].z, playerGO.transform.rotation * new Vector3(0,0,1));


			if (i==0) {
			addRot = (playerGO.transform.rotation * firstBoneWorldRotation) * addRot;
			//addRot = Quaternion.Slerp(initialFBwRot, addRot, influence); // INFLUENCE
			boneChain[0].boneTransform.rotation = addRot;
			}
			else {
			//addRot = (playerGO.transform.rotation * firstBoneWorldRotation) * addRot;
			//addRot = Quaternion.Slerp(initialRot, addRot, influence); // INFLUENCE
			boneChain[i].boneTransform.localRotation = addRot;
			}
		}
	}

}
