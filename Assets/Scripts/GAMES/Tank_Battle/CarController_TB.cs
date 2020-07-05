using UnityEngine;
using System.Collections;

using AIStates;

[AddComponentMenu("Sample Game Glue Code/Tank Battle/Car Controller")]

public class CarController_TB : BaseVehicle
{
	public BaseWeaponController weaponControl; // note that we don't use the standard slot system here!
	
	public bool canFire;
	public bool isRespawning;
	public bool isAIControlled;
	public bool isInvulnerable;
	
	public BaseAIController AIController;
	
	public int startHealthAmount= 50;
	
	public BasePlayerManager myPlayerManager;
	public BaseUserManager myDataManager;
	
	public float turnTorqueHelper= 90;
	public float TurnTorqueHelperMaxSpeed= 30;
	
	public float catchUpAccelMax= 8000;
	public float originalAccelMax= 5000;
	public float resetTime= 4;
	
	public LayerMask respawnLayerMask;
	
	private BaseArmedEnemy gunControl;
	private BattleController battleControl;
	
	private int racerID;
	private int myRacePosition;
	private bool isGameRunning;
	private float resetTimer;
	private bool canRespawn;
	private bool canPlayFireSound;
	public float timeBetweenFireSounds= 0.25f;
	private bool isFinished;
	
	private Vector3 respawnPoint;
	private Vector3 respawnRotation;
	
	public Material treadMaterial;
	public GameObject shieldMesh;
	public float respawnInvunerabilityTime= 5;
	
	public override void Start ()
	{
		// we are overriding the Start function of BaseVehicle because we do not want to
		// initialize from here! Game controller will call Init when it is ready
		
		myBody= GetComponent<Rigidbody>();
		myGO= gameObject;
		myTransform= transform;
	}
	public override void Init ()
	{
		Debug.Log ("CarController_TB Init called.");
		
		// cache the usual suspects
		myBody= GetComponent<Rigidbody>();
		myGO= gameObject;
		myTransform= transform;
		
		// allow respawning from the start
		canRespawn=true;
		
		// save our accelMax value for later, incase we need to change it to
		// do AI catch up
		originalAccelMax= accelMax;
		
		// add default keyboard input if we don't already have one
		default_input= myGO.GetComponent<Keyboard_Input>();
		
		if( default_input==null )
			default_input= myGO.AddComponent<Keyboard_Input>();
		
		// cache a reference to the player controller
		myPlayerController= myGO.GetComponent<BasePlayerManager>();
		
		// call base class init
		myPlayerController.Init();
		
		// with this simple vehicle code, we set the center of mass low to try to keep the car from toppling over
		myBody.centerOfMass= new Vector3(0,-6.5f,0);
		
		// see if we can find an engine sound source, if we need to
		if( engineSoundSource==null )
		{
			engineSoundSource= myGO.GetComponent<AudioSource>();
		}
		
		AddBattleController();
		
		// get a ref to the weapon controller
		weaponControl= myGO.GetComponent<BaseWeaponController>();
		
		// if a player manager is not set in the editor, let's try to find one
		if(myPlayerManager==null)
			myPlayerManager= myGO.GetComponent<BasePlayerManager>();
		
		// cache ref to data manager
		myDataManager= myPlayerManager.DataManager;
		
		// set default data
		myDataManager.SetName("Player");
		myDataManager.SetHealth(startHealthAmount);
				
		if(isAIControlled)
		{
			// set our name to an AI player
			myDataManager.SetName("AIPlayer");
			
			// set up AI
			InitAI();
		}
		
		isGameRunning= true;
		
		// store respawn point
		respawnPoint= myTransform.position;
		respawnRotation= myTransform.eulerAngles;
		
		MakeVulnerable();
		
		// grab volume from sound controller for our engine sound
		GetComponent<AudioSource>().volume= BaseSoundController.Instance.volume;
	}	
	
	void InitAI()
	{
		// cache a reference to the AI controller
		AIController= myGO.GetComponent<BaseAIController>();
		
		// check to see if we found an ai controller component, if not we add one here
		if(AIController==null)
			AIController= myGO.AddComponent<BaseAIController>();
		
		// initalize the AI controller
		AIController.Init();
		
		// tell the AI controller to go into waypoint steering mode
		AIController.SetAIState( AIStates.AIState.steer_to_waypoint );
		
		// disable our default input method
		default_input.enabled=false;
		
		// add an AI weapon controller
		gunControl= myGO.GetComponent<BaseArmedEnemy>();
		
		// if we don't already have a gun controller, let's add one to stop things breaking but
		// warn about it so that it may be fixed
		if( gunControl==null )
		{
			gunControl= myGO.AddComponent<BaseArmedEnemy>();
			Debug.LogWarning("WARNING! Trying to initialize car without a BaseArmedEnemy component attached. Player cannot fire!");
		}
		
		// tell gun controller to do 'look and destroy'
		gunControl.currentState= AIAttackStates.AIAttackState.look_and_destroy;
	}
		
