using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FKspineFollow : MonoBehaviour {

	public Transform target;
	[Range(-1f,1f)] public float leftRight;
	public float maxRotation;
	public List <Transform> boneChain;
	public List <Quaternion> baseRotation;
	public GameObject playerGO;
	private CharacterManager characterManager;
	private Vector3 prevNormVel;


	// Use this for initialization
	void Start () {
		baseRotation = GetRotations(boneChain);
		characterManager = playerGO.GetComponent<CharacterManager>();
	}
	
	// Update is called once per frame
	void LateUpdate () {

		Vector3 normVel = Vector3.Normalize(characterManager.publicVelocity);
		float ratio = characterManager.publicVelocity.sqrMagnitude / Mathf.Pow(characterManager.velocityLimit.y,2);
		normVel = Vector3.Lerp(prevNormVel, normVel, Mathf.Sqrt(ratio));

		/*if(normVel == Vector3.zero)
		{
			normVel = Vector3.Lerp(prevNormVel, normVel, spineLerp);
			//normVel = prevNormVel;
		}*/
		leftRight = LeftRight(playerGO.transform.right, normVel);

		prevNormVel = normVel;

		OrientSpine(Vector3.up, leftRight, maxRotation, boneChain);
	}

	public float LeftRight (Vector3 wForward, Vector3 wDirection) {
		return Vector3.Dot(wForward, wDirection);
	}

	public void OrientSpine(Vector3 axis, float leftRightFactor, float maxRot, List <Transform> bones) {

		float angle = (maxRot * leftRightFactor) / (bones.Count-1);
		Vector3 rotation = axis * angle;

		for(int i=0; i<bones.Count-1; i++)
		{
			bones[i].Rotate(rotation, Space.World);
		}
	}

	public List <Quaternion> GetRotations(List <Transform> bones) {
		List <Quaternion> rots = new List <Quaternion>();
		for(int i=0; i<bones.Count-1; i++)
		{
			rots.Add(bones[i].localRotation);
		}
		return rots;
	}
}
