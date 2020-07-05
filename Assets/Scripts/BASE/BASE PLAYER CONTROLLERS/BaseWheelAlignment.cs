// Converted from UnityScript to C# at http://www.M2H.nl/files/js_to_c.php - by Mike Hergaarden

// this script is based on car control code from this forum post:
// http://forum.unity3d.com/threads/50643-How-to-make-a-physically-real-stable-car-with-WheelColliders
// by Edy.

using UnityEngine;
using System.Collections;

public class BaseWheelAlignment : MonoBehaviour 
{

	// Define the variables used in the script, the Corresponding collider is the wheel collider at the position of
	// the visible wheel, the slip prefab is the prefab instantiated when the wheels slide, the rotation value is the
	// value used to rotate the wheel around it's axel.
	public WheelCollider correspondingCollider;
	public GameObject slipPrefab;
	public float slipAmountForTireSmoke= 50f;
	
	private float RotationValue = 0.0f;
	private Transform myTransform;
	private Quaternion zeroRotation;
	private Transform colliderTransform;
	private float suspensionDistance;
	
	void Start ()
	{
		// cache some commonly used things..
		myTransform= transform;
		zeroRotation= Quaternion.identity;
		colliderTransform= correspondingCollider.transform;
	}
	
	void Update ()
	{
		// define a hit point for the raycast collision
		RaycastHit hit;
		// Find the collider's center point, you need to do this because the center of the collider might not actually be
		// the real position if the transform's off.
		Vector3 ColliderCenterPoint= colliderTransform.TransformPoint( correspondingCollider.center );
		
		// now cast a ray out from the wheel collider's center the distance of the suspension, if it hit something, then use the "hit"
		// variable's data to find where the wheel hit, if it didn't, then se tthe wheel to be fully extended along the suspension.
		if ( Physics.Raycast( ColliderCenterPoint, -colliderTransform.up, out hit, correspondingCollider.suspensionDistance + correspondingCollider.radius ) ) {
			myTransform.position= hit.point + (colliderTransform.up * correspondingCollider.radius);
		} else {
			myTransform.position= ColliderCenterPoint - ( colliderTransform.up * correspondingCollider.suspensionDistance );
		}
		
		// now set the wheel rotation to the rotation of the collider combined with a new rotation value. This new value
		// is the rotation around the axle, and the rotation from steering input.
		myTransform.rotation= colliderTransform.rotation * Quaternion.Euler( RotationValue, correspondingCollider.steerAngle, 0 );
		
		// increase the rotation value by the rotation speed (in degrees per second)
		RotationValue+= correspondingCollider.rpm * ( 360 / 60 ) * Time.deltaTime;
		
		// define a wheelhit object, this stores all of the data from the wheel collider and will allow us to determine
		// the slip of the tire.
		WheelHit correspondingGroundHit= new WheelHit();
		correspondingCollider.GetGroundHit( out correspondingGroundHit );
		
		// if the slip of the tire is greater than 2.0f, and the slip prefab exists, create an instance of it on the ground at
		// a zero rotation.
		if ( Mathf.Abs( correspondingGroundHit.sidewaysSlip ) > slipAmountForTireSmoke ) {
			if ( slipPrefab ) {
				SpawnController.Instance.Spawn( slipPrefab, correspondingGroundHit.point, zeroRotation );
			}
		}
		
	}
}