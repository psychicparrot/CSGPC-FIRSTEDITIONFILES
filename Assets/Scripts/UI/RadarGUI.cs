// This script is based on the script from the Unity wiki (originally posted by me!)

using UnityEngine;
using System.Collections;
 
public class RadarGUI : MonoBehaviour 
{
	public Transform centerObject;
	
	public Texture enemyBlipTexture;
	public Texture radarBackgroundTexture;
 
	private Vector2 drawCenterPosition;
	public Vector2 drawOffset= new Vector2(5,5);
	
	public Vector2 drawBlipOffset;
	
	public string defaultTagFilter= "enemy";
	public float mapScale= 0.3f;
	public float maxDist= 200;
	public float mapWidth= 256;
	public float mapHeight= 256;
	
	public bool rotateAroundPlayer;
	
 	private ArrayList radarList;
	private ArrayList textureList;
	
	private Transform tempTRANS;
	private Texture tempTEXTURE;
	
	private float dist;
	private float dx;
	private float dz;
	private float deltay;
	private float bX;
	private float bY;
	private Vector3 centerPos;
	private Vector3 extPos;
	
	public enum Positioning {
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,
	}
	
	public Positioning drawPosition;
	
	void Start()
	{
		SetUpRadar();
	}
	
	void SetUpRadar()
	{
		// set up arraylists to hold transforms and textures
		radarList= new ArrayList();
		textureList= new ArrayList();
		
		// Find all game objects with default tag on
	    GameObject[] gos = GameObject.FindGameObjectsWithTag(defaultTagFilter); 
 
	    // Iterate through them
	    foreach (GameObject go in gos)  
	    {
			AddBlipToList(go.transform, enemyBlipTexture);
		}
	}
	 
	public void SetCenterObject( Transform aTransform )
	{
		centerObject= aTransform;
	}
	
	public void AddBlipToList( Transform transformToAdd, Texture aBlip )
	{
		// add transform and textures to arraylists
		radarList.Add ( transformToAdd );
		textureList.Add ( aBlip );
	}
	
	// we also have an publically available method for adding blips so that we don't have to do a FindGameObjectsWithTag
	// every time we add something new into the world. For 
	public void AddEnemyBlipToList( Transform transformToAdd )
	{
		// add transform and textures to arraylists
		radarList.Add ( transformToAdd );
		
		// for this, we will assume that we are adding an enemy and use the enemy blip
		textureList.Add ( enemyBlipTexture );
	}
	
	// we also provide a method for other scripts to remove blips 
	public void RemoveEnemyBlip( Transform transformToRemove )
	{
		radarList.Remove( transformToRemove );	
	}
	
	public void DrawRadar()
	{
		// calculate center position
		CalcCenter();
		
		// draw our radar background
	 	GUI.DrawTexture( new Rect( drawCenterPosition.x - ( mapWidth / 2 ) , drawCenterPosition.y - ( mapHeight / 2 ), mapWidth, mapHeight ), radarBackgroundTexture );
		
		// now iterate through the radarList to draw each blip
		for(int i=0; i<radarList.Count; i++)
		{			
			// draw its blip on the radar
			drawBlip( ( Transform ) radarList[i], ( Texture ) textureList[i] );
		}
	}
	
	void OnGUI() 
	{
		// transform the matrix to scale to different sized windows
		//GUI.matrix = Matrix4x4.TRS ( Vector3.zero, Quaternion.identity, new Vector3( Screen.width / 1024f, Screen.height / 768f, 1 ));
		
		// draw the radar
		DrawRadar();
	}
		
	private void drawBlip ( Transform go, Texture aTexture )
	{
		// if this is null, we need to do another scan for blips
		if(go==null)
			SetUpRadar();
		
		try
		{
			
			centerPos= centerObject.position;
			extPos= go.position;
		} catch {
			return;	
		}
		
		// first we need to get the distance of the enemy from the player
		dist= Vector3.Distance( centerPos, extPos );
 
		dx= centerPos.x - extPos.x; // how far to the side of the player is the enemy?
		dz= centerPos.z - extPos.z; // how far in front or behind the player is the enemy?
 
		if(rotateAroundPlayer)
		{
			// what's the angle to turn to face the enemy - compensating for the player's turning?
			deltay= Mathf.Atan2( dx, dz ) * Mathf.Rad2Deg - 270 - centerObject.eulerAngles.y;
		} else {
			// what's the angle to turn to face the enemy - compensating for the player's turning?
			deltay= Mathf.Atan2( dx, dz ) * Mathf.Rad2Deg -270;
		}
		// just basic trigonometry to find the point x,y (enemy's location) given the angle deltay
		bX= dist * Mathf.Cos( deltay * Mathf.Deg2Rad );
		bY= dist * Mathf.Sin( deltay * Mathf.Deg2Rad );
 
		bX= bX * mapScale; // scales down the x-coordinate by half so that the plot stays within our radar
		bY= bY * mapScale; // scales down the y-coordinate by half so that the plot stays within our radar
 
		if( dist<= maxDist )
		{ 
			// draw the blip
		   GUI.DrawTexture( new Rect( drawCenterPosition.x + bX + drawBlipOffset.x, drawCenterPosition.y + bY + drawBlipOffset.y, aTexture.width, aTexture.height ), aTexture );
		}
	}
 
	private void CalcCenter()
	{
		switch( drawPosition )
		{
		case Positioning.TopLeft:
			// top left
			drawCenterPosition.x= drawOffset.x + ( mapWidth / 2 );
			drawCenterPosition.y= drawOffset.y + ( mapHeight / 2 );
			break;
			
		case Positioning.TopRight:
			// top right
			drawCenterPosition.x= Screen.width - drawOffset.x - ( mapWidth / 2 );
			drawCenterPosition.y= drawOffset.y + ( mapHeight / 2 );
			break;
			
		case Positioning.BottomLeft:
			// bottom left
			drawCenterPosition.x= drawOffset.x + ( mapWidth / 2 );
			drawCenterPosition.y= Screen.height - ( drawOffset.y + ( mapHeight / 2 ) );
			break;
			
		case Positioning.BottomRight:
			// bottom right
			drawCenterPosition.x= Screen.width - drawOffset.x - ( mapWidth / 2 );
			drawCenterPosition.y= Screen.height - ( drawOffset.y + ( mapHeight / 2 ) );
			break;
		}
	}
	
}