using UnityEngine;
using System.Collections;

public class Camera_Third_Person : BaseCameraController
{ 
	public Transform myTransform;
	public Transform target;
	public float distance = 20.0f;
	public float height = 5.0f;
	public float heightDamping = 2.0f;
 
	public float lookAtHeight = 0.0f;
 
	public float rotationSnapTime = 0.3F;
 
	public float distanceSnapTime;

	public Vector3 lookAtAdjustVector;
 
	private float usedDistance;
 
	float wantedRotationAngle;
	float wantedHeight;
 
	float currentRotationAngle;
	float currentHeight;
 
	Quaternion currentRotation;
	Vector3 wantedPosition;
 
	private float yVelocity = 0.0F;
	private float zVelocity = 0.0F;
 
	public override void SetTarget( Transform aTarget )
	{
		target= aTarget;
	}
	
	void LateUpdate () 
    {
        if ( target == null )
            return;

		if ( myTransform==null )
		{
 			myTransform= transform;
		}

		wantedHeight = target.position.y + height;
		currentHeight = myTransform.position.y;
 
		wantedRotationAngle = target.eulerAngles.y;
		currentRotationAngle = myTransform.eulerAngles.y;
 
		currentRotationAngle = Mathf.SmoothDampAngle(currentRotationAngle, wantedRotationAngle, ref yVelocity, rotationSnapTime);
 
		currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);
 
		wantedPosition = target.position;
		wantedPosition.y = currentHeight;
 
		usedDistance = Mathf.SmoothDampAngle(usedDistance, distance, ref zVelocity, distanceSnapTime); 
 
		wantedPosition += Quaternion.Euler(0, currentRotationAngle, 0) * new Vector3(0, 0, -usedDistance);
 
		myTransform.position = wantedPosition;
 
		myTransform.LookAt( target.position + lookAtAdjustVector );
 
	}
 
}
