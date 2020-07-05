using UnityEngine;
using System.Collections;
using AIStates;

public class SetAIChaseTargetBasedOnTag : MonoBehaviour
{
	public BaseAIController AIControlComponent;
	public string defaultTagFilter= "enemy";
	public bool checkForWalls;
	public LayerMask raycastWallLayerMask;
	public float chaseDistance;
	
	private GameObject myGO;
	private Transform myTransform;
	private GameObject tempGO;

	private RaycastHit hit;
	private Vector3 tempDirVec;
	private bool foundTarget;

	public float visionHeightOffset= 1f;

	void Start ()
	{
		// first, let's try to get the AI control script automatically
		AIControlComponent= GetComponent<BaseAIController>();
		
		myGO= gameObject;
		myTransform= transform;
		
		// quick null check, to warn us if something goes wrong
		if( AIControlComponent == null )
		{
			Debug.LogWarning("SetAIChaseTargetBasedOnTag cannot find BaseAIController");	
		}
		
		InvokeRepeating( "LookForChaseTarget", 1.5f, 1.5f );
	}
	
	void LookForChaseTarget ()
	{
		// null check
		if( AIControlComponent == null )
			return;
		
		GameObject[] gos = GameObject.FindGameObjectsWithTag( defaultTagFilter );
 
	    // Iterate through them
	    foreach ( GameObject go in gos )
	    {
			if( go!= myGO ) // make sure we're not comparing ourselves to ourselves
			{
				float aDist = Vector3.Distance( myGO.transform.position, go.transform.position );
				if(checkForWalls)
				{
					// wall check required
					if( CanSee( go.transform )==true )
					{
						AIControlComponent.SetChaseTarget( go.transform );
						foundTarget= true;
					}
				} else {
					// no wall check required! go ahead and find something to chase!
					if( aDist< chaseDistance )
					{
						// tell our AI controller to chase this target
						AIControlComponent.SetChaseTarget( go.transform );
						foundTarget= true;
					} 
				}
			}
		}

		if( foundTarget==false )
		{
			// clear target
			AIControlComponent.SetChaseTarget( null );

			// change AI state
			AIControlComponent.SetAIState( AIState.moving_looking_for_target );
		}
	}

	public bool CanSee( Transform aTarget )
	{
		// first, let's get a vector to use for raycasting by subtracting the target position from our AI position
		tempDirVec= Vector3.Normalize( aTarget.position - myTransform.position );
		
		// lets have a debug line to check the distance between the two manually, in case you run into trouble!
		Debug.DrawLine( myTransform.position, aTarget.position );
		
		// cast a ray from our AI, out toward the target passed in (use the tempDirVec magnitude as the distance to cast)
		if( Physics.Raycast( myTransform.position + ( visionHeightOffset * myTransform.up ), tempDirVec, out hit, chaseDistance ))
		{
			// check to see if we hit the target
			if( hit.transform.gameObject == aTarget.gameObject )
			{
				return true;
			}
		}
		
		// nothing found, so return false
		return false;
	}
}
