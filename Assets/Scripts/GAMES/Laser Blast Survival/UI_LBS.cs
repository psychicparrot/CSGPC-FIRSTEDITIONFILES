using UnityEngine;
using System.Collections;

[AddComponentMenu("Sample Game Glue Code/Laser Blast Survival/In-Game UI")]

public class UI_LBS : BaseUIDataManager
{
	public GameObject gameOverMessage;
	public GameObject getReadyMessage;
	
	void Awake()
	{
		Init();
	}
	
	void Init()
	{
		LoadHighScore();
		
		HideMessages ();
		
		Invoke("ShowGetReady",1);
		Invoke("HideMessages",2);
		
	}
	
	public void HideMessages()
	{
		gameOverMessage.SetActive(false);
		getReadyMessage.SetActive(false);
	}

	public void ShowGetReady()
	{
		getReadyMessage.SetActive(true);
	}

	public void ShowGameOver()
	{
		SaveHighScore();
		
		// show the game over message
		gameOverMessage.SetActive(true);
	}
	
	void OnGUI()
	{
		GUI.Label(new Rect (10,10,100,50),"PLAYER 1");
		GUI.Label(new Rect (10,40,100,50),"SCORE "+player_score);
		GUI.Label(new Rect (10,70,200,50),"HIGH SCORE "+player_highscore);
		
		GUI.Label(new Rect (10,100,100,50),"LIVES "+player_lives);
	}
}
