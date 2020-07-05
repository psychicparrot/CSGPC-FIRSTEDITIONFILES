using UnityEngine;
using System.Collections;

[AddComponentMenu("Sample Game Glue Code/Interstellar Paranoids/In-Game UI")]

public class UI_IP : BaseUIDataManager
{
	public int scorePlayer2;
	public int livesPlayer2;
	
	public bool isTwoPlayer;
	
	public GameObject gameOverMessage;
	public GameObject getReadyMessage;
	
	public void Init()
	{
		HideMessages ();
		
		Invoke("ShowGetReady",1);
		Invoke("HideMessages",4);
		
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
		gameOverMessage.SetActive(true);
	}
	
	void OnGUI()
	{
		GUI.Label(new Rect (10,10,100,50),"PLAYER 1");
		GUI.Label(new Rect (10,40,100,50),"SCORE "+player_score);
		GUI.Label(new Rect (10,70,100,50),"LIVES "+player_lives);
		
		GUI.Label(new Rect (10,100,200,50),"HIGH SCORE "+player_highscore);
		
		if(!isTwoPlayer)
		{
			GUI.Label(new Rect (Screen.width-90,10,100,50),"PLAYER 2");
			GUI.Label(new Rect (Screen.width-90,40,100,50),"SCORE "+scorePlayer2);
			GUI.Label(new Rect (Screen.width-90,70,100,50),"LIVES "+livesPlayer2);	
		}
	}
			
	public void UpdateScoreP2(int aScore)
	{
		scorePlayer2=aScore;
	}
	
	public void UpdateLivesP2(int alifeNum)
	{
		livesPlayer2=alifeNum;
	}
}
