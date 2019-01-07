using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveInput : MonoBehaviour {

	[HideInInspector] public Vector3 outputVector;
	[HideInInspector] public bool keyboardOrController;
	[HideInInspector] public bool jump, jumpButtonDown;

	// Use this for initialization
	void Start () {
		outputVector = Vector3.zero;
	}
	
	// Update is called once per frame
	void FixedUpdate () {

		// test Jump
		if(Input.GetButton("Jump")) {

			if(!jump)
				jumpButtonDown = true;
			else
				jumpButtonDown = false;

			jump = true;
		}
		else jump = false;

		// test axis
		if(!keyboardOrController) outputVector = GetInputVectorFromArrowKeys();
		else outputVector = GetInputVectorFromPad();
		
	}

	public Vector3 GetInputVectorFromArrowKeys (){
		Vector3 currentVector = Vector3.zero;

		if(Input.GetKey("up"))
		{
			//if(Input.GetKeyDown("up")) keyJustPressed = true;
			currentVector += Vector3.forward;
		}
		if(Input.GetKey("down"))
		{
			//if(Input.GetKeyDown("down")) keyJustPressed = true;
			currentVector -= Vector3.forward;
		}
		if(Input.GetKey("right"))
		{
			//if(Input.GetKeyDown("right")) keyJustPressed = true;
			currentVector += Vector3.right;
		}
		if(Input.GetKey("left"))
		{
			//if(Input.GetKeyDown("left")) keyJustPressed = true;
			currentVector -= Vector3.right;
		}

		currentVector.Normalize();
		return currentVector;
	}

	public Vector3 GetInputVectorFromPad () {
		Vector3 currentVector = Vector3.zero;

		currentVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

		//Debug.Log(Input.GetAxis("Horizontal") + " ; " + Input.GetAxis("Vertical"));
		return currentVector;
	}
}
