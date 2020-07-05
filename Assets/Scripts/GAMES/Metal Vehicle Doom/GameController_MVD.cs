// the lovely square font used for this game can be found here http://www.dafont.com/vikas-kumar.d3807
// it's called Square Display and it is by Vikas Kumar

using UnityEngine;
using System.Collections;

[AddComponentMenu( "Sample Game Glue Code/Metal Vehicle Destruction/Game Controller" )]

public class GameController_MVD : BaseGameController
{
    public string mainMenuSceneName = "menu_MVD";
	public int totalLaps = 3;
    public int numberOfRacers = 4;

    public Transform playerParent;
    public Transform [] startPoints;
    public Camera_Third_Person cameraScript;

    [System.NonSerialized]
    public GameObject playerGO1;

    private CarController_MVD thePlayerScript;
    private CarController_MVD focusPlayerScript;

    private ArrayList playerList;
	private ArrayList playerTransforms;
	
    private float aSpeed;
	
    public GUIText lapText;
    public GUIText posText;

    // position checking
    private int myPos;
    private int theLap;
	
	private bool isAhead;
    private CarController_MVD car2;
    private int focusPlayerRacePosition;

    public GameObject count3;
    public GameObject count2;
    public GameObject count1;

    public GUIText finalPositionText;

    public GameObject [] playerPrefabList;

    [System.NonSerialized]
    public static GameController_MVD Instance;
	
	public Waypoints_Controller WaypointControllerForAI;
	
    // scale time here
    public float gameSpeed = 1;
	private bool didInit;
	
	public GameObject wrongWaySign;
	private bool oldIsWrongWay;
	
    public GameController_MVD ()
    {
        Instance = this;
    }

    void Start ()
    {
      	Init();
    }

    void Init ()
    {
        // incase we need to change the timescale, it gets set here
        Time.timeScale = gameSpeed;
		
		// tell race manager to prepare for the race
		GlobalRaceManager.Instance.InitNewRace( totalLaps );
		
        // initialize some temporary arrays we can use to set up the players
        Vector3 [] playerStarts = new Vector3 [numberOfRacers];
        Quaternion [] playerRotations = new Quaternion [numberOfRacers];

        // we are going to use the array full of start positions that must be set in the editor, which means we always need to
        // make sure that there are enough start positions for the number of players

        for ( int i = 0; i < numberOfRacers; i++ )
        {
            // grab position and rotation values from start position transforms set in the inspector
            playerStarts [i] = (Vector3) startPoints [i].position;
            playerRotations [i] = ( Quaternion ) startPoints [i].rotation;
        }
		
        SpawnController.Instance.SetUpPlayers( playerPrefabList, playerStarts, playerRotations, playerParent, numberOfRacers );
		
		playerTransforms=new ArrayList();
		
		// now let's grab references to each player's controller script
		playerTransforms = SpawnController.Instance.GetAllSpawnedPlayers();
		
		playerList=new ArrayList();
		
		for ( int i = 0; i < numberOfRacers; i++ )
        {
			Transform tempT= (Transform)playerTransforms[i];
			CarController_MVD tempController= tempT.GetComponent<CarController_MVD>();
			
			playerList.Add(tempController);
						
			BaseAIController tempAI=tempController.GetComponent<BaseAIController>();
			
			// tell each player where to find the waypoints
			tempAI.SetWayController(WaypointControllerForAI);
			
			tempController.Init ();
			
			// tell the car controller script about the waypoint controller so it can pass it on to the racecontroller (!)
			tempController.SetWayController(WaypointControllerForAI);
		}
				
        // grab a ref to the player's gameobject for later
        playerGO1 = SpawnController.Instance.GetPlayerGO( 0 );
		
		// add an audio listener to the first car so that the audio is based from the car rather than the main camera
		playerGO1.AddComponent<AudioListener>();
		
		// look at the main camera and see if it has an audio listener attached
		AudioListener tempListener= Camera.main.GetComponent<AudioListener>();
		
		// if we found a listener, let's destroy it
		if( tempListener!=null )
			Destroy(tempListener);

        // grab a reference to the focussed player's car controller script, so that we can
        // do things like access its speed variable
		focusPlayerScript = ( CarController_MVD ) playerGO1.GetComponent<CarController_MVD>();

        // assign this player the id of 0
		focusPlayerScript.SetID( 0 );

        // set player control
		focusPlayerScript.SetUserInput( true );

        // tell the camera script to target this new player
        cameraScript.SetTarget( playerGO1.transform );

        // do initial lap counter display
        UpdateLapCounter( 1 );

        // lock all the players on the spot until we're ready to go
        SetPlayerLocks( true );

        // start the game in 3 seconds from now
        Invoke( "StartRace", 4 );

        // update positions throughout the race, but we don't need
        // to do this every frame, so just do it every half a second instead
        InvokeRepeating( "UpdatePositions", 0.5f, 0.5f );

        // hide our count in numbers
        HideCount();

        // schedule count in messages
        Invoke( "ShowCount3", 1 );
        Invoke( "ShowCount2", 2 );
        Invoke( "ShowCount1", 3 );
        Invoke( "HideCount", 4 );

        // hide final position text
        finalPositionText.gameObject.SetActive( false );
        doneFinalMessage = false;

        // start by hiding our wrong way message
        wrongWaySign.SetActive( false );
		
		didInit=true;
    }
		
