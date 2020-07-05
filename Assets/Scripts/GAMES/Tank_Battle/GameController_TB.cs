// the lovely square font used for this game can be found here http://www.dafont.com/vikas-kumar.d3807
// it's called Square Display and it is by Vikas Kumar

using UnityEngine;
using System.Collections;

[AddComponentMenu( "Sample Game Glue Code/Tank Battle/Game Controller" )]

public class GameController_TB : BaseGameController
{
	public string mainMenuSceneName = "menu_TB";
    public int numberOfBattlers = 4;
	public int gameTime= 120;

    public Transform playerParent;
    public Transform [] startPoints;
    public Camera_Third_Person cameraScript;

    [System.NonSerialized]
    public GameObject playerGO1;

    private CarController_TB thePlayerScript;
    private CarController_TB focusPlayerScript;

    private ArrayList playerList;
	private ArrayList playerTransforms;
	
    private float aSpeed;
	
    public GUIText timerText;
    public GUIText posText;
	
	private bool isAhead;
    private CarController_TB car2;
    private int focusPlayerBattlePosition;

    public GameObject count3;
    public GameObject count2;
    public GameObject count1;

    public GUIText finalPositionText;

    public GameObject [] playerPrefabList;

    [System.NonSerialized]
    public static GameController_TB Instance;
	
	public Waypoints_Controller WaypointControllerForAI;
	
    // scale time here
    public float gameSpeed = 1;
	private bool didInit;
		
	private TimerClass theTimer;
	
    public GameController_TB ()
    {
        Instance = this;
    }

    void Start ()
    {
      	Init();
    }

    void Init ()
    {
		if(playerList!=null)
		{
			Debug.ClearDeveloperConsole();
			Debug.Log ("playerList.Count ="+playerList.Count);
			Debug.Break();
		}
		
		SpawnController.Instance.Restart();
		
        // incase we need to change the timescale, it gets set here
        Time.timeScale = gameSpeed;
		
		// tell battle manager to prepare for the battle
		GlobalBattleManager.Instance.InitNewBattle ();
		
        // initialize some temporary arrays we can use to set up the players
        Vector3 [] playerStarts = new Vector3 [numberOfBattlers];
        Quaternion [] playerRotations = new Quaternion [numberOfBattlers];

        // we are going to use the array full of start positions that must be set in the editor, which means we always need to
        // make sure that there are enough start positions for the number of players

        for ( int i = 0; i < numberOfBattlers; i++ )
        {
            // grab position and rotation values from start position transforms set in the inspector
            playerStarts [i] = (Vector3) startPoints [i].position;
            playerRotations [i] = ( Quaternion ) startPoints [i].rotation;
        }
		
        SpawnController.Instance.SetUpPlayers( playerPrefabList, playerStarts, playerRotations, playerParent, numberOfBattlers );
		
		playerTransforms=new ArrayList();
		
		// now let's grab references to each player's controller script
		playerTransforms = SpawnController.Instance.GetAllSpawnedPlayers();
		
		playerList=new ArrayList();
		
		for ( int i = 0; i < numberOfBattlers; i++ )
        {
			Transform tempT= (Transform)playerTransforms[i];
			CarController_TB tempController= tempT.GetComponent<CarController_TB>();
			
			playerList.Add(tempController);
						
			BaseAIController tempAI=tempController.GetComponent<BaseAIController>();
			
			tempController.Init ();
			
			if( i>0 )
			{
				// grab a ref to the player's gameobject for later
        		playerGO1 = SpawnController.Instance.GetPlayerGO( 0 );
				
				// tell AI to get the player!
				tempAI.SetChaseTarget( playerGO1.transform );
				
				// set AI mode to chase
				tempAI.SetAIState( AIStates.AIState.steer_to_target );
			}
		}
				
		// add an audio listener to the first car so that the audio is based from the car rather than the main camera
		playerGO1.AddComponent<AudioListener>();
		
		// look at the main camera and see if it has an audio listener attached
		AudioListener tempListener= Camera.main.GetComponent<AudioListener>();
		
		// if we found a listener, let's destroy it
		if( tempListener!=null )
			Destroy(tempListener);

        // grab a reference to the focussed player's car controller script, so that we can
        // do things like access its speed variable
        thePlayerScript = ( CarController_TB ) playerGO1.GetComponent<CarController_TB>();

        // assign this player the id of 0 - this is important. The id system is how we will know who is firing bullets!
        thePlayerScript.SetID( 0 );

        // set player control
        thePlayerScript.SetUserInput( true );
		
        // as this is the user, we want to focus on this for UI etc.
        focusPlayerScript = thePlayerScript;

        // tell the camera script to target this new player
        cameraScript.SetTarget( playerGO1.transform );

        // lock all the players on the spot until we're ready to go
        SetPlayerLocks( true );

        // start the game in 3 seconds from now
        Invoke( "StartGame", 4 );
		
		// initialize a timer, but we won't start it right away. It gets started in the FinishedCount() function after the count-in
		theTimer = ScriptableObject.CreateInstance<TimerClass>();
		
		// update positions throughout the battle, but we don't need
        // to do this every frame, so just do it every half a second instead
        InvokeRepeating( "UpdatePositions", 0f, 0.5f );
		
        // hide our count in numbers
        HideCount();

        // schedule count in messages
        Invoke( "ShowCount3", 1 );
        Invoke( "ShowCount2", 2 );
        Invoke( "ShowCount1", 3 );
        Invoke( "FinishedCount", 4 );

        // hide final position text
        finalPositionText.gameObject.SetActive( false );
        doneFinalMessage = false;
		
		didInit=true;
    }
	
