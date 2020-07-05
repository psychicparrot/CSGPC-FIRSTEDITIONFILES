using UnityEngine;
using System.Collections;

[AddComponentMenu("Sample Game Glue Code/Metal Vehicle Destruction/Car Controller")]

public class CarController_MVD : BaseVehicle
{
	public Standard_SlotWeaponController weaponControl;
	
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
	private RaceController raceControl;
	private int racerID;
	private int myRacePosition;
	private bool isRaceRunning;
	private float resetTimer;
	private bool canRespawn;
	private bool canPlayFireSound;
	public float timeBetweenFireSounds= 0.25f;
	
	public override void Start ()
	{
		// we are overriding the Start function of BaseVehicle because we do not want to
		// initialize from here! Game controller will call Init when it is ready
	}
	public override void Init ()
	{
		Debug.Log ("CarController_MVD Init called.");
		
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
		if( default_input==null )
			default_input= myGO.AddComponent<Keyboard_Input>();
		
		// cache a reference to the player controller
		myPlayerController= myGO.GetComponent<BasePlayerManager>();
		
		// call base class init
		myPlayerController.Init();
		
		// with this simple vehicle code, we set the center of mass low to try to keep the car from toppling over
		myBody.centerOfMass= new Vector3(0,-3.5f,0);
		
		// see if we can find an engine sound source, if we need to
		if( engineSoundSource==null )
		{
			engineSoundSource= myGO.GetComponent<AudioSource>();
		}
		
		AddRaceController();
		
        // reset our lap counter
        raceControl.ResetLapCounter();

		// get a ref to the weapon controller
		weaponControl= myGO.GetComponent<Standard_SlotWeaponController>();
		
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
		
		//GameController_MVD.Instance.UpdateWrongWay(false);
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
	
	public void AddRaceController()
	{
		if(myGO==null)
			myGO=gameObject;
		
		// add a race controller script to our object
		raceControl= myGO.AddComponent<RaceController>();
		
		// grab our racer ID from the race control script, so we don't have to look it up whenever
		// we communicate with the global race controller
		racerID=raceControl.GetID();
		
		// set up a repeating invoke to update our race position, rather than calling it
		// every single tick or update
		InvokeRepeating("UpdateRacePosition",0, 0.5f);
	}
	
	public void UpdateRacePosition()
	{
		// grab our race position from the global race manager
		myRacePosition=GlobalRaceManager.Instance.GetPosition( racerID );
	}
	
	public override void UpdatePhysics ()
	{
		if(canControl)
    		base.UpdatePhysics();
		
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
		isRaceRunning= raceControl.raceRunning;
		
		if(isRaceRunning)
		{
			// check to see if we've crashed and are upside down/wrong(!)
			Check_If_Car_Is_Flipped();
			
			// make sure that gunControl has been told it can fire
			if(isAIControlled)
				gunControl.canControl=true;
			
			// we check for input in LateUpdate because Unity recommends this
			if(isAIControlled)
			{
				GetAIInput();
			} else {
				GetInput();
			}
		} else {
			if(isAIControlled)
			{
				// since the race is not running, we'll tell the AI gunControl not to fire yet
				gunControl.canControl=false;
			}
		}
		
		// tell the race controller to update
		raceControl.UpdateWaypoints();
		
		// see if our car is supposed to be held in-place
		CheckLock();
		
		// tell race control to see if we're going the wrong way
		raceControl.CheckWrongWay();
			
		// check to see if we're going the wrong way and act on it
		if(raceControl.goingWrongWay)
		{
			if (!isAIControlled)
				GameController_MVD.Instance.UpdateWrongWay(true);
			
			// if going the wrong way, compare time since wrong way started to see if we need to respawn
			if(raceControl.timeWrongWayStarted!=-1)
				timedif= Time.time - raceControl.timeWrongWayStarted;
			
			if(timedif>resetTime)
			{
				// it's been x resetTime seconds in the wrong way, let's respawn this thing!
				Respawn();
			}	
		} else if (!isAIControlled){
			GameController_MVD.Instance.UpdateWrongWay(false);
		}
		
		accelMax= originalAccelMax;
		
		// do catch up if this car is falling behind in the race
		if(isRaceRunning && isAIControlled)
		{
			if(myRacePosition>3)
			{
				// speed up
				accelMax= catchUpAccelMax;
			} else {
				accelMax= originalAccelMax;
			}
			
			// first place, let's slow you down!
			if(myRacePosition<2)
			{
				accelMax= originalAccelMax*0.25f;
				//motor=-1;
			}
		}
		
		// update the audio
		UpdateEngineAudio();
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
		
		// first, we make sure that a weapon controller exists (otherwise no need to fire!)
		if( weaponControl != null )
		{
			
			// fire if we need to
			if( default_input.GetFire() && canFire )
			{
				// tell weapon controller to deal with firing
				weaponControl.Fire();
				
				if(canPlayFireSound)
				{
					// tell our sound controller to play a pew sound
					BaseSoundController.Instance.PlaySoundByIndex(0, myTransform.position);
					
					canPlayFireSound=false;
					Invoke ("ResetFireSoundDelay", timeBetweenFireSounds);
				}
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
        motor= Mathf.Clamp( AIController.GetVertical() , 0, 1 );
		
		// how much brake?
		//brake= -1 * Mathf.Clamp( AIController._input.GetVertical(), -1, 0 );
	}

    public void FinishedRace ()
    {
        // stop allowing control for the vehicle
        canControl = false;
        brake = 1;
        motor = 0;

        // set a flag so it's easy to tell when we are done
        raceControl.RaceFinished();
    }

    public void SetAIInput (bool aiFlag)
    {
        isAIControlled = aiFlag;
    }

    public bool IsFinished ()
    {
        return raceControl.GetIsFinished();
    }

    public int GetCurrentLap ()
    {
        return raceControl.GetCurrentLap();
    }

    public int GetCurrentWaypointNum ()
    {
        return raceControl.GetCurrentWaypointNum();
    }

    public float GetCurrentWaypointDist ()
    {
        return raceControl.GetCurrentWaypointDist();
    }

    public bool IsLapDone ()
    {
        return raceControl.GetIsLapDone();
    }
	
	public void SetWayController( Waypoints_Controller aControl )
	{
		raceControl.SetWayController(aControl);	
	}
	
	void OnCollisionEnter(Collision collider)
	{
		// MAKE SURE that weapons don't have colliders
		// if you are using primitives, only use a single collider on the same gameobject which has this script on
		
		// when something collides with our ship, we check its layer to see if it is on 11 which is our projectiles
		// (Note: remember when you add projectiles to set the layer correctly!)
		if(isAIControlled)
		{
			if(collider.gameObject.layer==9 && !isRespawning && !isInvulnerable)
			{
				Hit();
			}
		} else {
			if(collider.gameObject.layer==17 && !isRespawning && !isInvulnerable)
			{
				Hit();
			}	
		}
		
	}
	
	public void OnTriggerEnter( Collider other )
	{
	
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
			
			// blow up!
			myBody.AddExplosionForce(myBody.mass * 2000f, myBody.position, 100);
			myBody.angularVelocity=new Vector3( Random.Range (-100,100), Random.Range (-100,100), Random.Range (-100,100) );
			
			// tell game controller to do a nice big explosion
			GameController_MVD.Instance.PlayerBigHit( myTransform );
			
			// respawn 
			Invoke("Respawn",4f); 
			
			// reset health to full
			myDataManager.SetHealth(startHealthAmount);

		} else {
			
			// tell game controller to do small scale hit
			GameController_MVD.Instance.PlayerHit( myTransform );
				
			// disable and hide weapon
			//weaponControl.DisableCurrentWeapon();
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
		tempTR= raceControl.GetRespawnWaypointTransform();
		tempVEC= tempTR.position;
		
		// cast a ray down from the waypoint to try to find the ground
		RaycastHit hit;
		if(Physics.Raycast(tempVEC + (Vector3.up * 300), -Vector3.up, out hit)){
			tempVEC.y=hit.point.y+15;
		}
		
		// reposition the player at tempVEC (the waypoint position with a corrected y value via raycast)
		// and also we set the player rotation to the waypoint's rotation so that we are facing in the right
		// direction after respawning
		myTransform.rotation= tempTR.rotation;
		myTransform.position= tempVEC;
		
		// we need to be invulnerable for a little while
		MakeInvulnerable();
		
		Invoke ("MakeVulnerable",3);
		
		// revert to the first weapon
		if( weaponControl!=null )
			weaponControl.SetWeaponSlot(0);
	}
	
	void MakeInvulnerable()
	{
		isInvulnerable=true;
		//shieldMesh.SetActive(true);
	}
	
	void MakeVulnerable()
	{
		isInvulnerable=false;
		//shieldMesh.SetActive(false);
	}
	
	public void PlayerFinished()
	{
		// disable this vehicle
		isAIControlled= false;
		canControl= false;
		canFire= false;
		AIController.canControl= false;
		motor= 0;
		steer= 0;
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
	
	void PlayerFinishedRace( int whichPositionDidFinish )
	{
		if( !isAIControlled )
		{
			// tell game controller that the game is finished	
			GameController_MVD.Instance.RaceComplete( whichPositionDidFinish );
			
			// take over the car with AI control
			isAIControlled=true;
			InitAI();
		}
	}
}
