using UnityEngine;
using System.Collections;

[AddComponentMenu("Sample Game Glue Code/Interstellar Paranoids/Stop Scrolling")]

public class StopScrollingScript : MonoBehaviour {
	
	// uses a renderer to detect when this gameObject on-screen, then tells gamecontroller to stop scrolling
	private Renderer myRenderer;
	public Camera theCamera;
	
	void Start ()
	{
		myRenderer=GetComponent<Renderer>();
		
		// the camera may be set in the editor, or we'll just use the main camera
		if(theCamera==null)
			theCamera=Camera.main;
	}
	
	void Update()
	{
		// tell game controller to stop the player moving forward through the level any more
		// if our renderer is on-screen
		if(myRenderer.IsVisibleFrom(theCamera))
			GameController_IP.Instance.StopMovingForward();
	}
}
