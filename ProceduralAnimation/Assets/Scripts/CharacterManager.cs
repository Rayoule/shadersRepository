using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour {

	public bool localOrientation;
	[SerializeField] MoveInput inputScript;
	[SerializeField] AnimatorManager animScript;
	[SerializeField] float forwardTiltFactor = 1f;
	[SerializeField] [Range(0.0f, 1.0f)] float ForwardTiltLerpFactor = 0.05f;
	[SerializeField] float sideTiltFactor = 1f;
	[Range(0, 0.02f)] public float drag;
	public Vector2 velocityLimit;
	private float minimumVelocity = 0.00001f;
	public float turnFactor;
	public float maxRotY;
	public Vector3 inputAxis;

	private Vector3 velocity;
	[HideInInspector] public Vector3 publicVelocity = Vector3.zero;
	[HideInInspector] public float yPublicVelocity = 0f;
	private float prevAngleY;
	public float accelerationFactor;
	[SerializeField] [Range(0.0f, 1.0f)] float accelerationLerpFactor = 0.1f;
	private Vector3 currentPos;
	private Vector3 currentAddedVelocity;
	private Vector3 currentSpeed;
	private Vector3 precedentSpeed;
	[HideInInspector] public Vector3 acceleration, m_previousAccel;
	private Quaternion previousXZOrient;
	//private Animator anim;


	public float jumpVelocityFactor;
	public float jumpImpulseForce;
	public float jumpFlyForce;
	public float gravityForce;
	public float minLandingImpact;
	public float minFallingSpeed;
	public float preJumpDuration;
	 public bool isGrounded, isLanding, preJumpTimer, isPreJumping, isFalling, isJumping;
	[HideInInspector] public float yVelocity;
	private IEnumerator preJumpCoroutine;


	void Start () {
		isPreJumping = false;
		preJumpTimer = false;
		isGrounded = false;
		isLanding = false;
		velocity = Vector3.zero;
		yVelocity = 0f;
		currentPos = transform.position;
		previousXZOrient = transform.localRotation;
		currentAddedVelocity = Vector3.zero;

		animScript = GetComponent<AnimatorManager>();
	}

	
	void FixedUpdate () {

		currentPos = transform.position;
		precedentSpeed = currentSpeed;
		m_previousAccel = acceleration;

		// Add inputs Axis on localOrientation or world
		if(localOrientation) {
			inputAxis = Quaternion.AngleAxis(transform.localRotation.eulerAngles.y, Vector3.up) * inputScript.outputVector;
		} else {
			inputAxis = inputScript.outputVector;
		}


		Vector3 inputVector = new Vector3(inputAxis.x * Time.deltaTime,
				0f,
				inputAxis.z  * Time.deltaTime);

		if(isGrounded)
		{

			float velSqrMagnitude = velocity.sqrMagnitude;

			if (velSqrMagnitude < velocityLimit.x*velocityLimit.x)
			{
				if (inputVector.sqrMagnitude != 0)
				{
					currentAddedVelocity = inputVector;
					velocity += currentAddedVelocity;

				}else if (velSqrMagnitude <= minimumVelocity)
				{
					velocity = Vector3.zero;
				} else {
					velocity -= Vector3.Normalize(velocity) * drag;
				}
			} else if (velSqrMagnitude >= velocityLimit.y*velocityLimit.y && inputVector != Vector3.zero)
			{
				Vector3 currentVel = velocity;
				currentAddedVelocity = inputVector;
				velocity += currentAddedVelocity;
				velocity.Normalize();
				velocity *= velocityLimit.y;
				currentAddedVelocity = velocity - currentVel;
			} else {
				currentAddedVelocity = inputVector;
				velocity += currentAddedVelocity;

				velocity -= Vector3.Normalize(velocity) * drag;
			}

		}

		// Check if the character make sudden return
		/*if(HardTurn())
		{
			float Ypos = transform.position.y;
			Ypos -= 0.1f;
			transform.position = new Vector3(transform.position.x, Ypos, transform.position.z);
		}*/


		// jump calculation
		yVelocity = VerticalVelocityProcess(yVelocity);


		// Update character
		Vector3 newPos = transform.position;
		newPos.x += velocity.x;
		newPos.z += velocity.z;
		newPos.y += yVelocity;
		transform.position = newPos;

		publicVelocity = velocity;
		yPublicVelocity = yVelocity;


		// calculate Speed and Acceleration
		currentSpeed = transform.position - currentPos;
		acceleration = currentSpeed - precedentSpeed;
		acceleration = Vector3.Lerp(m_previousAccel, acceleration, accelerationLerpFactor);

		Debug.DrawRay(transform.position + new Vector3(0,2,0), acceleration * 1000f, Color.red);
		Debug.DrawRay(transform.position + new Vector3(0,2,0), velocity*10f, Color.green);



		// Update rotation
		YOrientationProcess(velocity);
		XZOrientationProcess(acceleration * velocity.magnitude * 10f); // tilt with acceleration
		//XZOrientationProcess(velocity*10f); // tilt with velocity

	}

	void OnCollisionExit() {
		isGrounded = false;
		isLanding = false;
	}

	void OnCollisionEnter(Collision collision) {
		if(!isGrounded) {
			isLanding = true;
			//Debug.Log("LANDING !");
		}
		isGrounded = true;
		isJumping = false;
		isFalling = false;
	}
	void OnCollisionStay() {
		isGrounded = true;
		isJumping = false;
		isFalling = false;
	}

	private float VerticalVelocityProcess (float yVel) {

		float newVelY = yVel;

		// is just landing ?
		if(isLanding)
		{
			if(newVelY < minFallingSpeed) { // Ne joue l'anime Landing que si la chute était importante
				animScript.SetTrigger("landingTrigger", true);
			}
			newVelY = 0f;
			//Debug.Log("LANDED");
			isLanding = false;
			return newVelY;
		}

		// jump effect
		if (inputScript.jump)
		{
			if(isGrounded)
			{
				if(inputScript.jumpButtonDown && !isPreJumping) {
					// Launch timer for preJump
					preJumpCoroutine = WaitAndJump(preJumpDuration);
					StartCoroutine(preJumpCoroutine);
					animScript.SetTrigger("preJumpTrigger", true);
					
					isPreJumping = true;
				}
			} else if(newVelY > 0f){
				newVelY += jumpFlyForce; // JUMP!
				isJumping = true;
			}
		}

		// start the jump after the coroutine ends
		if (preJumpTimer)
		{
			newVelY = Jump(newVelY);
			animScript.SetTrigger("jumpTrigger", true);
			preJumpTimer = false;
		}



		if(!isGrounded)
		{
			if(!isJumping && newVelY < minFallingSpeed) {	 // is Falling ?
				//Debug.Log("Falling !!!");
				isFalling = true;

				if(!animScript.anim.GetCurrentAnimatorStateInfo(0).IsName("JumpBlendTree"))
					animScript.SetTrigger("fallingTrigger", true);
			}

			newVelY += gravityForce;  // gravity
		}

		// reset preJumpTimer
		preJumpTimer = false;


		return newVelY;
	}


	public IEnumerator WaitAndJump(float duration)
    {
        // suspend execution for [duration] seconds
        yield return new WaitForSeconds(duration);
		isPreJumping = false;
        preJumpTimer = true;
		//Debug.Log("preJump Coroutine Ended");
    }


	public float Jump(float velY) {
		velY += jumpImpulseForce;
		velocity = velocity * jumpVelocityFactor;
		isGrounded = false;
		return velY;
	}



	public void YOrientationProcess (Vector3 vel) {
		if(vel.sqrMagnitude < velocityLimit.x*velocityLimit.x) return;
		
		Quaternion currentRot = transform.localRotation;
		Vector3 targetPoint = currentPos + vel;
		Vector3 toLookVector = targetPoint - currentPos;
		Quaternion lookRot = Quaternion.identity;
		if(toLookVector != Vector3.zero) {
			lookRot = Quaternion.LookRotation(toLookVector, Vector3.up);
			Quaternion lerpRot = Quaternion.Slerp(currentRot, lookRot, Mathf.Clamp01(turnFactor * vel.magnitude));

			float angle = Quaternion.Angle(currentRot, lookRot);
			if(Mathf.Abs(angle) >= maxRotY)
			{
				//animScript.SetHardTurnAnim();
				animScript.SetTrigger("hardTurnTrigger", true);
			}
			
			transform.localRotation = lerpRot;
		}
	}

	public void XZOrientationProcess (Vector3 acceleration) {

		Vector3 eulerLocalRot = this.transform.localRotation.eulerAngles;

		eulerLocalRot.x = Vector3.Dot(acceleration, transform.forward * forwardTiltFactor);

		eulerLocalRot.z = Vector3.Dot(acceleration, -transform.right * sideTiltFactor);

		transform.localRotation = Quaternion.Lerp(previousXZOrient, Quaternion.Euler(eulerLocalRot), ForwardTiltLerpFactor);

		// restore la rotation Z qui doit rester inchangée ici
		Vector3 restoredYrotation = transform.localRotation.eulerAngles;
		restoredYrotation.y = eulerLocalRot.y;
		transform.localRotation = Quaternion.Euler(restoredYrotation);
		previousXZOrient = transform.localRotation;
	}
}
