using UnityEngine;
using System.Collections;

[AddComponentMenu("Sample Game Glue Code/Interstellar Paranoids/Enemy control")]

public class Enemy_IP : BaseArmedEnemy {
	
	private bool isRespawning;
	
	// here we add collision and respawning to the base armed enemy behaviour
	
	public void OnCollisionEnter(Collision collider) 
	{
		// when something collides with us, we check its layer to see if it is on 9 which is our projectiles
		// (Note: remember when you add projectiles to set the layer of the weapon parent correctly!)
		if( collider.gameObject.layer==9 && !isRespawning )
		{
			myDataManager.ReduceHealth(1);
			 
			if( myDataManager.GetHealth()==0 )
			{
				tempINT= int.Parse( collider.gameObject.name );
				
				// tell game controller to make an explosion at our position and to award the player points for hitting us
				TellGCEnemyDestroyed();
				
				// if this is a boss enemy, tell the game controller when we get destroyed so it can end the level
				if( isBoss )
					TellGCBossDestroyed();
				
				// destroy this
				Destroy(gameObject);
			}
		}
	}
	
	// game controller specifics (which will be overridden for different game controller scripts)
	// ------------------------------------------------------------------------------------------
	
	public virtual void TellGCEnemyDestroyed()
	{
		GameController_IP.Instance.EnemyDestroyed( myTransform.position, pointsValue, tempINT );
	}
	
	public virtual void TellGCBossDestroyed()
	{
		GameController_IP.Instance.BossDestroyed();
	}
	
	// ------------------------------------------------------------------------------------------
	
}
