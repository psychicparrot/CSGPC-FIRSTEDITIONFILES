using UnityEngine;
using System.Collections;

[AddComponentMenu("Sample Game Glue Code/Interstellar Paranoids/Game Controller")]

public class GameController_IP : BaseGameController 
{
	public string mainMenuSceneName = "menu_IP";

	public bool didInit;
	public Transform playerParent;
	public GameObject[] playerPrefabList;
	
	public GameObject powerUpPrefab;
	
	public float levelForwardSpeed =1f;
	public Transform CameraTransform;
	
	[System.NonSerialized]
	public GameObject playerGO1;
	public GameObject playerGO2;
	
	private Player_SpaceShip_IP playerScript1;
	private Player_SpaceShip_IP playerScript2;
	
	public BaseUserManager mainPlayerDataManager1;
	public BaseUserManager mainPlayerDataManager2;
		
	private int powerupExplosionCounter =0;
	public int numberOfExplosionsToMakePowerup =10;
	
	public UI_IP UIControl;
	public UI_GameOver gameOverUIScript;
	
	public BaseCameraController cameraControl;
	
	private int tempINT;
	private Vector3 tempVec3;
	private Quaternion tempQuat;
	
	private bool isStopped;
	
	[System.NonSerialized]
	public static GameController_IP Instance;
	
	public float gameSpeed=8;
	
	public bool BossWinToNextLevel= true;
	
	// there should always be one player
	public int totalPlayers= 1;
	
	public SceneManager sceneManager;
	
	public float CameraStartPositionZ= -11;
	
	public GameController_IP()
	{
		Instance=this;
	}
	
	public void Start()
	{
		// we want to keep the game controller alive right through the game, so we use DontDestroyOnLoad to keep it alive
		DontDestroyOnLoad (this.gameObject);
	}
	
	public void LevelLoadingComplete()
	{
		Init();
		UIControl.Init();
	}
	
	public void Init()
	{
		// tell the player that it can move, in 4 seconds
		Invoke ("StartPlayer",4);
		
		// incase we need to change the timescale, it gets set here
		Time.timeScale=gameSpeed;
		
		// we store the current game's number of players in a pref so it may be set elsewhere (the main menu for example) and
		// carried into every level of the game.
		if(PlayerPrefs.HasKey( "totalPlayers" )) // does this pref exist?
		{
			totalPlayers= PlayerPrefs.GetInt( "totalPlayers" ); // then use the value it holds
		} else {
			totalPlayers= 1; // default to single player
		}
		
		// find player parent transform
		playerParent= GameObject.Find("Player_Parent_Object").transform;
		
		Vector3[] playerStarts= new Vector3[totalPlayers];
		Quaternion[] playerRotations= new Quaternion[totalPlayers];
		
		// this may be a little over-the-top, but hard coding it just wouldn't fit in with the overall theme of re-use
		for(int i=0; i<totalPlayers; i++)
		{
			tempQuat= Quaternion.identity;
			
			if(i==0)
			{
				// place player 1 at the default start position of -5,0,0
				tempVec3= new Vector3( -5, 0, 0 );
			} else {
				// we'll make player 2 a start position 5 units to the right of the start position of player 1
				tempVec3= new Vector3( -5 + (i*5), 0, 0 );
			}
			playerStarts[i]=tempVec3;
			playerRotations[i]=tempQuat;
		}
		
		// if we haven't already got players set up, didInit will still be false.. otherwise we skip creating the players
		if(!didInit)
			SpawnController.Instance.SetUpPlayers( playerPrefabList, playerStarts, playerRotations, playerParent, totalPlayers );
		
		// grab a ref to the player's gameobject for later
		playerGO1= SpawnController.Instance.GetPlayerGO( 0 );
		
		// if we have a two player game, let's grab that second player's gameobject too
		if( totalPlayers>1 )
			playerGO2= SpawnController.Instance.GetPlayerGO( 1 );
		
		// find the game camera
		CameraTransform = GameObject.Find("GameCamera").transform;
		
		// position the camera at the specified start position (set in the Unity editor Inspector window on this component)
		tempVec3 = CameraTransform.localPosition;
		tempVec3.z = CameraStartPositionZ;
		CameraTransform.localPosition = tempVec3;
		
		// if we don't have a camera control script object set by the editor, try to find one
		cameraControl= CameraTransform.GetComponent<BaseCameraController>();
		
		isStopped=false;
		
		// make sure we have a scene manager to talk to
		GetSceneManager ();
		
		didInit=true;
	}
	
	public void LateUpdate ()
	{
		if(!isStopped)
		{
			// do fly movement through the level
			CameraTransform.Translate( Vector3.up * Time.deltaTime * levelForwardSpeed );
		}
	}
	
