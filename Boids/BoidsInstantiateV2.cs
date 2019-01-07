using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidsInstantiateV2 : MonoBehaviour {

	[SerializeField] private Transform TARGET; // Target of boids
	[SerializeField] private GameObject prefab; // boid prefab
	[SerializeField] private int boidsNumber = 50;
	[SerializeField] private float startVelocity = 0.02f;
	[SerializeField] private Vector3 minSpawnPos = new Vector3(-25.0f,-25.0f, -25.0f);
	[SerializeField] private Vector3 maxSpawnPos = new Vector3(25.0f,25.0f, 25.0f);
	

	[SerializeField] private float repulsionZone = 1.0f;
	[SerializeField] private float repulsionForce = 0.02f;

	[SerializeField] private float orientationZone = 3.0f;
	[SerializeField] private float orientationForce = 0.1f;

	[SerializeField] private float attractionZone = 50.0f;
	[SerializeField] private float attractionForce = 0.02f;

	[SerializeField] private float toTargetEffectDistance = 150;
	[SerializeField] private float toTarget = 0.1f;

	[SerializeField] private float speedLimit = 1.0f;
	[SerializeField] [Range(0f,1f)] private float depassementMaxReduction = 0.041f;

	private Boid[] boids;
	private Vector3 repulsionVelocity, orientationVelocity, attractionVelocity;
	private float previousDistToTarget;

	// Use this for initialization
	void Start () {

		// Setup and Instanciate Boids
		boids = new Boid[boidsNumber];
		for (int i = 0; i < boidsNumber; i++){
			Vector3 spawnPos = new Vector3 (
				Random.Range(minSpawnPos.x, maxSpawnPos.x),
				Random.Range(minSpawnPos.y, maxSpawnPos.y),
				Random.Range(minSpawnPos.z, maxSpawnPos.z));
			Boid newBoid = new Boid();
            GameObject newBoidGO = Instantiate(prefab, spawnPos, Quaternion.identity);
			if(newBoidGO.GetComponent<MeshRenderer>() != null) newBoidGO.GetComponent<MeshRenderer>().material.SetColor("_Color", Random.ColorHSV(0f,1f,0f,1f,0f,1f));
			newBoid.b_IDint = i;
			newBoid.b_body = newBoidGO.transform;
			newBoid.b_repulsionFactor = Random.value + 0.5f;
			newBoid.b_orientationFactor = Random.value + 0.5f;
			newBoid.b_attractionFactor = Random.value + 0.5f;
			newBoid.b_speedFactor = Random.value + 0.5f;
			newBoid.b_velocity = new Vector3(Random.value*startVelocity, Random.value*startVelocity, Random.value*startVelocity);
            boids[i] = newBoid;
		}
		
	}
	

	void Update () {

		foreach(Boid item in boids)
        {
			// Reset velocities
			repulsionVelocity = Vector3.zero;
			orientationVelocity = Vector3.zero;
			attractionVelocity = Vector3.zero;

			foreach(Boid r in boids){

				// Check distance from EACH other boid
				Vector3 toOther = r.b_body.position - item.b_body.position;
				Vector3 normToOther = Vector3.Normalize(toOther);
				float distOther = toOther.magnitude;

				// rule Repulsion
				if(distOther < repulsionZone){
					Vector3 addedRepVel = (-normToOther * repulsionForce) / boidsNumber;
					addedRepVel *= 1 - Mathf.Clamp(distOther / repulsionZone, 0, 1);
					repulsionVelocity += addedRepVel;
				}
				// rule Orientation
				else if(distOther < orientationZone){
					Vector3 addedOrientVel = (r.b_velocity * orientationForce) / boidsNumber;
					orientationVelocity += addedOrientVel;
				}
				// rule Attraction
				else if(distOther < attractionZone){
					Vector3 addedAttVel = (normToOther * attractionForce) / boidsNumber;
					addedAttVel *= 1 - Mathf.Clamp(distOther / attractionZone, 0, 1);
					attractionVelocity += addedAttVel;
				}
			}

			// Go to the Target
			Vector3 toTargetVec = TARGET.position - item.b_body.position;
			Vector3 toTargetForce = new Vector3();
			float distToTarget = toTargetVec.magnitude;
            	toTargetForce = toTargetVec;
				toTargetForce.Normalize();
				toTargetForce *= toTarget;
				toTargetForce *= distToTarget / toTargetEffectDistance;

			// Target depassement
			float factorDepassement = previousDistToTarget / distToTarget;
			if(factorDepassement <= 0.8f) {
				factorDepassement *= depassementMaxReduction;
			}else factorDepassement = 0.0f;

			previousDistToTarget = distToTarget;

			// process new Velocity
			item.b_velocity += repulsionVelocity * item.b_repulsionFactor
							+ orientationVelocity * item.b_orientationFactor
							+ attractionVelocity * item.b_attractionFactor
							+ toTargetForce
							- item.b_velocity*factorDepassement;

			// limit speed
			float thisSpeedLimit = speedLimit * item.b_speedFactor;
			float speedClamp = Mathf.Clamp((item.b_velocity.magnitude - thisSpeedLimit) / 2f, 0f, Mathf.Infinity);
			Vector3 normVel = Vector3.Normalize(item.b_velocity);
			normVel *= item.b_velocity.magnitude - speedClamp;
			item.b_velocity = normVel;

			// Apply
			item.b_body.position += item.b_velocity;
			item.b_body.rotation = Quaternion.LookRotation(item.b_velocity);

        }



    }
}
