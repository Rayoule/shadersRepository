using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InverseKinematicsBehaviour : MonoBehaviour {

	
	// Store bones transforms
	public float baseArmatureScale;
	[SerializeField] private List <Transform> armatureTransform = new List <Transform>();
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

	// Use this for initialization
	void Start () {


		// VERSION PORIGINAL
		/*for(int i=0; i <= armatureTransform.Count-1; i++)
		{
			// Assign the joint transform with the class IKbone
			IKbone bone = new IKbone();
			bone.EditValues(armatureTransform[i], false, false, true, minMaxAngleX[i], minMaxAngleY[i], minMaxAngleZ[i]);
			boneChain.Add(bone);

			if(i != 0)
			{
				maxReachLength += Vector3.Distance(boneChain[i-1].boneTransform.position, boneChain[i].boneTransform.position);
			}

		}*/



		// NEW VERSION
		for(int i=0; i <= armatureTransform.Count-1; i++)
		{
			// Assign the joint transform with the class IKbone
			IKbone bone = armatureTransform[i].gameObject.AddComponent<IKbone>();
			bone.EditValues(armatureTransform[i], false, false, true, minMaxAngleX[i], minMaxAngleY[i], minMaxAngleZ[i]);
			boneChain.Add(bone);

			if(i != 0)
			{
				maxReachLength += Vector3.Distance(boneChain[i-1].boneTransform.position, boneChain[i].boneTransform.position);
			}

		}

		// Get base rotations of bone chain
		for(int i=0; i<= boneChain.Count-1; i++)
		{
			baseRotation.Add(boneChain[i].boneTransform.localRotation);

			//Full List <Vector3> boneAngles with empty Vector3
			boneAngles.Add(Vector3.zero);
		}
		firstBoneWorldRotation = boneChain[0].boneTransform.rotation;
		Debug.Log(boneChain.Count);
		
	}
	



	// Update is called once per frame
	void LateUpdate () {

		Debug.DrawLine(boneChain[0].boneTransform.position, ForwardKinematics(boneAngles), Color.red);

		InverseKinematics(this.transform.position, boneAngles);
		ApplyAngles(boneAngles);
		
	}




	public float PartialGradient (Vector3 target, List <Vector3> angles, int i) {
		
		Vector3 angle = angles[i];

		float f_x = DistanceFromTarget(target, angles);

		angles[i] += new Vector3(samplingDistance, samplingDistance, samplingDistance);
		float f_x_plus_d = DistanceFromTarget(target, angles);

		float gradient = (f_x_plus_d - f_x) / samplingDistance;

		angles[i] = angle;

		return gradient;
	}




	public void InverseKinematics (Vector3 target, List <Vector3> angles) {
		int itCount = 0;
		int maxItThisFrame;

		if(DistanceFromTarget(transform.position, boneAngles) <= distanceThreshold)
		{
			//Debug.Log("Aborted: no need to move");
			return;
		}

		if(CheckOutOfRange(transform.position, boneChain[0].boneTransform.position, maxReachLength))
		{
			maxItThisFrame = maxIterationsOutOfRange;
			//Debug.Log("Out of Range");
		} else maxItThisFrame = maxIterationsNb;


		for(int a=0; a< maxItThisFrame; a++)
		{
			for(int i = boneChain.Count-1; i >= 0f; i --)
			{
				float gradient = PartialGradient(target, angles, i);
				angles[i] -= new Vector3(learningRate * gradient, learningRate * gradient, learningRate * gradient);

				// clamp angles
				Vector3 angleList = angles[i];
				angleList.x = Mathf.Clamp(angles[i].x, boneChain[i].minMaxAngleX.x, boneChain[i].minMaxAngleX.y);
				angleList.y = Mathf.Clamp(angles[i].y, boneChain[i].minMaxAngleY.x, boneChain[i].minMaxAngleY.y);
				angleList.z = Mathf.Clamp(angles[i].z, boneChain[i].minMaxAngleZ.x, boneChain[i].minMaxAngleZ.y);
				angles[i] = angleList;

				//Early termination
				if(DistanceFromTarget(transform.position, boneAngles) <= distanceThreshold) {
					//Debug.Log(itCount + " iterations");
					return;
				}
			}
			itCount ++;
		}

		//Debug.Log("not solved...");
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



	public void ApplyAngles(List <Vector3> angles) {
		for(int i=0; i<=boneChain.Count-1; i++)
		{
			Quaternion allRotations = Quaternion.identity;

			if(boneChain[i].xAxis)
				allRotations *= Quaternion.AngleAxis(angles[i].x, new Vector3(1,0,0));
			if(boneChain[i].yAxis)
				allRotations *= Quaternion.AngleAxis(angles[i].y, new Vector3(0,1,0));
			if(boneChain[i].zAxis)
				allRotations *= Quaternion.AngleAxis(angles[i].z, new Vector3(0,0,1));

			boneChain[i].boneTransform.localRotation = baseRotation[i] * allRotations;
		}
	}



	public float DistanceFromTarget(Vector3 target, List <Vector3> angles) {
		Vector3 point = ForwardKinematics(angles);
		return Vector3.Distance(target, point);
	}



// If true, then target is out of range
	public bool CheckOutOfRange (Vector3 target, Vector3 rootBone, float boneChainMaxLength) {
		float distRootTarget = Vector3.Distance(rootBone, target);
		if(maxReachLength < distRootTarget)
		{
			return true;
		} else return false;
	}

}
