// this script is based on car control code from this forum post:
// http://forum.unity3d.com/threads/50643-How-to-make-a-physically-real-stable-car-with-WheelColliders
// by Edy.

using UnityEngine;
using System.Collections;

[AddComponentMenu("Base/Vehicles/Wheeled Vehicle")]

public class BaseVehicle : ExtendedCustomMonoBehaviour 
{
	public WheelCollider frontWheelLeft;
	public WheelCollider frontWheelRight;
	public WheelCollider rearWheelLeft;
	public WheelCollider rearWheelRight;
	
    public float steerMax = 30f;
    public float accelMax = 5000f;
    public float brakeMax = 5000f;

	public float steer = 0f;  
	public float motor = 0f;  
	public float brake = 0f;
	
	// wheel values
	public float wheelMass= 						10;
	public float wheelRadius= 						1.72f;
	public float suspensionDistance= 				1.2f;
	public float suspensionSpring= 					6000;
	public float suspensionDamper= 					90;
	
	public float forwardFrictionExtremumSlip= 		1;
	public float forwardFrictionExtremumValue=		20000;
	public float forwardFrictionAsymptoteSlip=		2;
	public float forwardFrictionAsymptoteValue=		10000;
	public float forwardFrictionStiffnessFactor=	0.008f;
	
	public float sidewaysFrictionExtremumSlip= 		1;
	public float sidewaysFrictionExtremumValue=		20000;
	public float sidewaysFrictionAsymptoteSlip=		2;
	public float sidewaysFrictionAsymptoteValue=	10000;
	public float sidewaysFrictionStiffnessFactor=	0.006f;
	
	[System.NonSerialized]
	public float mySpeed;

    public bool isLocked;

	[System.NonSerialized]
	public Vector3 velo;
	
	[System.NonSerialized]
	public Vector3 flatVelo;

	public BasePlayerManager myPlayerController; 
	
	public Keyboard_Input default_input;
	
	public AudioSource engineSoundSource;
	public float audioPitchOffset= 0.5f;
	
	public virtual void Start ()
	{
		Init ();	
	}
	
	public virtual void Init ()
	{
		Debug.Log ("BaseVehicle Init called.");
		
		// cache the usual suspects
		myBody= GetComponent<Rigidbody>();
		myGO= gameObject;
		myTransform= transform;
		
		// add default keyboard input
		if( myGO.GetComponent<Keyboard_Input>()==null )
			default_input= myGO.AddComponent<Keyboard_Input>();
		
		// cache a reference to the player controller
		myPlayerController= myGO.GetComponent<BasePlayerManager>();
		
		// call base class init
		myPlayerController.Init();
		
		// with this simple vehicle code, we set the center of mass low to try to keep the car from toppling over
		myBody.centerOfMass= new Vector3(0,-4f,0);
		
		// see if we can find an engine sound source, if we need to
		if( engineSoundSource==null )
		{
			engineSoundSource= myGO.GetComponent<AudioSource>();
		}
		
		// now lets copy over our friction and suspension values to the wheels (keeping these values in one place
		// rather than having to change them on each wheel is a neater way to work!)
		
		// set up curves for front and sideways values
		
		WheelFrictionCurve curveF = new WheelFrictionCurve();

        curveF.extremumSlip = forwardFrictionExtremumSlip;
	    curveF.extremumValue = forwardFrictionExtremumValue;
        curveF.asymptoteSlip = forwardFrictionAsymptoteSlip;
        curveF.asymptoteValue = forwardFrictionAsymptoteValue;
        curveF.stiffness = forwardFrictionStiffnessFactor;
		
		WheelFrictionCurve curveS = new WheelFrictionCurve();

        curveS.extremumSlip = sidewaysFrictionExtremumSlip;
	    curveS.extremumValue = sidewaysFrictionExtremumValue;
        curveS.asymptoteSlip = sidewaysFrictionAsymptoteSlip;
        curveS.asymptoteValue = sidewaysFrictionAsymptoteValue;
        curveS.stiffness = sidewaysFrictionStiffnessFactor;
		
		// set up JointSpring for the suspension settings
		JointSpring suspensionJointSpring = new JointSpring();
		suspensionJointSpring.damper = suspensionDamper;
		suspensionJointSpring.spring= suspensionSpring;
		
		// front left
		frontWheelLeft.mass= wheelMass;
		frontWheelLeft.radius= wheelRadius;
		
		frontWheelLeft.suspensionDistance= suspensionDistance;
		frontWheelLeft.suspensionSpring= suspensionJointSpring;
		
        frontWheelLeft.forwardFriction = curveF;
		frontWheelLeft.sidewaysFriction = curveS;
	
		// front right
		frontWheelRight.mass= wheelMass;
		frontWheelRight.radius= wheelRadius;
		
		frontWheelRight.suspensionDistance= suspensionDistance;
		frontWheelRight.suspensionSpring= suspensionJointSpring;
		
		frontWheelRight.forwardFriction = curveF;
		frontWheelRight.sidewaysFriction = curveS;
		
		// rear left
		rearWheelLeft.mass= wheelMass;
		rearWheelLeft.radius= wheelRadius;
		
		rearWheelLeft.suspensionDistance= suspensionDistance;
		rearWheelLeft.suspensionSpring= suspensionJointSpring;
		
		rearWheelLeft.forwardFriction = curveF;
		rearWheelLeft.sidewaysFriction = curveS;
		
		// rear right	
		rearWheelRight.mass= wheelMass;
		rearWheelRight.radius= wheelRadius;
		
		rearWheelRight.suspensionDistance= suspensionDistance;
		rearWheelRight.suspensionSpring= suspensionJointSpring;
		
		rearWheelRight.forwardFriction = curveF;
		rearWheelRight.sidewaysFriction = curveS;
		
		Debug.Log ("BaseVehicle wheels setup and Init() complete.");
	}
	