	void AddBattleController()
	{			
		if(myGO==null)
			myGO=gameObject;
		
		// add a race controller script to our object
		battleControl= myGO.AddComponent<BattleController>();
		
		// grab an ID from the race control script, so we don't have to look it up whenever
		// we communicate with the global battle controller
		racerID= battleControl.GetID();
		
		// we are going to use the same id for this player and its weapons
		id=racerID;
		
		// tell weapon controller this id so we can add it to projectiles. This way, when a projectile hits something
		// we can look up its id and trace it back to this player
		weaponControl.SetOwner(id);
		
		// set up a repeating invoke to update our position, rather than calling it
		// every single tick or update
		InvokeRepeating("UpdateBattlePosition",0, 0.5f);
	}
	
	public void UpdateBattlePosition()
	{
		// grab our race position from the global race manager
		myRacePosition= GlobalBattleManager.Instance.GetPosition( racerID );
	}
	
	public override void UpdatePhysics ()
	{
		if( canControl )
    		base.UpdatePhysics();
		
		if( isFinished )
			myBody.velocity *= 0.99f;
			
		// if we are moving slow, apply some extra force to turn the car quickly
		// so we can do donuts (!) - note that since there is no 'ground detect' it will apply it in the air, too (bad!)
		if( mySpeed < TurnTorqueHelperMaxSpeed )
		{
			myBody.AddTorque( new Vector3( 0, steer * myBody.mass * turnTorqueHelper * motor, 0 ) );
		}
	}
	
	private float timedif;
	
	public override void LateUpdate()
	{		
		// get the state of the race from the race controller
		isGameRunning= battleControl.battleRunning;
		
		if( isGameRunning )
		{
			// check to see if we've crashed and are upside down/wrong(!)
			Check_If_Car_Is_Flipped();
			
			// make sure that gunControl has been told it can fire
			if( isAIControlled )
				gunControl.canControl=true;
			
			// we check for input in LateUpdate because Unity recommends this
			if( isAIControlled )
			{
				GetAIInput();
			} else {
				GetInput();
			}
		} else {
			if( isAIControlled ) 
			{
				// since the race is not running, we'll tell the AI gunControl not to fire yet
				gunControl.canControl=false;
			}
		}
				
		// see if our car is supposed to be held in-place
		CheckLock();
				
		// update the audio
		UpdateEngineAudio();
		
		// finally, update the tread scrolling texture
		if(treadMaterial!=null)
			treadMaterial.SetTextureOffset ( "_MainTex", new Vector2(0, treadMaterial.mainTextureOffset.y + (mySpeed * -0.005f) ) );
	}
	
	public override void GetInput()
	{
		// calculate steering amount
		steer= Mathf.Clamp( default_input.GetHorizontal() , -1, 1 );
				
		// how much accelerator?
        motor= Mathf.Clamp( default_input.GetVertical() , 0, 1 );
		
		// how much brake?
		brake= -1 * Mathf.Clamp( default_input.GetVertical() , -1, 0 );
		
		if( default_input.GetRespawn() && !isRespawning && canRespawn)
		{
			isRespawning=true;
			Respawn();
			canRespawn=false;
			Invoke ("resetRespawn",2);
		}
		
		// fire if we need to
		if( default_input.GetFire() && canFire )
		{
			// tell weapon controller to deal with firing
			weaponControl.Fire();
			
			if(canPlayFireSound)
			{
				canPlayFireSound=false;
				Invoke ("ResetFireSoundDelay", timeBetweenFireSounds);
			}
		}
	}
	
	void ResetFireSoundDelay()
	{
		canPlayFireSound=true;
	}
	
	void resetRespawn()
	{
		canRespawn=true;	
	}
	
	public void GetAIInput ()
	{
		// calculate steering amount
		steer= Mathf.Clamp( AIController.GetHorizontal(), -1, 1 );
		
		// how much accelerator?
        motor= Mathf.Clamp( AIController.GetVertical() , -1, 1 );

		// THIS IS IMPORTANT! As we are a wheeled vehicle, the engine needs to drive to turn when stopped
		if( AIController.currentAIState == AIState.stopped_turning_left || AIController.currentAIState == AIState.stopped_turning_right )
		{
			// back up
			motor= 0.4f;
		}
	}

    public void SetAIInput (bool aiFlag)
    {
        isAIControlled = aiFlag;
    }
	
	private ProjectileController aProj;
	private GameObject tempGO;
	
