using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKbone : MonoBehaviour {

	public void EditValues (Transform tf, bool x, bool y, bool z, Vector2 vX, Vector2 vY, Vector2 vZ)
	{
		boneTransform = tf;
		xAxis = x;
		yAxis = y;
		zAxis = z;
		minMaxAngleX = vX;
		minMaxAngleY = vY;
		minMaxAngleZ = vZ;
	}

	public Transform boneTransform;
	public bool xAxis, yAxis, zAxis;
	public Vector2 minMaxAngleX, minMaxAngleY, minMaxAngleZ;
}