	public void SetUserInput( bool setInput )
	{
		canControl= setInput;	
	}

    public void SetLock(bool lockState)
    {
        isLocked = lockState;
    }

	public virtual void LateUpdate()
	{
		// we check for input in LateUpdate because Unity recommends this
		if(canControl)
			GetInput();
		
		// update the audio
		UpdateEngineAudio();
	}
	
    public virtual void FixedUpdate()
    {
		UpdatePhysics();
    }
	
	public virtual void UpdateEngineAudio()
	{
		// this is just a 'made up' multiplier value applied to mySpeed.
		engineSoundSource.pitch= audioPitchOffset + ( Mathf.Abs( mySpeed ) * 0.005f );
	}
		
	public virtual void UpdatePhysics()
	{
        CheckLock();

		// grab the velocity of the rigidbody and convert it into flat velocity (remove the Y)
		velo= myBody.velocity;

		// convert the velocity to local space so we can see how fast we're moving forward (along the local z axis)
		velo= transform.InverseTransformDirection(myBody.velocity);
		flatVelo.x= velo.x;
		flatVelo.y= 0;
		flatVelo.z= velo.z;
		
		// work out our current forward speed
		mySpeed= velo.z;
		
		// if we're moving slow, we reverse motorTorque and remove brakeTorque so that the car will reverse
		if( mySpeed<2 )
		{
			// that is, if we're pressing down the brake key (making brake>0)
			if( brake>0 )
			{
				rearWheelLeft.motorTorque = -brakeMax * brake;
		        rearWheelRight.motorTorque = -brakeMax * brake;
				
				rearWheelLeft.brakeTorque = 0;
        		rearWheelRight.brakeTorque = 0;
				
		        frontWheelLeft.steerAngle = steerMax * steer;
		        frontWheelRight.steerAngle = steerMax * steer;
				
				// drop out of this function before applying the 'regular' non-reversed values to the wheels
				return;
			}
		}
		
		// apply regular movement values to the wheels
		rearWheelLeft.motorTorque = accelMax * motor;
        rearWheelRight.motorTorque = accelMax * motor;

        rearWheelLeft.brakeTorque = brakeMax * brake;
        rearWheelRight.brakeTorque = brakeMax * brake;

        frontWheelLeft.steerAngle = steerMax * steer;
        frontWheelRight.steerAngle = steerMax * steer;
	}

    public void CheckLock()
    {
        if (isLocked)
        {
            // control is locked out and we should be stopped
            steer = 0;
            brake = 0;
            motor = 0;

            // hold our rigidbody in place (but allow the Y to move so the car may drop to the ground if it is not exactly matched to the terrain)
            tempVEC = myBody.velocity;
            tempVEC.x = 0;
            tempVEC.z = 0;
            myBody.velocity = tempVEC;
        }
    }

	public virtual void GetInput()
	{
		// calculate steering amount
		steer= Mathf.Clamp( default_input.GetHorizontal() , -1, 1 );
		
		// how much accelerator?
        motor= Mathf.Clamp( default_input.GetVertical() , 0, 1 );
		
		// how much brake?
		brake= -1 * Mathf.Clamp( default_input.GetVertical() , -1, 0 );
	}
	
}