    void StartRace ()
    {
		// the SetPlayerLocks function tells all players to unlock
        SetPlayerLocks( false );
		
		// tell the global race manager that we are now racing
		GlobalRaceManager.Instance.StartRace();
    }

    void SetPlayerLocks ( bool aState )
    {
        // tell all of the players to set their locks
        for ( int i = 0; i < numberOfRacers; i++ )
        {
			thePlayerScript = ( CarController_MVD ) playerList [i];
            thePlayerScript.SetLock( aState );
        }
    }

	void UpdatePositions()
	{
		// here we need to talk to the race controller to get what we need to display on screen
		focusPlayerRacePosition= GlobalRaceManager.Instance.GetPosition(1);
		theLap= GlobalRaceManager.Instance.GetLapsDone(1) +1;
		
		// update the display
		UpdateRacePositionText();
		UpdateLapCounter(theLap);
	}
	
    void UpdateLapCounter ( int theLap )
    {
        // if we've finished all the laps we need to finish, let's cap the number so that we can
        // have the AI cars continue going around the track without any negative implications
        if ( theLap > totalLaps )
            theLap = totalLaps;

        // now we set the text of our GUIText object lap count display
        lapText.text = "Lap " + theLap.ToString() + " of " + totalLaps.ToString();
    }

    void UpdateRacePositionText ()
    {
        posText.text = "Pos " + focusPlayerRacePosition.ToString() + " of " + numberOfRacers;
    }

    private bool doneFinalMessage;

    public void RaceComplete ( int finalPosition )
    {
        if ( !doneFinalMessage )
        {
			if ( finalPosition == 1 )
				finalPositionText.text = "FINISHED 1st";

			if ( finalPosition == 2 )
				finalPositionText.text = "FINISHED 2nd";

			if ( finalPosition == 3 )
				finalPositionText.text = "FINISHED 3rd";

			if ( finalPosition >= 4 )
				finalPositionText.text = "FINISHED";

			doneFinalMessage = true;
			
			finalPositionText.gameObject.SetActive(true);
			
			// drop out of the race scene completely in 10 seconds...
			Invoke( "FinishRace", 10 );
        }
    }

    void FinishRace ()
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
    void HideCount ()
    {
        count1.SetActive( false );
        count2.SetActive( false );
        count3.SetActive( false );
    }
	
    public void UpdateWrongWay ( bool isWrongWay )
    {
		 if( isWrongWay==oldIsWrongWay)
			return;
		
		if ( isWrongWay )
        {
            wrongWaySign.SetActive( true );
        }
        else
        {
            wrongWaySign.SetActive( false );
        }
		
		oldIsWrongWay=isWrongWay;
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