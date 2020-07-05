using UnityEngine;
using System.Collections;

public class SimplePlayerController : BaseTopDownSpaceShip 
{
	public BasePlayerManager myPlayerManager;
	public BaseUserManager myDataManager;
	
	public override void Start()
	{
		// tell our base class to initialize
		base.Init ();
		
		// now do our own init
		this.Init();
	}
	
	public override void Init ()
	{			
		// if a player manager is not set in the editor, let's try to find one
		if(myPlayerManager==null)
			myPlayerManager= myGO.GetComponent<BasePlayerManager>();
		
		myDataManager= myPlayerManager.DataManager;
		myDataManager.SetName("Player");
		myDataManager.SetHealth(3);
		
		didInit=true;
	}
	
	public override void Update ()
	{
		// do the update in our base
		UpdateShip ();
		
		// don't do anything until Init() has been run
		if(!didInit)
			return;
		
		// check to see if we're supposed to be controlling the player before checking for firing
		if(!canControl)
			return;
	}
	
	public override void GetInput ()
	{
		// we're overridding the default input function to add in the ability to fire
		horizontal_input= default_input.GetHorizontal();
		vertical_input= default_input.GetVertical();
	}
	
	void OnCollisionEnter(Collision collider)
	{
		// React to collisions here
	}
	
	void OnTriggerEnter(Collider other)
	{
		// React to triggers here
	}
	
	public void PlayerFinished()
	{
		// Deal with the end of the game for this player
	}
	
	public void ScoredPoints(int howMany)
	{
		myDataManager.AddScore( howMany );
	}
}




