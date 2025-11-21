using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class experimentGenerator : MonoBehaviour
{
    public Session UXF_Session = null;
    public GameObject ballPrefab;
    public GameObject paddle;

    public bool debugMode = true;
    public bool sessionHasBeenCreated = false;

    //public bool triggerCablibration = true;      
    //public ChangeResetBoxColor changeBoxColor;

    private bool bodyDimensionsSet;
    private bool resetControllerLocation;
    public GameObject resetController;

    private bool isLeftHanded;

    private bool trialInProgress = false;

    public Transform handTransform;

    public InputActionReference set_body_dimensions_action;
    public InputActionReference launch_ball_action;

    public void Start()
    {
        

        GameObject.Find("CatchingEnvironment").SetActive(true);
        set_body_dimensions_action.action.performed += OnSetBodyDimensions;
        launch_ball_action.action.performed += onLaunchBall;

        
        //GameObject.Find("CatchingEnvironment/DebugObjects").SetActive(false);
        
    }

    public void generateSession()
    {
        Debug.Log("Generating experiment, blocks, and trials.");
        
        // Debug mode is not fully implemented, but one could take advantage of this property..
        // e.g. if debugmode, show additional diagnostic information
        UXF_Session.settings.SetValue("debugMode", debugMode);

        float eyeHeight = UXF_Session.settings.GetFloat("eyeHeight");
        float armLength = UXF_Session.settings.GetFloat("armLength");

        bool isLeftHanded = UXF_Session.settings.GetBool("isLeftHanded");
        int isLeftHandedInt = isLeftHanded ? -1 : 1; // So that xPos * isLeftHandedInt will flip the xPos (used below)

        List<float> gravity_xyz = UXF_Session.settings.GetFloatList("gravity_xyz");        
        Physics.gravity = new Vector3(gravity_xyz[0], gravity_xyz[1], gravity_xyz[2]);

        List<string> blockList = UXF_Session.settings.GetStringList("blockList");

        List<float> launchPlanePosition_XYZ = UXF_Session.settings.GetFloatList("launchPlanePosition_XYZ");
        float launchPlaneWidth = UXF_Session.settings.GetFloat("launchPlaneWidth");
        float launchPlaneHeight = UXF_Session.settings.GetFloat("launchPlaneHeight");

        float initialBallRadiusM = UXF_Session.settings.GetFloat("initialBallRadiusM");

        List<float> passingHeightInHeadHeights_minMax = UXF_Session.settings.GetFloatList("passingHeightInHeadHeights_minMax");
        List<float> passingDistancesInArmLengths = UXF_Session.settings.GetFloatList("passingDistancesInArmLengths");
        List<float> secondsToPassage_minMax = UXF_Session.settings.GetFloatList("secondsToPassage_minMax");

        foreach (var blockPrefix in blockList)
        {
            List<float> expGains = UXF_Session.settings.GetFloatList(blockPrefix + "_expansionGains");
            List<float> trialRepetitions = UXF_Session.settings.GetFloatList(blockPrefix + "_trialRepetitions");
            List<float> passingDistanceInArmLengths = UXF_Session.settings.GetFloatList(blockPrefix + "_passingDistanceInArmLengths");

            Block block = UXF_Session.CreateBlock();

            int count = 0;

            foreach (float dist in passingDistancesInArmLengths)
            {
                foreach (float gain in expGains)
                {
                    for (int rep = 1; rep <= trialRepetitions[count]; rep++)
                    {

                        Trial newTrial = block.CreateTrial();

                        // Store expansionGain, passingDistance, repetitionNumber
                        newTrial.settings.SetValue("expansionGain", gain);
                        newTrial.settings.SetValue("repetitionNumber", rep);

                        // Randomize seconds to passage / time of flight before passing subject's plane
                        float secondsToPassage = UnityEngine.Random.Range(secondsToPassage_minMax[0], secondsToPassage_minMax[1]);
                        newTrial.settings.SetValue("secondsToPassage", secondsToPassage);

                        // Randomize passingHeightInHeadHeights
                        float passingHeightInHeadHeights = UnityEngine.Random.Range(passingHeightInHeadHeights_minMax[0], passingHeightInHeadHeights_minMax[1]);
                        newTrial.settings.SetValue("passingHeightInHeadHeights", passingHeightInHeadHeights);

                        // Randomize passing position
                        // List<float> ballPassingPos_XYZ = new List<float>{ isLeftHandedInt * armLength  * dist,
                        //     eyeHeight * passingHeightInHeadHeights,
                        //     0};
                            
                        // newTrial.settings.SetValue("ballPassingPos_XYZ", ballPassingPos_XYZ);
                        newTrial.settings.SetValue("passingDistanceInArmLengths", dist);

                        // Randomize ballInitialPos_XYZ
                        float ballInitialPos_X = isLeftHandedInt * UnityEngine.Random.Range(launchPlanePosition_XYZ[0] - launchPlaneWidth / 2.0f, launchPlanePosition_XYZ[0] + launchPlaneWidth / 2.0f);
                        float ballInitialPos_Y = UnityEngine.Random.Range(launchPlanePosition_XYZ[1] - launchPlaneHeight / 2.0f, launchPlanePosition_XYZ[1] + launchPlaneHeight / 2.0f);
                        float ballInitialPos_Z = launchPlanePosition_XYZ[2];

                        List<float> ballInitialPos_XYZ = new List<float> { ballInitialPos_X, ballInitialPos_Y, ballInitialPos_Z };
                        newTrial.settings.SetValue("ballInitialPos_XYZ", ballInitialPos_XYZ);

                        // // Randomize initial velocity
                        // float ballInitialVel_X = (ballPassingPos_XYZ[0] - ballInitialPos_XYZ[0]) / secondsToPassage;
                        // float ballInitialVel_Y = (-0.5f * gravity_xyz[1] * secondsToPassage * secondsToPassage + (ballPassingPos_XYZ[1] - ballInitialPos_XYZ[1])) / secondsToPassage;
                        // float ballInitialVel_Z = (-initialBallRadiusM + ballPassingPos_XYZ[2] - ballInitialPos_XYZ[2]) / secondsToPassage;

                        // List<float> ballInitialVel_XYZ = new List<float> { ballInitialVel_X, ballInitialVel_Y, ballInitialVel_Z };
                        // newTrial.settings.SetValue("ballInitialVel_XYZ", new List<float> { ballInitialVel_X, ballInitialVel_Y, ballInitialVel_Z });
                    }

                    count += 1;

                }
            }

            block.trials.Shuffle();

        }
        
        
        sessionHasBeenCreated = true;

    }
    
    void repositionDebugObjects()
    {
       
        var debugParentGO = GameObject.Find("CatchingEnvironment/DebugObjects");

        if( debugParentGO is null ){ return; }
        
        debugParentGO.SetActive(true);

        bool isLeftHanded = UXF_Session.settings.GetBool("isLeftHanded");
        int isLeftHandedInt = isLeftHanded ? -1 : 1; // So that xPos * isLeftHandedInt will flip the xPos (used below)

        float eyeHeight = UXF_Session.settings.GetFloat("eyeHeight");
        float armLength = UXF_Session.settings.GetFloat("armLength");

        float launchPlaneWidth = UXF_Session.settings.GetFloat("launchPlaneWidth");
        float launchPlaneHeight = UXF_Session.settings.GetFloat("launchPlaneHeight");

        List<float> passingHeightInHeadHeights_minMax = UXF_Session.settings.GetFloatList("passingHeightInHeadHeights_minMax");
        List<float> passingDistancesInArmLengths = UXF_Session.settings.GetFloatList("passingDistancesInArmLengths");
        //List<float> secondsToPassage_minMax = UXF_Session.settings.GetFloatList("secondsToPassage_minMax");
        List<float> launchPlanePosition_XYZ = UXF_Session.settings.GetFloatList("launchPlanePosition_XYZ");

        float stripHeightM = (passingHeightInHeadHeights_minMax[1] - passingHeightInHeadHeights_minMax[0]) * eyeHeight;

        Debug.Log("Adjusting passing zone debug objects.");

        var pZone = GameObject.Find("CatchingEnvironment/DebugObjects/PassingZone");

        var strip = pZone.transform.Find("nearStrip");
        strip.localPosition = new Vector3(isLeftHandedInt * armLength * passingDistancesInArmLengths[0], eyeHeight, strip.localPosition.z);
        strip.localScale = new Vector3(strip.localScale.x, stripHeightM, strip.localScale.z);

        strip = pZone.transform.Find("midStrip");
        strip.localPosition = new Vector3(isLeftHandedInt * armLength * passingDistancesInArmLengths[1], eyeHeight, strip.localPosition.z);
        strip.localScale = new Vector3(strip.localScale.x, stripHeightM, strip.localScale.z);

        strip = pZone.transform.Find("farStrip");
        strip.localPosition = new Vector3(isLeftHandedInt * armLength * passingDistancesInArmLengths[2], eyeHeight, strip.localPosition.z);
        strip.localScale = new Vector3(strip.localScale.x, stripHeightM, strip.localScale.z);

        var backing = pZone.transform.Find("Backing");
        backing.localPosition = new Vector3(isLeftHandedInt * armLength * passingDistancesInArmLengths[1], eyeHeight, strip.localPosition.z);
        backing.localScale = new Vector3(0.5f * armLength, 0.5f * eyeHeight, 0.01f);

        var launchPlane = GameObject.Find("CatchingEnvironment/DebugObjects/LaunchPlane");

        launchPlane.transform.localPosition = new Vector3(isLeftHandedInt * launchPlanePosition_XYZ[0], launchPlanePosition_XYZ[1], launchPlanePosition_XYZ[2]);
        launchPlane.transform.localScale = new Vector3(launchPlaneWidth, launchPlaneHeight, 0.01f);

        // Vector3 resetControllerPos = new Vector3(isLeftHandedInt* 0.6f * armLength, 0.71f * eyeHeight, 0.43f * armLength);
        // resetController.transform.position = resetControllerPos;

        // resetController.SetActive(true);
        // resetControllerLocation = true;

    }

    public void placeAndLaunchBall()
    {

        if (trialInProgress == true)
        {
            return;
        }

        if (sessionHasBeenCreated == false)
        {
            generateSession();
        }

        if (bodyDimensionsSet == false)
        {
            Debug.Log("Body dimensions must be set before launching ball.");
            return;
        }

        // if (debugMode == false & changeBoxColor.flag)
        // {
        // placeAndLaunchBall();
        // }

        var tr = UXF_Session.NextTrial;
        
        List<float> ballInitialPos_XYZ = tr.settings.GetFloatList("ballInitialPos_XYZ");
        Vector3 ballInitialPos = new Vector3(ballInitialPos_XYZ[0], ballInitialPos_XYZ[1], ballInitialPos_XYZ[2]);

        GameObject Ball = Instantiate(ballPrefab, ballInitialPos, Quaternion.identity);
        UXF_Session.trackedObjects.Add(Ball.GetComponent<BallTracker>());

        StartCoroutine(executeTrial());

        IEnumerator executeTrial()
        {
            trialInProgress = true;

            BallBehavior ballBehavior = Ball.GetComponent<BallBehavior>();
            
            // Debug.Log("Coroutine: placeBall().");
        
            ballBehavior.placeBall();
            yield return new WaitForSeconds(0.75f);

            // Debug.Log("Coroutine: launchBall().");
            UXF_Session.BeginNextTrial();

            ballBehavior.launchBall();
            yield return new WaitForSeconds(2.0f);

            setTrialResults(Ball);

            // Debug.Log("Coroutine: removeBall().");
            // ballBehavior.removeBall();
            
            
            UXF_Session.CurrentTrial.End();
            UXF_Session.trackedObjects.Remove(Ball.GetComponent<BallTracker>());
            Destroy(Ball);

      
            
            //// Uncommenting this would cause balls to continually be launched, with 1 sec intervals between
            // UXF_Session.BeginNextTrialSafe();
            // yield return new WaitForSeconds(1.0f);
            // placeAndLaunchBall();

            trialInProgress = false;

        }
    }

    void setTrialResults(GameObject ball){

        BallBehavior ballBehavior = ball.GetComponent<BallBehavior>();
        var tr = UXF_Session.CurrentTrial;

        UXF_Session.CurrentTrial.result["trialType"] = "interception";
        UXF_Session.CurrentTrial.result["isCaughtQ"] = ballBehavior.hasBeenCaughtQ;

        if( ballBehavior.hasBeenCaughtQ){

            UXF_Session.CurrentTrial.result["contactLocinWorld_x"] = ballBehavior.contactLocinWorld.x.ToString();
            UXF_Session.CurrentTrial.result["contactLocinWorld_y"] = ballBehavior.contactLocinWorld.y.ToString();
            UXF_Session.CurrentTrial.result["contactLocinWorld_z"] = ballBehavior.contactLocinWorld.z.ToString();

            UXF_Session.CurrentTrial.result["contactLocOnPaddle_x"] = ballBehavior.contactLocOnPaddle.x.ToString();
            UXF_Session.CurrentTrial.result["contactLocOnPaddle_y"] = ballBehavior.contactLocOnPaddle.y.ToString();
            UXF_Session.CurrentTrial.result["contactLocOnPaddle_z"] = ballBehavior.contactLocOnPaddle.z.ToString();
            
            UXF_Session.CurrentTrial.result["timeOfContact"] = ballBehavior.timeOfContact;

        }

        UXF_Session.CurrentTrial.result["maxReach"] = UXF_Session.settings.GetFloat("armLength");
        UXF_Session.CurrentTrial.result["eyeHeight"] = UXF_Session.settings.GetFloat("eyeHeight");
        UXF_Session.CurrentTrial.result["isLeftHanded"] = UXF_Session.settings.GetBool("isLeftHanded");
        UXF_Session.CurrentTrial.result["noExpansionLastXSeconds"] = UXF_Session.settings.GetFloat("noExpansionLastXSeconds");
        
        UXF_Session.CurrentTrial.result["expansionGain"] = tr.settings.GetFloat("expansionGain");
        UXF_Session.CurrentTrial.result["repetitionNumber"] = tr.settings.GetInt("repetitionNumber");
        
        UXF_Session.CurrentTrial.result["passingHeightInHeadHeights"] = tr.settings.GetFloat("passingHeightInHeadHeights");
        UXF_Session.CurrentTrial.result["passingDistanceInArmLengths"] = tr.settings.GetFloat("passingDistanceInArmLengths");
        UXF_Session.CurrentTrial.result["secondsToPassage"] = tr.settings.GetFloat("secondsToPassage");


        List<float> ballInitialPos_XYZ = tr.settings.GetFloatList("ballInitialPos_XYZ");
        List<float> ballPassingPos_XYZ = tr.settings.GetFloatList("ballPassingPos_XYZ");
        List<float> ballInitialVel_XYZ = tr.settings.GetFloatList("ballInitialVel_XYZ");

        UXF_Session.CurrentTrial.result["ballInitialPos_x"] = ballInitialPos_XYZ[0];
        UXF_Session.CurrentTrial.result["ballInitialPos_y"] = ballInitialPos_XYZ[1];
        UXF_Session.CurrentTrial.result["ballInitialPos_z"] = ballInitialPos_XYZ[2];

        UXF_Session.CurrentTrial.result["ballFinalPos_x"] = ballPassingPos_XYZ[0];
        UXF_Session.CurrentTrial.result["ballFinalPos_y"] = ballPassingPos_XYZ[1];
        UXF_Session.CurrentTrial.result["ballFinalPos_z"] = ballPassingPos_XYZ[2];

        UXF_Session.CurrentTrial.result["ballInitialVel_x"] = ballInitialVel_XYZ[0];
        UXF_Session.CurrentTrial.result["ballInitialVel_y"] = ballInitialVel_XYZ[1];
        UXF_Session.CurrentTrial.result["ballInitialVel_z"] = ballInitialVel_XYZ[2];

    }
    public void sampleEyeHeightAndArmLength()
    {
        float eyeHeight = Camera.main.transform.position.y;

        Transform handTransform = this.handTransform;

        // Check if the Meta controller has a valid pose
        UnityEngine.XR.InputDevice controllerDevice = isLeftHanded 
            ? InputDevices.GetDeviceAtXRNode(XRNode.LeftHand) 
            : InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        // bool isTracked = false;
        // if (!controllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out isTracked) || !isTracked)
        // {
        //     Debug.LogWarning("Controller pose is not valid or not tracked.");
        //     return; // Exit the method if the controller is not tracked
        // }

        // If the controller is tracked, proceed with sampling
        if (!isLeftHanded)
        {
            handTransform = GameObject.Find("OpenXRRightHand").transform;
        }
        else
        {
            handTransform = GameObject.Find("OpenXRLeftHand").transform;
        }

        Vector3 handInHeadSpace_xyz = Camera.main.transform.InverseTransformPoint(handTransform.position);
        float armLength = handInHeadSpace_xyz.x;

        UXF_Session.settings.SetValue("eyeHeight", eyeHeight);
        UXF_Session.settings.SetValue("armLength", armLength);
        UXF_Session.settings.SetValue("isLeftHanded", isLeftHanded);

        bodyDimensionsSet = true;

        resetSeatedPosition();
        repositionDebugObjects();
    }

    public void resetSeatedPosition()
    {
        
        if ( bodyDimensionsSet == false) 
        {
            Debug.Log("Body dimensions must be set before recentering seated position.");
            return;
        }

        Transform mainCamera = Camera.main.transform;
        Transform cameraParent = mainCamera.parent;

        Vector3 targetPosition = new Vector3(0.0f, 0.0f, 0.0f);
      
        targetPosition.y = UXF_Session.settings.GetFloat("eyeHeight");

        //ROTATION
        // Get current head heading in scene (y-only, to avoid tilting the floor)
        float offsetAngle = mainCamera.rotation.eulerAngles.y;
        // Now rotate CameraRig in opposite direction to compensate
        cameraParent.Rotate(0f, -offsetAngle, 0f);

        //POSITION
        // Calculate postional offset between CameraRig and Camera
        Vector3 offsetPos = mainCamera.position - cameraParent.position;
        // Reposition CameraRig to desired position minus offset
        cameraParent.position = targetPosition - offsetPos;

        Debug.Log("Seat recentered!");

    }

    public void OnSetBodyDimensions(InputAction.CallbackContext context)
    {
        Debug.Log("Set body dimensions action performed.");
        sampleEyeHeightAndArmLength();
    }

    public void onLaunchBall(InputAction.CallbackContext context)
    {
        Debug.Log("Launch ball action performed.");
        placeAndLaunchBall();
    }


    
    // public void Update()
    // {
        
    //     // if( OVRInput.Get(OVRInput.RawButton.Y) || OVRInput.Get(OVRInput.RawButton.B) )
    //     // {
    //     //     sampleEyeHeightAndArmLength();
    //     // }

    //     // if( OVRInput.Get(OVRInput.RawButton.A) || OVRInput.Get(OVRInput.RawButton.X) )
    //     // {
    //     //     placeAndLaunchBall();
    //     // }

    // }

}
