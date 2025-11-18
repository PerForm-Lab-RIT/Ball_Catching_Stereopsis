using System.Collections.Generic;
using UnityEngine;
using UXF;
using UnityEngine.Animations; // for parentConstraint

public class BallBehavior : MonoBehaviour
{
    //********
    // Ball fade + depth-only timing
    private float T;                 // total time-to-arrival (secondsToPassage)
    private float elapsed;           // time since launch
    private bool switchedToRDS = false; // whether we've switched to DepthOnly layer

    public Renderer ballRenderer;    // assign in prefab

    //****
    private bool isBeingLaunched;
    private bool isInFlight;

    public AudioClip contactSound;

    [System.NonSerialized] public bool hasBeenCaughtQ = false;

    [System.NonSerialized] public Vector3 contactLocOnPaddle = new Vector3();
    [System.NonSerialized] public Vector3 contactLocinWorld = new Vector3();

    [System.NonSerialized] public float timeOfContact;


    private Session UXF_Session;

    public void Awake()
    {
        UXF_Session = GameObject.FindWithTag("Session").GetComponent<Session>();
        
    }
    

 public void placeBall()
    {
        
        gameObject.layer = 8;

        isBeingLaunched = true;
        gameObject.GetComponent<MeshRenderer>().enabled = true;
        

        // Note that this places the ball in preparation for the NEXT trial.
        // The trial, and data output, begins upon launch.
        var tr = UXF_Session.NextTrial;
        

        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.None;
        rb.transform.parent = null;

        List<float> ballInitialPos_XYZ = tr.settings.GetFloatList("ballInitialPos_XYZ");
        List<float> ballPassingPos_XYZ = tr.settings.GetFloatList("ballPassingPos_XYZ");
        List<float> ballInitialVel_XYZ = tr.settings.GetFloatList("ballInitialVel_XYZ");
        

        float currentBlock = tr.block.number;
        float currentTrial = tr.numberInBlock;
    
        Debug.Log("*** B:" + currentBlock + " T:" + currentTrial + " initialPos: " + ballInitialPos_XYZ);
        Debug.Log("*** B:" + currentBlock + " T:" + currentTrial + " passingPos: " + ballPassingPos_XYZ);
        Debug.Log("*** B:" + currentBlock + " T:" + currentTrial + " initialVel: " + ballInitialVel_XYZ);

        Debug.Log($"*** B:{currentBlock} T:{currentTrial} initialPos: [{string.Join(", ", ballInitialPos_XYZ)}]");
        Debug.Log($"*** B:{currentBlock} T:{currentTrial} passingPos: [{string.Join(", ", ballPassingPos_XYZ)}]");
        Debug.Log($"*** B:{currentBlock} T:{currentTrial} initialVel: [{string.Join(", ", ballInitialVel_XYZ)}]");


        // gameObject.SetActive(true);
        gameObject.GetComponent<Transform>().SetParent(null);
        gameObject.GetComponent<Rigidbody>().useGravity = false;
        gameObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, 0f);

        float ballRad = UXF_Session.settings.GetFloat("initialBallRadiusM");
        gameObject.transform.localScale = new Vector3(ballRad*2.0f,ballRad*2.0f,ballRad*2.0f);

        gameObject.transform.localPosition = new Vector3(ballInitialPos_XYZ[0], ballInitialPos_XYZ[1], ballInitialPos_XYZ[2]);
        

        if (UXF_Session.settings.GetBool("debugMode"))
        {
            GameObject debugTarget = GameObject.Find("CatchingEnvironment/DebugObjects/Target");
            Debug.Log("entered");
            bool isLeftHanded = UXF_Session.settings.GetBool("isLeftHanded");
            int isLeftHandedInt = isLeftHanded ? -1 : 1;
            debugTarget.transform.position = new Vector3(isLeftHandedInt * ballPassingPos_XYZ[0], ballPassingPos_XYZ[1], ballPassingPos_XYZ[2]);

        }

