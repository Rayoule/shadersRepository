using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadManager : MonoBehaviour {

	public Vector2 velXconstraint;
	public Vector2 velYconstraint;
	public Vector2 velZconstraint;

	public Transform headBone;
	public Transform lookAtHead;
	public bool lookAtVelocity;
	[Range(0f,1f)] public float lookAtLerpFactor;
	private Quaternion headBaseWorldRotation;
	public List <Transform> boneChain;
	[HideInInspector] public List <Quaternion> baseBonesRotation;
	[SerializeField] private List <Vector3> poseIdle = new List <Vector3>();
	[SerializeField] private List <Vector3> poseForward = new List <Vector3>();
	[SerializeField] private List <Vector3> poseBack = new List <Vector3>();
	[SerializeField] private List <Vector3> poseUp = new List <Vector3>();
	[SerializeField] private List <Vector3> poseDown = new List <Vector3>();
	[SerializeField] private List <Vector3> poseRight = new List <Vector3>();
	[SerializeField] private List <Vector3> poseLeft = new List <Vector3>();

	private List <Quaternion> rotForward = new List <Quaternion>();
	private List <Quaternion> rotBack = new List <Quaternion>();
	private List <Quaternion> rotUp = new List <Quaternion>();
	private List <Quaternion> rotDown = new List <Quaternion>();
	private List <Quaternion> rotRight = new List <Quaternion>();
	private List <Quaternion> rotLeft = new List <Quaternion>();

	public GameObject playerGO;
	private CharacterManager characterManager;
	[Range(0f,1f)] public float inputLerp;
	[Range(0f,1f)] public float accelerationLerp;
	public float accelerationFactor;
	private Vector3 lerpedAcceleration;
	[Range(0f,1f)] public float HeadYOverlap;
	private float YDirLerped = 0f;
	private Vector3 inputLocalLerped, prevInputLocalLerped, inputGlobalLerped, inputAcceleration;


	void Start () {

		characterManager = playerGO.GetComponent<CharacterManager>();

		// fill baseBonesRotations
		for(int i=0; i < boneChain.Count; i++)
		{
			baseBonesRotation.Add(boneChain[i].localRotation);
		}

		// set base world rotation of head and first neck bone
		headBaseWorldRotation = headBone.rotation;

		rotForward = PoseToRotation(poseIdle, poseForward);
		rotBack = PoseToRotation(poseIdle, poseBack);
		rotUp = PoseToRotation(poseIdle, poseUp);
		rotDown = PoseToRotation(poseIdle, poseDown);
		rotRight = PoseToRotation(poseIdle, poseRight);
		rotLeft = PoseToRotation(poseIdle, poseLeft);
	}
	

	void LateUpdate () {
		
		// Lerp the input local and global and apply it to the head position and rotation
		Vector3 inputAxisLocal = Quaternion.Inverse(playerGO.transform.rotation) * characterManager.inputAxis;

		inputLocalLerped = Vector3.Lerp(inputLocalLerped, inputAxisLocal, inputLerp);
		inputGlobalLerped = Vector3.Lerp(inputGlobalLerped, characterManager.inputAxis, inputLerp);

		// version with acceleration
		Vector3 localAcceleration = Quaternion.Inverse(playerGO.transform.rotation) * characterManager.acceleration;
		localAcceleration *= accelerationFactor;
		lerpedAcceleration = Vector3.Lerp(lerpedAcceleration, localAcceleration, accelerationLerp);

		BlendRotationsXYZ(lerpedAcceleration*100, boneChain);

		if(lookAtVelocity) HeadLookAtVelocity(lookAtHead, headBone, inputGlobalLerped);
		HeadRotation(headBone, headBaseWorldRotation, lookAtHead.position);
	}


// the direction needs to be LOCAL
	void BlendRotationsXYZ (Vector3 direction, List <Transform> bones) {

		for(int i=0; i < bones.Count; i++)
		{
			Quaternion newRot;

			// Le premier bone est animé, les autres ne le sont pas et ne sont pas update par l'animator
			if(i == 0) newRot = bones[i].localRotation;
			else newRot = baseBonesRotation[i];

			// process Z position
			if(direction.z > 0.0f)
			{
				float lerpZ = direction.z / velZconstraint.y;
				newRot = Quaternion.Lerp(Quaternion.identity, rotForward[i], lerpZ) * newRot;
			}
			else if(direction.z < 0.0f)
			{
				float lerpZ = direction.z / velZconstraint.x;
				newRot = Quaternion.Lerp(Quaternion.identity, rotBack[i], lerpZ) * newRot;
			}

			// process X position
			if(direction.x > 0.0f)
			{
				float lerpX = direction.x / velXconstraint.y;
				newRot = Quaternion.Lerp(Quaternion.identity, rotRight[i], lerpX) * newRot;
			}
			else if(direction.x < 0.0f)
			{
				float lerpX = direction.x / velXconstraint.x;
				newRot = Quaternion.Lerp(Quaternion.identity, rotLeft[i], lerpX) * newRot;
			}

			YDirLerped = Mathf.Lerp(YDirLerped, direction.y, HeadYOverlap);

			// process Y position
			if(direction.y > 0.0f)
			{
				float lerpY = direction.y / velYconstraint.y;
				newRot = Quaternion.Lerp(Quaternion.identity, rotUp[i], lerpY) * newRot;
			}
			else if(direction.y < 0.0f)
			{
				float lerpY = direction.y / velYconstraint.x;
				newRot = Quaternion.Lerp(Quaternion.identity, rotDown[i], lerpY) * newRot;
			}

			// Apply
			bones[i].localRotation = newRot;
		}
	}


	void HeadRotation (Transform head, Quaternion baseWorldRot, Vector3 target) {

		if(lookAtHead)
		{
			Quaternion lookRot = Quaternion.LookRotation(head.position - target, Vector3.up);
			head.rotation = Quaternion.Lerp(head.rotation, lookRot, lookAtLerpFactor);
		} else head.rotation = baseWorldRot * Quaternion.Inverse(playerGO.transform.rotation);
	}

	void HeadLookAtVelocity (Transform target, Transform head, Vector3 direction) {

		float targetDist = 1f;

		Vector3 posToDir = head.position + (direction * targetDist) + playerGO.transform.forward * 0.001f;

		target.position = posToDir;
	}


	List <Quaternion> PoseToRotation (List <Vector3> idleVecList, List <Vector3> poseVecList) {

		List <Quaternion> quatList = new List <Quaternion> ();

		for(int i=0; i <= poseVecList.Count-1; i++)
		{
			Quaternion newRot = Quaternion.Inverse(Quaternion.Euler(idleVecList[i])) * Quaternion.Euler(poseVecList[i]);
			quatList.Add(newRot);
		}

		return quatList;
	}
}