using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngularOverlap : MonoBehaviour {

	public GameObject playerGO;
	public List <Transform> bones = new List <Transform>();
	public List <Vector3> bonesDirection = new List <Vector3>();
	public List <Quaternion> baseRotation = new List <Quaternion>();
	private List <Vector3> angularVelocity = new List <Vector3>();
	public float threshold;
	public float rotateForceIntensity;
	public float maxForce;
	public float drag;
	public Vector3 debugForce;
	[Range(0f,1f)] public float accelLerp;
	private Vector3 baseBoneVelocity, baseBoneAcceleration, lerpedBaseBoneAcceleration;
	private Vector3 pervBaseBonePosition, pervBaseBoneVelocity;
	void Start () {
		
		// Setup lists
		for(int i=0; i < bones.Count-1; i++) {
			angularVelocity.Add(Vector3.zero);
			bonesDirection.Add(Vector3.zero);
			baseRotation.Add(bones[i].localRotation);
		}
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {

		// Get Base Bone Acceleration in wSpace
		baseBoneVelocity = bones[0].position - pervBaseBonePosition;
		baseBoneAcceleration = baseBoneVelocity - pervBaseBoneVelocity;

		lerpedBaseBoneAcceleration = Vector3.Lerp(lerpedBaseBoneAcceleration, baseBoneAcceleration, Mathf.Clamp(accelLerp, 0.00001f, 0.99999f));
		//lerpedBaseBoneAcceleration = -lerpedBaseBoneAcceleration; // Inverser l'acceleration et créer la force d'overlap
		//if(lerpedBaseBoneAcceleration.sqrMagnitude < threshold*threshold) lerpedBaseBoneAcceleration = Vector3.zero;
		Vector3 nLerpedBaseBoneAcceleration = Vector3.Normalize(lerpedBaseBoneAcceleration);
		Debug.DrawRay(transform.position, lerpedBaseBoneAcceleration* 1000, Color.yellow);

		float clampedForce = Mathf.Clamp(lerpedBaseBoneAcceleration.magnitude / maxForce, 0,1);
		//Debug.Log(lerpedBaseBoneAcceleration.magnitude);


			//Get current bones direction
			//bonesDirection[i] = Vector3.Normalize(bones[i+1].position - bones[i].position);

			// process angular velocity
			/*Quaternion addRotation = Quaternion.FromToRotation(bones[i].forward, lerpedBaseBoneAcceleration);
			addRotation = Quaternion.Slerp(bones[i].rotation, bones[i].rotation * addRotation, clampedForce);
			Debug.Log(clampedForce);*/
			Vector3 localRotateForce = Quaternion.Inverse(playerGO.transform.rotation) * debugForce;
			float thisForce = 0f;
			thisForce += Vector3.Dot(localRotateForce, Vector3.forward);
			thisForce += Vector3.Dot(localRotateForce, Vector3.up);
			thisForce /= 2;
			//Vector3 localRotations = bones[i].InverseTransformDirection(rotations);

			// Add drag to get back to initial pose
			//Vector3 FinalRotation = baseRotation[i].eulerAngles - localRotations;

			// Apply
			for(int i=0; i < bones.Count-1; i++) {
				float boneForce = thisForce / (i+1);
				bones[i].Rotate(Quaternion.Inverse(playerGO.transform.rotation) * (thisForce * Vector3.right), Space.World);
			}


		pervBaseBonePosition = bones[0].position; // update previous position & velocity
		pervBaseBoneVelocity = baseBoneVelocity;
		
	}

}
