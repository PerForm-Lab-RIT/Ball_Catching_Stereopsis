using System.Collections.Generic;
using UnityEngine;
using UXF;
// using UnityEngine.Animations; // for parentConstraint


public class BallBehavior : MonoBehaviour
{
    //********
    // Ball fade + depth-only timing
    private float T;                 // total time-to-arrival (secondsToPassage)
    private float elapsed;           // time since launch
    private bool switchedToRDS = false; // whether we've switched to DepthOnly layer

    public Renderer ballRenderer;    // assign in prefab

    [Header("Fade Settings")]
    [Tooltip("Slope (alpha change per meter) used while the ball approaches the world X-axis. Use a negative value to fade out as the distance shrinks.")]
    public float alphaFadeSlope = -2f;
    [Tooltip("Distance (meters) from the world X-axis where the ball becomes fully transparent (alpha = 0).")]
    public float alphaZeroDistance = 13.0f;

    //****
    private bool isBeingLaunched;
    private bool isInFlight;

    public AudioClip contactSound;

    [System.NonSerialized] public bool hasBeenCaughtQ = false;

    [System.NonSerialized] public Vector3 contactLocOnPaddle = new Vector3();
    [System.NonSerialized] public Vector3 contactLocinWorld = new Vector3();

    [System.NonSerialized] public float timeOfContact;

    private bool isStuckToPaddle = false;
    private Transform stuckPaddleTransform;

    private Vector3 localContactPointOnPaddle;
    private Vector3 localContactNormalOnPaddle;
    private float stuckRadius = 0f;




    private Session UXF_Session;

    public void Awake()
    {
        UXF_Session = GameObject.FindWithTag("Session").GetComponent<Session>();
        
    }
        
    // void FixedUpdate()
    // {
    //     if (isStuckToPaddle && stuckPaddleTransform != null)
    //     {
    //         // Reconstruct the impact point and normal in world space
    //         Vector3 worldContact = stuckPaddleTransform.TransformPoint(localContactPointOnPaddle);
    //         Vector3 worldNormal  = stuckPaddleTransform.TransformDirection(localContactNormalOnPaddle);

    //         // Place the ball so that its surface is at the contact point
    //         Vector3 desiredPosition = worldContact + worldNormal * stuckRadius;
    //         transform.position = desiredPosition;
    //     }
    // }

 public void placeBall()
    {
        
        gameObject.layer = 1;

        isBeingLaunched = true;
        gameObject.GetComponent<MeshRenderer>().enabled = true;
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.enabled = true;
        }
        

        // Note that this places the ball in preparation for the NEXT trial.
        // The trial, and data output, begins upon launch.
        var tr = UXF_Session.NextTrial;
        
        float secondsToPassage = tr.settings.GetFloat("secondsToPassage");
        List<float> gravity_xyz = tr.settings.GetFloatList("gravity_xyz");
        float initialBallRadiusM = UXF_Session.settings.GetFloat("initialBallRadiusM");
        float eyeHeight = UXF_Session.settings.GetFloat("eyeHeight");
        float armLength = UXF_Session.settings.GetFloat("armLength");
        float passingHeightInHeadHeights = tr.settings.GetFloat("passingHeightInHeadHeights");
        float passingDistanceInArmLengths = tr.settings.GetFloat("passingDistanceInArmLengths");

        bool isLeftHanded = UXF_Session.settings.GetBool("isLeftHanded");
        int isLeftHandedInt = isLeftHanded ? -1 : 1; // So that xPos * isLeftHandedInt will flip the xPos (used below)

        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.None;
        rb.transform.parent = null;

        List<float> ballPassingPos_XYZ = new List<float>{ isLeftHandedInt * armLength  * passingDistanceInArmLengths,
                            eyeHeight * passingHeightInHeadHeights,
                            0};
                            
        tr.settings.SetValue("ballPassingPos_XYZ", ballPassingPos_XYZ);

        List<float> ballInitialPos_XYZ = tr.settings.GetFloatList("ballInitialPos_XYZ");
        gameObject.transform.position = new Vector3(ballInitialPos_XYZ[0], ballInitialPos_XYZ[1], ballInitialPos_XYZ[2]);