    void StartGame ()
    {
		// the SetPlayerLocks function tells all players to unlock
        SetPlayerLocks( false );
		
		// tell battle manager to start the battle!
		GlobalBattleManager.Instance.StartBattle();
    }
	
	void UpdatePositions()
	{		
		// update the display
		UpdateBattlePositionText();
	}

	void UpdateBattlePositionText ()
    {
		// get a string back from the timer to display on-screen
        timerText.text = theTimer.GetFormattedTime();
		
		// get the current player position scoreboard from the battle manager and show it via posText.text
		posText.text = GlobalBattleManager.Instance.GetPositionListString();
		
		// check the timer to see how much time we've been playing. If it's more than gameTime,
		// the game is over
		if( theTimer.GetTime() > gameTime )
		{
			// end the game
			BattleComplete();
		}
    }
	
    void SetPlayerLocks ( bool aState )
    {
        // tell all of the players to set their locks
        for ( int i = 0; i < numberOfBattlers; i++ )
        {
			thePlayerScript = ( CarController_TB ) playerList [i];
            thePlayerScript.SetLock( aState );
        }
    }

    private bool doneFinalMessage;

    public void BattleComplete ()
    {
		// tell battle manager we're done
		GlobalBattleManager.Instance.StopBattle();
		
		// stop the timer!
		theTimer.StopTimer();
		
		// now display a message to tell the user the result of the battle
        if ( !doneFinalMessage )
        {
			// get the final position for our local player (which is made first, so always has the id 1)
			int finalPosition= GlobalBattleManager.Instance.GetPosition(1);
			
			if ( finalPosition == 1 )
				finalPositionText.text = "FINISHED 1st";

			if ( finalPosition == 2 )
				finalPositionText.text = "FINISHED 2nd";

			if ( finalPosition == 3 )
				finalPositionText.text = "FINISHED 3rd";

			if ( finalPosition >= 4 )
				finalPositionText.text = "GAME OVER";

			doneFinalMessage = true;
			
			finalPositionText.gameObject.SetActive(true);
			
			// drop out of the scene completely in 10 seconds...
			Invoke( "FinishBattle", 10 );
        }
    }

	void FinishBattle ()
    {
        Application.LoadLevel( mainMenuSceneName );
    }

    void ShowCount1 ()
    {
        count1.SetActive( true );
        count2.SetActive( false );
        count3.SetActive( false );
    }
    void ShowCount2 ()
    {
        count1.SetActive( false );
        count2.SetActive( true );
        count3.SetActive( false );
    }
    void ShowCount3 ()
    {
        count1.SetActive( false );
        count2.SetActive( false );
        count3.SetActive( true );
    }
	
	void FinishedCount ()
	{
		HideCount ();
		
		// let the timer begin!
		theTimer.StartTimer();
	}
    void HideCount ()
    {
        count1.SetActive( false );
        count2.SetActive( false );
        count3.SetActive( false );
    }
	
	public void PlayerHit(Transform whichPlayer)
	{
		// tell our sound controller to play an explosion sound
		BaseSoundController.Instance.PlaySoundByIndex( 1, whichPlayer.position );
		
		// call the explosion function!
		//Explode( whichPlayer.position );
	}
	
	public void PlayerBigHit(Transform whichPlayer)
	{
		// tell our sound controller to play an explosion sound
		BaseSoundController.Instance.PlaySoundByIndex( 2, whichPlayer.position );
		
		// call the explosion function!
		Explode( whichPlayer.position );
	}
	
	public void Explode ( Vector3 aPosition )
	{		
		// instantiate an explosion at the position passed into this function
		Instantiate( explosionPrefab,aPosition, Quaternion.identity );
	}	
}