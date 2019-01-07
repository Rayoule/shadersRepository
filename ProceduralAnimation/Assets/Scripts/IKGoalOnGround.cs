using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKGoalOnGround : MonoBehaviour {

	public GameObject playerGO;
	public float raycastOffset;
	public float raycastLength;
	public float threshold;
	private Transform lastBoneTransform;
	//private Vector3 footAnimPos;
	private List <Transform> bones = new List <Transform>();
	private AnimatorManager animScript;
	private ClassInverseKinematicsBehaviour IKScript;
	private Vector3 ground;
	//private RaycastHit hit;

	// Use this for initialization
	void Start () {
		
		bones = GetComponent<ClassInverseKinematicsBehaviour>().armatureTransform;
		lastBoneTransform = bones[bones.Count-1].transform;
		animScript = playerGO.GetComponent<AnimatorManager>();
		IKScript = GetComponent<ClassInverseKinematicsBehaviour>();
	}

	void LateUpdate () {
		CheckGround();
	}
	
	// Update is called once per frame
	void CheckGround () {

		float getInfluence = animScript.GetFloatParam(IKScript.influenceParamName);
		getInfluence =Mathf.Clamp(getInfluence, 0.01f, 0.99f);

		if(getInfluence < 0.05f)
		{
			ground = HitRaycast(); // Raycast

			float dist = Mathf.Abs(ground.y - IKScript.handAnimWorldPos.y);
			//Debug.DrawRay(IKScript.handAnimWorldPos, Vector3.up * 1000, Color.yellow);
			if(ground != Vector3.zero && dist > threshold) // checke le threshold pour laisser l'anime faire quand c'est plat
			{
				// L'IK va etre appliquée en fonction de l'inflence
				transform.position = ground;
				IKScript.influence = getInfluence;
			}
			else
			{
				// Pas d'effet d'IK
				IKScript.influence = 0.01f;
			}
		}
		
	}

	private Vector3 HitRaycast() {
		RaycastHit[] hits;

		Vector3 origin = IKScript.handAnimWorldPos + (Vector3.up * raycastOffset);

		hits = Physics.RaycastAll(origin, Vector3.down, raycastLength);

		Vector3 hitPoint = Vector3.zero;

		for(int i=0; i < hits.Length; i++)
		{
			if(hits[i].transform.tag != "Player")
			{
				hitPoint = hits[i].point;
			}
		}
		
		//Debug.Log(hitPoint);
		return hitPoint;
	}

}