        // Randomize initial velocity
        float ballInitialVel_X = (ballPassingPos_XYZ[0] - ballInitialPos_XYZ[0]) / secondsToPassage;
        float ballInitialVel_Y = (-0.5f * gravity_xyz[1] * secondsToPassage * secondsToPassage + (ballPassingPos_XYZ[1] - ballInitialPos_XYZ[1])) / secondsToPassage;
        float ballInitialVel_Z = (-initialBallRadiusM + ballPassingPos_XYZ[2] - ballInitialPos_XYZ[2]) / secondsToPassage;
        List<float> ballInitialVel_XYZ = new List<float> { ballInitialVel_X, ballInitialVel_Y, ballInitialVel_Z };
        tr.settings.SetValue("ballInitialVel_XYZ", new List<float> { ballInitialVel_X, ballInitialVel_Y, ballInitialVel_Z });


        // gameObject.SetActive(true);
        gameObject.GetComponent<Transform>().SetParent(null);
        gameObject.GetComponent<Rigidbody>().useGravity = false;
        gameObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, 0f);

        float ballRad = UXF_Session.settings.GetFloat("initialBallRadiusM");
        gameObject.transform.localScale = new Vector3(ballRad*2.0f,ballRad*2.0f,ballRad*2.0f);

        
        
        if (UXF_Session.settings.GetBool("debugMode"))
        {
            GameObject debugTarget = GameObject.Find("CatchingEnvironment/DebugObjects/Target");
            Debug.Log("entered");
            debugTarget.transform.position = new Vector3(isLeftHandedInt * ballPassingPos_XYZ[0], ballPassingPos_XYZ[1], ballPassingPos_XYZ[2]);

        }

        Debug.Log("Ball has been placed.");

        

    }

    public void launchBall(){

        //DrawTrajectory(2.0f, 0.01f);
        
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
        // Color c = ballRenderer.material.color;
        // c.a = 1f;
        // ballRenderer.material.color = c;

        // Start on normal visible layer
        //gameObject.layer = LayerMask.NameToLayer("Default");

        //******

    }


    public void DrawTrajectory(float simulationTime, float timeStep)
    {
        var tr = UXF_Session.CurrentTrial;
        if (tr == null)
        {
            Debug.LogWarning("DrawTrajectory skipped: no current trial available.");
            return;
        }

        List<float> initialPosList = tr.settings.GetFloatList("ballInitialPos_XYZ");
        List<float> passingPosList = tr.settings.GetFloatList("ballPassingPos_XYZ");

        // Velocity is stored as an object list when authored earlier, so retrieve via GetObject.
        var initialVelObj = tr.settings.GetObject("ballInitialVel_XYZ");
        if (initialVelObj is not List<float> initialVelList)
        {
            Debug.LogWarning("DrawTrajectory skipped: missing ballInitialVel_XYZ data on trial.");
            return;
        }

        Vector3 initialPosition = new Vector3(initialPosList[0], initialPosList[1], initialPosList[2]);
        Vector3 passingPosition = new Vector3(passingPosList[0], passingPosList[1], passingPosList[2]);
        Vector3 initialVelocity = new Vector3(initialVelList[0], initialVelList[1], initialVelList[2]);

        Vector3 gravity = Physics.gravity;
        Vector3 currentPosition = initialPosition;
        Vector3 currentVelocity = initialVelocity;

        for (float t = 0; t < simulationTime; t += timeStep)
        {
            Vector3 nextPosition = currentPosition + currentVelocity * timeStep + 0.5f * gravity * timeStep * timeStep;
            Vector3 nextVelocity = currentVelocity + gravity * timeStep;

            Debug.DrawLine(currentPosition, nextPosition, Color.green, 5f);

            currentPosition = nextPosition;
            currentVelocity = nextVelocity;
        }

        Debug.DrawLine(currentPosition, passingPosition, Color.yellow, 5f);
    }
    public void Update(){
        
        Vector3 vel = gameObject.GetComponent<Rigidbody>().linearVelocity;

        
        if (!isInFlight) return;

        elapsed += Time.deltaTime;

        // Distance from world X-axis is the magnitude in the YZ plane.
        Vector3 pos = transform.position;
        float distanceToXAxis = Mathf.Sqrt(pos.y * pos.y + pos.z * pos.z);

        // Line equation: alpha = slope * distance + intercept, intercept chosen so alpha=0 at alphaZeroDistance.
        // float intercept = -alphaFadeSlope * alphaZeroDistance;
        // float targetAlpha = Mathf.Clamp01(alphaFadeSlope * distanceToXAxis + intercept);

        // Color c = ballRenderer.material.color;
        // c.a = targetAlpha;
        // ballRenderer.material.color = c;
        
        // > 0.5T: switch to depth-only layer once
        if (!switchedToRDS &&  distanceToXAxis < alphaZeroDistance )
        {
            gameObject.layer = 6;   // DepthOnly
            switchedToRDS = true;
        }
                  
    }
    void OnCollisionEnter(Collision collision)
    {
        // Only catch objects on the paddle layer (7)
        if (collision.gameObject.layer != 7) return;

        gameObject.layer = 0;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        if (collision.contactCount == 0) return;

        ContactPoint contact = collision.contacts[0];

        // Only log / stick during an active trial
        if (UXF_Session.CurrentTrial.status == TrialStatus.InProgress)
        {
            AudioSource.PlayClipAtPoint(contactSound, contact.point, 1.0f);

            // Stop physics motion; we will drive the pose explicitly
            rb.constraints = RigidbodyConstraints.None;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Compute ball radius in world units
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                float maxScale = Mathf.Max(transform.lossyScale.x,
                                        transform.lossyScale.y,
                                        transform.lossyScale.z);
                stuckRadius = sphereCollider.radius * maxScale;
            }
            else
            {
                stuckRadius = transform.localScale.x * 0.5f;
            }

            // Store the contact geometry in paddle local coordinates
            stuckPaddleTransform      = collision.collider.transform;
            localContactPointOnPaddle = stuckPaddleTransform.InverseTransformPoint(contact.point);
            localContactNormalOnPaddle =
                stuckPaddleTransform.InverseTransformDirection(contact.normal).normalized;

            isStuckToPaddle = true;

            // Snap immediately so the *surface* of the ball meets the paddle
            Vector3 worldContact = contact.point;
            Vector3 worldNormal  = contact.normal.normalized;
            Vector3 desiredCenter = worldContact + worldNormal * stuckRadius;
            transform.position = desiredCenter;
            transform.rotation = stuckPaddleTransform.rotation;

            // Disable collider while stuck to avoid re-collisions
            if (sphereCollider != null)
                sphereCollider.enabled = false;

            // UXF logging
            UXF_Session.CurrentTrial.result["isCaughtQ"] = true;
            hasBeenCaughtQ = true;
            contactLocOnPaddle = stuckPaddleTransform.InverseTransformPoint(contact.point);
            contactLocinWorld  = contact.point;
            timeOfContact      = Time.time;
        }

        isInFlight = false;
        Debug.Log("Ball has collided and is now stuck to paddle (radius-correct).");
    }

    public void removeBall()
    {
        Destroy(gameObject);
        // gameObject.GetComponent<MeshRenderer>().enabled = false;
        isInFlight = false;

        timeOfContact = float.NaN;
        contactLocOnPaddle = new Vector3(float.NaN, float.NaN, float.NaN);
        contactLocinWorld = new Vector3(float.NaN, float.NaN, float.NaN);
        hasBeenCaughtQ = false;

        // isStuckToPaddle = false;
        // stuckPaddleTransform = null;
        // stuckRadius = 0f;

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

    void LateUpdate()
{
    if (isStuckToPaddle && stuckPaddleTransform != null)
    {
        // Reconstruct contact point and normal in world space
        Vector3 worldContact = stuckPaddleTransform.TransformPoint(localContactPointOnPaddle);
        Vector3 worldNormal  = stuckPaddleTransform.TransformDirection(localContactNormalOnPaddle).normalized;

        // Place ball so its *surface* lies at the contact point
        Vector3 desiredCenter = worldContact + worldNormal * stuckRadius;
        transform.position = desiredCenter;

        // Optional: lock ball orientation to paddle
        transform.rotation = stuckPaddleTransform.rotation;
    }
}

}
