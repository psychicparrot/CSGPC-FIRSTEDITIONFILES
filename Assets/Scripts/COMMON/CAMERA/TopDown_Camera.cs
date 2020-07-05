using UnityEngine;
using System.Collections;

public class TopDown_Camera : MonoBehaviour {

	public Transform followTarget;
	public Vector3 targetOffset;
	public float moveSpeed= 2f;
	
	public float maxHeight;
	public float minHeight;
	
	private Transform myTransform;
	
	void Start ()
	{
		myTransform= transform;	
	}
	
	public void SetTarget( Transform aTransform )
	{
		followTarget= aTransform;	
	}
	
	void LateUpdate ()
	{
		if(followTarget!=null)
			myTransform.position= Vector3.Lerp( myTransform.position, followTarget.position + targetOffset, moveSpeed * Time.deltaTime );
	}
}