        Debug.Log("Ball has been placed.");

    }

    public void launchBall(){


        Debug.Log("A.");

        //    inflateOrDeflate = true;
        gameObject.GetComponent<Rigidbody>().useGravity = true;

        Debug.Log("B.");

        var ballInitialVel_XYZ = (List<float>)UXF_Session.CurrentTrial.settings.GetObject("ballInitialVel_XYZ");
        gameObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(ballInitialVel_XYZ[0], ballInitialVel_XYZ[1], ballInitialVel_XYZ[2]);
        

        Debug.Log("Ball has been launched.");

        isBeingLaunched = false;
        isInFlight = true;

        
        // Initialize layer timing
        T = UXF_Session.CurrentTrial.settings.GetFloat("secondsToPassage");
        elapsed = 0f;
        switchedToRDS = false;

        // Start fully visible
        Color c = ballRenderer.material.color;
        c.a = 1f;
        ballRenderer.material.color = c;

        // Start on normal visible layer
        gameObject.layer = LayerMask.NameToLayer("Default");

        //******

    }

    public void Update(){
        
        Vector3 vel = gameObject.GetComponent<Rigidbody>().linearVelocity;

        // if( isInFlight ){
        //     scaleBallRadiusByGain();
        // }
        //Ball changes to RDS environment
        
        if (!isInFlight) return;

        elapsed += Time.deltaTime;
        float halfT = T * 0.5f;

        // 0 → 0.5T: 
        if (elapsed <= halfT)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / halfT);
            Color c = ballRenderer.material.color;
            c.a = alpha;
            ballRenderer.material.color = c;
        }

        // > 0.5T: switch to depth-only layer once
        if (!switchedToRDS && elapsed > halfT)
        {
            gameObject.layer = 6;   // DepthOnly
            switchedToRDS = true;
        }
                  
    }

    // void OnCollisionEnter(Collision collision){
    //     // Only proceed if the collision is with a paddle during an active trial
    //     if (!collision.gameObject.CompareTag("Paddle")) return;
    //     if (UXF_Session.CurrentTrial.status != TrialStatus.InProgress) return;

    //     // Ensure there is a contact point
    //     if (collision.contactCount == 0) return;
    //     ContactPoint contact = collision.contacts[0];

    //     // Debug visualization
    //     Debug.DrawRay(contact.point, contact.normal, Color.green, 2f);

    //     // Get the Rigidbody for this ball
    //     Rigidbody rb = GetComponent<Rigidbody>();
    //     if (rb == null) return;

    //     // Play contact sound
    //     AudioSource.PlayClipAtPoint(contactSound, contact.point, 1.0f);

    //     // Stop any motion
    //     rb.linearVelocity = Vector3.zero;
    //     rb.angularVelocity = Vector3.zero;
    //     rb.useGravity = false;

    //     // Add a FixedJoint to attach the ball to the paddle’s rigidbody
    //     // (If the paddle is kinematic, this still works correctly in Unity physics)
    //     FixedJoint joint = gameObject.AddComponent<FixedJoint>();
    //     joint.connectedBody = collision.rigidbody;  // Connect to the paddle’s rigidbody
    //     joint.breakForce = Mathf.Infinity;
    //     joint.breakTorque = Mathf.Infinity;

    //     // Record relevant data for your UXF trial
    //     UXF_Session.CurrentTrial.result["isCaughtQ"] = true;
    //     hasBeenCaughtQ = true;
    //     contactLocOnPaddle = collision.transform.InverseTransformPoint(contact.point);
    //     contactLocinWorld = contact.point;
    //     timeOfContact = Time.time;

    //     // Optional: adjust local offset for left vs. right handedness
    //     Vector3 localPos = transform.localPosition;
    //     float offset = transform.localScale.x / 2.0f;
    //     localPos.x = UXF_Session.settings.GetBool("isLeftHanded") ? offset : -offset;
    //     transform.localPosition = localPos;

    //     // Log
    //     Debug.Log("Ball attached to paddle with FixedJoint.");
    //     isInFlight = false;
    // }



    void OnCollisionEnter(Collision collision)
    {

        Debug.DrawRay(collision.contacts[0].point, collision.contacts[0].normal, Color.green, 2, false);
        // Debug.Log( "collide (name) : " + collision.collider.gameObject.name );
        // Debug.Log( "collide (tag) : " + collision.collider.gameObject.tag );
        gameObject.layer =0 ;

        

        Rigidbody rb = gameObject.GetComponent<Rigidbody>();

        ContactPoint contact = collision.contacts[0];
        gameObject.transform.position = contact.point;


        if( collision.contacts[0].otherCollider.CompareTag("Paddle") && UXF_Session.CurrentTrial.status == TrialStatus.InProgress)  {

            AudioSource.PlayClipAtPoint(contactSound, contact.point, 1.0f);

            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = new Vector3(0f, 0f, 0f);

            gameObject.GetComponent<Transform>().SetParent(collision.contacts[0].otherCollider.transform.parent);


            UXF_Session.CurrentTrial.result["isCaughtQ"] = true;
            hasBeenCaughtQ = true;
            contactLocOnPaddle = collision.contacts[0].otherCollider.transform.InverseTransformPoint(contact.point);
            contactLocinWorld = contact.point;

            if( UXF_Session.settings.GetBool("isLeftHanded") ){
                transform.localPosition = new Vector3( transform.localScale.x/2.0f, transform.localPosition.y, transform.localPosition.z );
            }
            else{
                transform.localPosition = new Vector3( -transform.localScale.x/2.0f, transform.localPosition.y, transform.localPosition.z );
            }


            timeOfContact = Time.time;
        }

        Debug.Log("Ball has collided and is now a child.");

        

        isInFlight = false;

    }

    public void removeBall()
    {

        Destroy(gameObject);

        gameObject.GetComponent<MeshRenderer>().enabled = false;
        isInFlight = false;

        timeOfContact = float.NaN;
        contactLocOnPaddle = new Vector3(float.NaN, float.NaN, float.NaN);
        contactLocinWorld = new Vector3(float.NaN, float.NaN, float.NaN);
        hasBeenCaughtQ = false;



    }

     public void scaleBallRadiusByGain() {

        Vector3 curCameraPos_XYZ = Camera.main.transform.position;
        Vector3 curBallPos_XYZ = transform.position;
        Rigidbody ballRb = gameObject.GetComponent<Rigidbody>();

        float xzDist = Mathf.Sqrt( Mathf.Pow(curBallPos_XYZ.x - curCameraPos_XYZ.x ,2.0f) + Mathf.Pow(curBallPos_XYZ.z-curCameraPos_XYZ.z ,2.0f));
        
        // THis can be refined ... I'm not using hte ball's approach speed to the person
        // ...I am using the ball's speed through the world.
        float xzVel =  Mathf.Sqrt( Mathf.Pow( ballRb.linearVelocity.x,2) + Mathf.Pow(ballRb.linearVelocity.y,2));
        float timeToArrival = xzDist / xzVel;
        
        if( timeToArrival <= UXF_Session.settings.GetFloat("noExpansionLastXSeconds")){
            return;
        }

        var tr = UXF_Session.CurrentTrial;
        float expansionGain = tr.settings.GetFloat("expansionGain");
        
        float curDistFromViewToBall = Vector3.Distance(curCameraPos_XYZ, curBallPos_XYZ);
        float curAngularRadiusRadians = Mathf.Atan( (transform.localScale[0]/2.0f) / curDistFromViewToBall);

        // maybe we should get the ACtual delta time?
        Vector3 nextBallPos_XYZ = curBallPos_XYZ + ballRb.linearVelocity * Time.fixedDeltaTime; // Note this is FIXED deltaTime

        float nextDistFromViewToBall = Vector3.Distance(curCameraPos_XYZ, nextBallPos_XYZ);
        float nextAngularRadiusRadians = Mathf.Atan((transform.localScale[0]/2.0f) / nextDistFromViewToBall);

        // What would the angular radius be on the next frame, if scaled by the gain term?
        float scaledAngularRadiusRads = curAngularRadiusRadians + (nextAngularRadiusRadians - curAngularRadiusRadians) * expansionGain;

        // What physical radius (m) would bring about this angular subtense?
        float newBallRadius = nextDistFromViewToBall * Mathf.Tan(scaledAngularRadiusRads);

        transform.localScale = new Vector3(newBallRadius*2.0f, newBallRadius * 2.0f, newBallRadius * 2.0f);

        // this code assumes that each component of the balls local scale is equal to its diameter        

    }
}