	void OnCollisionEnter(Collision collider)
	{
		// when something collides with our ship, we check its layer to see if it is a projectile
		// (Note: remember when you add projectiles to set the layers correctly!)
		// by default, we're using layer 17 for projectiles fired by enemies and layer 9 for projectiles fired
		// by the main player but all we need to know here is that it *is* a projectile of any type
		
		if(!isRespawning && !isInvulnerable) // make sure no respawning or invulnerability is happening
		{
			// temp ref to this collider's gameobject so that we don't need to keep looking it up
			tempGO= collider.gameObject;
			
			// do a quick layer check to make sure that these are in fact projectiles
			if(tempGO.layer==17 || tempGO.layer==9)
			{
				// grab a ref to the projectile's controller
				aProj= tempGO.GetComponent<ProjectileController>();
			
				// quick check to make sure that this projectile was not launched by this player
				if( aProj.ownerType_id != id )
				{
					// tell the hit function about this collision, passing in the gameobject so that we can
					// get to its projectile controller script and find out more about where it came from
					Hit();
					
					// tell our battle controller that we got hit
					battleControl.Fragged();
					
					// tell the global battle controller who fragged us
					GlobalBattleManager.Instance.RegisterFrag( aProj.ownerType_id );
				}
			}
		}
	}
	
	public void OnTriggerEnter( Collider other )
	{
	
		// check to see if the trigger uses any of the layers where we want to automatically respawn the player on impact
		int objLayerMask = (1 << other.gameObject.layer);

		if ((respawnLayerMask.value & objLayerMask) > 0) 
		{
			Respawn();
		}
	}
	
	void Hit()
	{	
		// reduce lives by one
		myDataManager.ReduceHealth(1);
		
		if(myDataManager.GetHealth()<1) // <- destroyed
		{
			isRespawning=true;
			
			// blow up! (apply a force to affect everything in the vicinity and let's get the model spinning too, with angular velocity)
			myBody.AddExplosionForce(myBody.mass * 2000f, myBody.position, 100);
			myBody.angularVelocity=new Vector3( Random.Range (-100,100), Random.Range (-100,100), Random.Range (-100,100) );
			
			// tell game controller to do a nice big explosion
			GameController_TB.Instance.PlayerBigHit( myTransform );
			
			// respawn 
			Invoke("Respawn",4f);
			
			// reset health to full
			myDataManager.SetHealth(startHealthAmount);

		} else {
			
			// tell game controller to do small scale hit if we still have some health left
			GameController_TB.Instance.PlayerHit( myTransform );
		}
	}
		
	void Respawn()
	{
		// reset the 'we are respawning' variable
		isRespawning= false;
		
		// reset our velocities so that we don't reposition a spinning vehicle
		myBody.velocity=Vector3.zero;
		myBody.angularVelocity=Vector3.zero;
		
		// get the waypoint to respawn at from the race controller
		tempVEC= respawnPoint;
		
		// cast a ray down from the waypoint to try to find the ground
		RaycastHit hit;
		if(Physics.Raycast(tempVEC + (Vector3.up * 300), -Vector3.up, out hit)){
			tempVEC.y=hit.point.y+15;
		}
		
		// reposition the player at tempVEC (the waypoint position with a corrected y value via raycast)
		// and also we set the player rotation to the waypoint's rotation so that we are facing in the right
		// direction after respawning
		myTransform.eulerAngles= respawnRotation;
		myTransform.position= tempVEC;
		
		// we need to be invulnerable for a little while
		MakeInvulnerable();
		
		Invoke ("MakeVulnerable", respawnInvunerabilityTime);
		
		// revert to the first weapon
		weaponControl.SetWeaponSlot(0);
		
		// show the current weapon (since it was hidden when the ship explosion was shown)
		//weaponControl.EnableCurrentWeapon();
	}
	
	void MakeInvulnerable()
	{
		isInvulnerable=true;
		shieldMesh.SetActive(true);
	}
	
	void MakeVulnerable()
	{
		isInvulnerable=false;
		shieldMesh.SetActive(false);
	}
	
	public void PlayerFinishedBattle()
	{
		Debug.Log ("PlayerFinished() called!");
		
		// disable this vehicle
		isAIControlled= false;
		//canControl= false;
		canFire= false;
		// if we have an AI controller, let's take away its control now that the battle is over
		if(AIController!=null)
			AIController.canControl= false;
		motor= 0;
		steer= 0;
		isFinished=true;
	}
	
	void Check_If_Car_Is_Flipped()
	{
		if((myTransform.localEulerAngles.z > 80 && myTransform.localEulerAngles.z < 280) || (myTransform.localEulerAngles.x > 80 && myTransform.localEulerAngles.x < 280)){
			resetTimer += Time.deltaTime;
		} else {
			resetTimer = 0;
		}
		
		if(resetTimer > resetTime)
			Respawn();
	}
}
