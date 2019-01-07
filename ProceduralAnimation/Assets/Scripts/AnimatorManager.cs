using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorManager : MonoBehaviour {

	[HideInInspector] public Animator anim;
	private CharacterManager characterManager;
	[SerializeField] private GameObject sourcePlayer;
	[Range(0f,1f)] public float lerpSpeedParam;
	public float blendSpeedParam;
	private Vector2 cm_velocityLimit;
	private float cm_velocitySpeed;
	private float yVelParam;
	private bool keepLastNotGroundedVel;
	public Vector2 YVelocityLimit;
	[HideInInspector] public float LF_grounded, LH_grounded, RF_grounded, RH_grounded;


	// Use this for initialization
	void Start () {
		if(sourcePlayer) {
			anim = sourcePlayer.GetComponent<Animator>();
			characterManager = GetComponent<CharacterManager>();
		}

		if(!characterManager)
			Debug.LogError("Information Script Missing");

		if(YVelocityLimit.y - YVelocityLimit.x == 0f)
			Debug.LogError("Set YVelocityLimit x=min  y=max");
	}
	
	// Update is called once per frame
	void Update () {

		if(!characterManager.isGrounded)
		{
			anim.SetBool("grounded", false);
			yVelParam = (characterManager.yVelocity - YVelocityLimit.x) / (YVelocityLimit.y - YVelocityLimit.x);
			yVelParam = Mathf.Clamp(yVelParam, 0.001f, 0.999f);
		} else anim.SetBool("grounded", true);

		SetFloatParam("verticalVelocity", yVelParam);



		cm_velocityLimit = characterManager.velocityLimit;
		cm_velocitySpeed = characterManager.publicVelocity.magnitude;
		float oldSpeedParam = blendSpeedParam;

		blendSpeedParam = Mathf.InverseLerp(0, cm_velocityLimit.y, cm_velocitySpeed);
		blendSpeedParam = Mathf.Lerp(oldSpeedParam, blendSpeedParam, lerpSpeedParam);

		SetFloatParam("blendSpeed", blendSpeedParam);
		//Debug.Log("Animator Updated !");
		
	}

	public void SetTrigger(string triggerName, bool setTrueResetFlase) {
		if(setTrueResetFlase)
			anim.SetTrigger(triggerName);
		else
			anim.ResetTrigger(triggerName);
	}
	public void SetFloatParam(string floatName, float value) {
		anim.SetFloat(floatName, value);
	}
	public float GetFloatParam(string ftName) {
		if(HasParameter(ftName, anim)) return anim.GetFloat(ftName);
		else return 0f;
	}
	public void SetBoolParam (string boolName, bool b) {
		anim.SetBool(boolName, b);
	}

	public bool HasParameter(string paramName, Animator animator)
	{
	/*	foreach (AnimatorControllerParameter param in animator.parameters)
		{
		if (param.name == paramName)
			return true;
		}/* */
		return false;
	}
}
