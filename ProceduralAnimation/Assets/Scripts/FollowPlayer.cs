using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour {

	public GameObject player;
	private Vector3 baseDistance;
	private Vector3 newPos, basePos;
	private Quaternion newRot, baseRot;
	public GameObject cameraTarget;

	public bool followTarget;

	[Range(0f,1f)] public float positionLerp;
	[Range(0f,1f)] public float rotationLerp;

	// Use this for initialization
	void Start () {

		// base distance
		baseDistance = transform.position - player.transform.position;

		// keepsValues
		newRot = transform.localRotation;
		baseRot = newRot;

		// Crée une camera target en child du player
		cameraTarget = Instantiate(new GameObject(), transform.position, transform.localRotation);
		cameraTarget.name = "camera_Target";
		cameraTarget.transform.parent = player.transform;
	}
	
	// Update is called once per frame
	void Update () {

		newPos = Vector3.zero;

		if (followTarget) {
			newPos = cameraTarget.transform.position;
			newRot = cameraTarget.transform.rotation;
		} else {
			newPos = player.transform.position + baseDistance;
			newRot = baseRot;
		}

		transform.position = Vector3.Lerp(transform.position, newPos, positionLerp);
		transform.localRotation = Quaternion.Lerp(transform.localRotation, newRot, rotationLerp);
	}
}
