using UnityEngine;
using System.Collections;

[AddComponentMenu("Sample Game Glue Code/Interstellar Paranoids/Player Space Ship")]

public class Player_SpaceShip_IP : BaseTopDownSpaceShip 
{
	private Standard_SlotWeaponController weaponControl;

	private bool isInvulnerable;
	private bool isRespawning;
	
	public bool isMouseControlled;
	private bool fire_input;
	
	// alternative control method
	private Mouse_Input mouse_input;
	
	public GameObject theMeshGO;
	public GameObject shieldMesh;
	
	public BasePlayerManager myPlayerManager;
	public BaseUserManager myDataManager;
	
	public bool godMode =true;
	public int ownerID =-1;
	
	public override void Start()
	{
		// we want to keep the player object alive right through the game, so we use DontDestroyOnLoad to keep it alive
		DontDestroyOnLoad (this.transform);
		
		didInit=false;

		// tell our base class to initialize
		base.Init ();
		
		// now do our own init
		this.Init();
	}
	
	public override void Init ()
	{			
		// hide the invulnerability shield(!)
		if(!godMode)
		{
			MakeVulnerable();
		} else {
			MakeInvulnerable();
		}
		
		// get a ref to the weapon controller
		weaponControl= myGO.GetComponent<Standard_SlotWeaponController>();
		
		// tell weapon control who we are (so all weapon control can tell projectiles who sent them)
		weaponControl.SetOwner(ownerID);
			
		// if a player manager is not set in the editor, let's try to find one
		if(myPlayerManager==null)
			myPlayerManager= myGO.GetComponent<BasePlayerManager>();
		
		myDataManager= myPlayerManager.DataManager;
		myDataManager.SetName("Player");
		myDataManager.SetHealth(3);
		
		// update UI lives
		if(ownerID==1)
		{
			// if our owner ID is 1, we must be player 1
			GameController_IP.Instance.UpdateLivesP1(myDataManager.GetHealth());
		} else {
			// we are player 2, so set that UI instead
			GameController_IP.Instance.UpdateLivesP2(myDataManager.GetHealth());
		}
		
		if(isMouseControlled)
		{
			// if we are going to use mouse controls, add a mouse input controller
			mouse_input= gameObject.AddComponent<Mouse_Input>();
		}
		
		didInit=true;
	}
	
	public override void Update ()
	{
		// don't do anything until Init() has been run
		if(!didInit)
			return;

		// do the update in our base
		UpdateShip ();

		// check to see if we're supposed to be controlling the player before checking for firing
		if(!canControl)
			return;
		
		// fire if we need to
		if(fire_input)
		{
			// tell weapon controller to deal with firing
			weaponControl.Fire();
		}
	}
	
	public override void GetInput ()
	{
		if(isMouseControlled)
		{
			// we're overridding the default input function to add in the ability to fire
			horizontal_input= mouse_input.GetHorizontal();
			vertical_input= mouse_input.GetVertical();
			
			// firing isn't in the default spaceship (BaseTopDownSpaceShip.cs) behaviour, so we add it here
			fire_input= mouse_input.GetFire();
		} else {
			// we're overridding the default input function to add in the ability to fire
			horizontal_input= default_input.GetHorizontal();
			vertical_input= default_input.GetVertical();
			
			// firing isn't in the default spaceship (BaseTopDownSpaceShip.cs) behaviour, so we add it here
			fire_input= default_input.GetFire();
		}
    }
	
	void OnCollisionEnter(Collision collider)
	{
		// MAKE SURE that weapons don't have colliders
		// if you are using primitives, only use a single collider on the same gameobject which has this script on
		
		// when something collides with our ship, we check its layer to see if it is on 11 which is our projectiles
		// (Note: remember when you add projectiles to set the layer correctly!)
		if(collider.gameObject.layer==17 && !isRespawning && !isInvulnerable)
		{
			LostLife();
		}
	}
	
	void OnTriggerEnter(Collider other)
	{
		if( other.gameObject.layer==12 )
		{
			// tell our sound controller to play a powerup sound
			BaseSoundController.Instance.PlaySoundByIndex( 3, myTransform.position );
			
			// hit a powerup trigger
			Destroy ( other.gameObject );
			
			// advance to the next weapon
			weaponControl.NextWeaponSlot( false );
		}
	}
	
	void LostLife()
	{
		isRespawning=true;
			
		// blow us up!
		GameController_IP.Instance.PlayerHit( myTransform );
			
		// reduce lives by one
		myDataManager.ReduceHealth(1);
		
		// update UI lives
		if( ownerID==1 )
		{
			// as our ID is 1, we must be player 1
			GameController_IP.Instance.UpdateLivesP1( myDataManager.GetHealth() );
		} else {
			// as our ID is 2, we must be player 2
			GameController_IP.Instance.UpdateLivesP2( myDataManager.GetHealth() );
		}
		
		if(myDataManager.GetHealth()<1) // <- game over
		{
			// hide ship body
			theMeshGO.SetActive(false);
			
			// disable and hide weapon
			weaponControl.DisableCurrentWeapon();
			
			// do anything we need to do at game finished
			PlayerFinished();
		} else {
			// hide ship body
			theMeshGO.SetActive(false);
			
			// disable and hide weapon
			weaponControl.DisableCurrentWeapon();
			
			// respawn 
			Invoke("Respawn",2f);
		}
	}
	
	void Respawn()
	{
		// reset the 'we are respawning' variable
		isRespawning= false;
		
		// we need to be invulnerable for a little while
		MakeInvulnerable();
		
		Invoke ("MakeVulnerable",3);
		// show ship body again
		theMeshGO.SetActive(true);
		
		// revert to the first weapon
		weaponControl.SetWeaponSlot(0);
		
		// show the current weapon (since it was hidden when the ship explosion was shown)
		weaponControl.EnableCurrentWeapon();
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
	
	public void PlayerFinished()
	{
		// tell the player controller that we have finished
		GameController_IP.Instance.PlayerDied( ownerID );
	}
}




