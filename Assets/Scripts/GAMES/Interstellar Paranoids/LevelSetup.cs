using UnityEngine;
using System.Collections;

public class LevelSetup : MonoBehaviour 
{
	public GameObject GameControllerPrefab;
	public GameObject SceneManagerPrefab;
	
	private GameObject gameControllerGO;
	private GameObject checker;
	
	void Awake ()
	{
		// quick null check, to be safe
		if( GameControllerPrefab != null )
		{
			// first, we check to see if there is already an instance of the prefab in the scene
			gameControllerGO = GameObject.Find ( GameControllerPrefab.name );
			
			// if the prefab is not already in the scene, we create one here
			if( gameControllerGO == null )
				gameControllerGO = (GameObject) Instantiate( GameControllerPrefab );
			
			// rename it to get rid of Unity's default naming
			gameControllerGO.name = GameControllerPrefab.name;
		}
		
		// quick null check, to be safe
		if( SceneManagerPrefab != null )
		{
			// first, we check to see if there is already an instance of the prefab in the scene
			checker = GameObject.Find ( SceneManagerPrefab.name );
			
			// if the prefab is not already in the scene, we create one here
			if( checker == null )
				checker = (GameObject) Instantiate( SceneManagerPrefab );
			
			// rename it to get rid of Unity's default naming
			checker.name = SceneManagerPrefab.name;
		}
		
		// now everything is done, we should be safe to tell the game controller that the scene is ready to use
		gameControllerGO.SendMessage( "LevelLoadingComplete" );
	}
}