	public void StartPlayer ()
	{
		Debug.Log ("StartPlayer!!!");
		// find the player's control script and hold it in playerScript
		playerScript1= playerGO1.GetComponent<Player_SpaceShip_IP>();
		
		mainPlayerDataManager1= playerGO1.GetComponent<BasePlayerManager>().DataManager;
		
		// all ready to play, let's go!
		playerGO1.SendMessage( "GameStart" );
				
		// now, if there *is* a player 2, let's tell it to get going
		if(totalPlayers>1)
		{
			// find the player's control script and hold it in playerScript
			playerScript2= playerGO2.GetComponent<Player_SpaceShip_IP>();
			
			mainPlayerDataManager2= playerGO2.GetComponent<BasePlayerManager>().DataManager;
		
			playerGO2.SendMessage( "GameStart" );
		}
	}
		
	public void StopMovingForward ()
	{
		isStopped=true;
	}
	
	public void ContinueForward()
	{
		isStopped=false;
	}
	
	public void BossDestroyed()
	{
		ContinueForward();
		
		if( BossWinToNextLevel )
		{
			// go to next level
			Invoke("FinishedLevel", 3f);
		}
	}
	
	public void FinishedLevel ()
	{
		// make sure we have a scene manager to talk to
		GetSceneManager ();
		
		// tell scene manager to load the next level
		if( sceneManager != null )
		{
			sceneManager.GoNextLevel();
		} else {
			Debug.LogError("SCENE MANAGER DOES NOT EXIST. CAN'T MOVE TO NEXT LEVEL!");	
		}
	}
	
	void GetSceneManager ()
	{
		// find level loader object
		GameObject sceneManagerGO = GameObject.Find ( "SceneManager" );
		
		// check to see if we managed to find a manager object before trying to get at its script
		if( sceneManagerGO!=null )
			sceneManager= sceneManagerGO.GetComponent<SceneManager>();
	}
	
	public override void EnemyDestroyed ( Vector3 aPosition, int pointsValue, int hitByID )
	{
		// tell our sound controller to play an explosion sound
		BaseSoundController.Instance.PlaySoundByIndex( 1, aPosition );
		
		// play an explosion effect at the enemy position
		Explode ( aPosition );
		
		if(hitByID==1)
		{
			// tell main data manager to add score
			mainPlayerDataManager1.AddScore( pointsValue );
			
			// update the score on the UI
			UpdateScoreP1( mainPlayerDataManager1.GetScore() );
		} else {
			// tell main data manager to add score
			mainPlayerDataManager2.AddScore( pointsValue );
		
			// update the score on the UI
			UpdateScoreP2( mainPlayerDataManager2.GetScore() );
		}
		
		// count how many have been destroyed and if necessary spawn a powerup here instead
		powerupExplosionCounter++;
		if( powerupExplosionCounter>numberOfExplosionsToMakePowerup )
		{
			Instantiate( powerUpPrefab,aPosition,Quaternion.identity );
			powerupExplosionCounter=0;
		}
	}
	
	public void Explode ( Vector3 aPosition )
	{		
		// instantiate an explosion at the position passed into this function
		Instantiate( explosionPrefab,aPosition, Quaternion.identity );
	}
	
	public void PlayerHit(Transform whichPlayer)
	{
		// tell our sound controller to play an explosion sound
		BaseSoundController.Instance.PlaySoundByIndex( 2, whichPlayer.position );
		
		// call the explosion function!
		Explode( whichPlayer.position );
	}
	
	// UI update calls
	// 
	public void UpdateScoreP1( int aScore )
	{
		if( UIControl != null )
			UIControl.UpdateScoreP1( aScore );
	} 
	
	public void UpdateLivesP1( int aScore )
	{
		if( UIControl != null )
			UIControl.UpdateLivesP1( aScore );
	}

	public void UpdateScoreP2( int aScore )
	{
		if( UIControl != null )
			UIControl.UpdateScoreP2( aScore );
	} 
	
	public void UpdateLivesP2( int aScore )
	{
		if( UIControl != null )
			UIControl.UpdateLivesP2( aScore );
	}
	

		
	private bool player1Dead;
	private bool player2Dead;
	
	public void PlayerDied(int whichID)
	{
		if(whichID==1)
			player1Dead=true;
		
		if(whichID==2)
			player2Dead=true;
		
		if(player1Dead && player2Dead && totalPlayers>1)
		{
			// both players are dead, so end the game
			UIControl.ShowGameOver();
			Invoke ("Exit",5);
		} else if(totalPlayers==1)
		{
			// this is a single player game, so just end the game now
			// both players are dead, so end the game
			UIControl.ShowGameOver();
			Invoke ("Exit",5);
		}
	}
	
	void Exit()
	{
		SpawnController.Instance.Restart();
		Destroy( this.gameObject );
		
		// make sure we have a scene manager to talk to
		GetSceneManager ();
		
		sceneManager.LoadLevel( mainMenuSceneName );
	}
}