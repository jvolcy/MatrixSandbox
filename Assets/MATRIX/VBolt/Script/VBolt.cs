using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Collider))]
///JVolcy; Spelman College; Dec. 2025
///VBolt.cs

/// <summary>
/// Requires nuts tagged as "VNut".
/// A VNut is any GameObject with a collider or trigger tagged as "VNut"
/// and a scale of 1,1,1.  Note that any and all parent objects of a 
/// VNut must also have a 1,1,1 scale.
/// The GameObject's forward axis must be aligned with the bolt's forward
/// axis within MAX_ALIGN_ANGLE in order to mount the bolt onto the nut.
/// 
/// How to use:
/// The shaftLength and pitchMetersPerRev values are set to match the
/// default bolt geometry provided.  You can leave these untouched
/// unless you want to use a different bolt/screw.
/// 
/// shaftLength is the lenght of the bolt's shank in meters.  This is
/// the length measured from the tip of the bolt up to, but excluding,
/// the head.  If you are using a different bolt model, the root of the
/// local coordinate must be at the tip of the bolt, with the forward
/// direction pointing away from the shaft.
/// 
/// pitchMetersPerRev specifis how far the bolt advances with each
/// revolution.  The units are meters per revolution.
/// 
/// revPerSec specifies how fast the bolt turns.  This is a positive
/// value.
/// 
/// TargetPosition is the normalized target position of the bolt relative
/// to the nut (aka, the target position of the nut on the bolt).  A value
/// of one means that the bolt is fully threaded to the nut.  A value of
/// zero means we want to unmount the bolt from the nut.  Normally, when 
/// the bolt is first mounted to the nut, the TargetPosition is zero (if
/// ResetTargetOnMount is true).  This is the only time, a zero value
/// of the TargetPosition will not cause the bolt to unmount.  Set the
/// TargetPosition to a value from 0 to 1 and the bolt will travel at
/// the rate of revPerSec until it reaches the target value.  Set the
/// target value to 0 to unmount the bolt.
/// 
/// ResetTargetOnMount is a boolean that specifies whether we should
/// reset the target position to zero when a bolt is first mounted
/// to a nut.  If this value is set to false, a mounted bolt may immediately
/// begin to move towards whatever position is specified by TargetPosition. 
/// </summary>
public class VBolt : MonoBehaviour
{
    Transform ParentNut = null;     //the nut we are bound to
    Transform CandidateNut = null;  //the net we are able to bind to.  This may eventually become the ParentNut.
    public float shaftLength = 0.054f;  //the length of the bolt shaft.
    public float pitchMetersPerRev = 0.01f;   //The distance traveled by the bolt with one revolution by a driver.
    public float revPerSec = 1f;   //the speed at which we should turn the bolt
    [Range(0f, 1f)] public float TargetPosition = 0f;   //The normalized target position of the nut on the bolt.
    float threadPosition = 0f;      //the normalized position 0 to 1 of the nut on the bolt.  1 = full length of the bolt shaft.
    /// Note on threadPosition:  An inital threadPosition of
    /// 0 means that the bolt is mounted to the nut, but it hasn't yet
    /// engaged the threads.  In such a state, the bolt remains grabbable,
    /// allowing the user to remove it from the nut.  Any value above 0
    /// means that the bolt has been threaded to the nut.  Any threading,
    /// no matter how small, means that the bolt is no longer grabbable.  It
    /// needs to be untreaded before it can be grabbed.  A value of 1 means
    /// that the bolt is fully threaded into the nut.  threadPosition attempts
    /// to move the bolt the normalized position specified by TargetPosition.
    public bool ResetTargetOnMount = true;  //whether we should reset the TargetPosition to zero when the bolt is first mounted to a nut.

    //Component References
    Rigidbody rb;
    XRGrabInteractable grabInteractable;
    Collider boltCollider;

    bool grabbed = false;   //true when the bolt has been grabbed;  false, otherwise.  Field is serialized for debug only.

    //The XR Grab Interactor changes some of the properties of the game object when the object is grabbed.
    //For example, grabbed objects become kinematic and their colliders become triggers.  When the object
    //is released, the different properties are returned to the value before the object was grabbed.  For
    //us, this is problem.  In some cases, we want to change the properties of the object after it has been
    //grabbed and we don't want thos properties to revert to whatever value the XRGrabInteractor want to
    //restore.  To get around this, we create the shadow registers isTrigger and isKinematic.  These store
    //respectively the corresponding Collider and RigidBody values with the same name that the XRGrabInteractor
    //restore when the object is released.  In our case, in FixedUpdate, we constantly assign the shadow
    //registers to their corresponding parameters so that when an object is relased after a grab, the
    //values are set correctly (not reverted to their pre-grab values).
    bool isTrigger = false;  //whether or not the bolt's Collider should be a trigger when it is released from a grab.  It is a trigger when grabbed.
    bool isKinematic = false;  //whether or not the bolts RigidBody should be kinematic when it is released from a grab.  It is kinematic when grabbed.

    bool bAutoMount = false;    //set to true to force the bolt to mount to a specified nut

    public float BACKOUT_RATE = 0.05f;   //the value by which we decrement the thread variable when we are auto-backing out the bolt.
    public float MAX_ALIGN_ANGLE = 10f;  //the maximum allowed alignment angle between nut and bolt

    /// <summary>
    /// BoltState
    /// UNTHREADED - this is the default state.  The bolt is not engaged with any nut
    /// CAN_MOUNT - in this state, the bolt is grabbed and in contact with a nut
    /// MOUNTED - the bolt has been parented to a nut, but can still be grabbed (thread = 0.0)
    /// THREADED - the bolt is parented to a nut, but can not longer be grabbed (0.0 < thread <= 1.0)
    /// BACKOUT - the bolt has been untreaded to the point where thread <= 0.0.  Here, we 
    /// automatically continue backing it out until it is no longer in contact with the nut.
    /// UNMOUNT - thread value is zero: this is a transition state to get back to the UNTHREADED state.
    /// AUTO_MOUNT - explicitly mount to a specified bolt.
    /// </summary>
    enum BoltState { UNTHREADED, CAN_MOUNT, MOUNTED, THREADED, BACKOUT, UNMOUNT, AUTO_MOUNT };
    BoltState boltState = BoltState.UNMOUNT;


    void OnEnable()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        ParentNut = null;
        CandidateNut = null;

        if (grabInteractable != null)
        {
            //Subscribe to the grab/release events
            grabInteractable.selectEntered.AddListener(OnObjectGrabbed);
            grabInteractable.selectExited.AddListener(OnObjectReleased);
        }
    }

    void OnDisable()
    {
        //Un-subscribe to the grab/release events
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnObjectGrabbed);
            grabInteractable.selectExited.RemoveListener(OnObjectReleased);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        boltCollider = GetComponent<Collider>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (ParentNut != null)
        {
            TargetPosition = Mathf.Clamp(TargetPosition, -1f, 1f);

            if (TargetPosition > threadPosition)
            {
                float deltaRev = revPerSec * Time.deltaTime;
                threadPosition += pitchMetersPerRev * deltaRev / shaftLength;
                threadPosition = Mathf.Clamp(threadPosition, -1f, TargetPosition);
                transform.Rotate(0, 0, -deltaRev * 360f, Space.Self);
            }
            else if (TargetPosition < threadPosition)
            {
                float deltaRev = revPerSec * Time.deltaTime;
                threadPosition -= pitchMetersPerRev * deltaRev / shaftLength;
                threadPosition = Mathf.Clamp(threadPosition, TargetPosition, 1f);
                transform.Rotate(0, 0, deltaRev * 360f, Space.Self);
            }

            transform.localPosition = shaftLength * threadPosition * Vector3.forward;
        }
    }

    void FixedUpdate()
    {
        switch (boltState)
        {
            case BoltState.UNMOUNT:
                /// This is a transition state to get us back to the default
                /// UNTHREADED state from any other state.  In the UNTHREADED
                /// state, the rigidbody is non-kinematic, the collider is not
                /// a trigger and the bolt is grabbable.
                isTrigger = false;
                isKinematic = false;
                boltState = BoltState.UNTHREADED;
                ParentNut = null;
                grabInteractable.enabled = true;
                threadPosition = 0f;
                transform.SetParent(null, true);
                //Note: TargetPosition is set/reset in the CAN_MOUNT state.

                break;

            case BoltState.UNTHREADED:
                //This is the default state.  We sit here until the bolt is grabbed and
                //brought into contact with a nut.
                if (CandidateNut && grabbed) { boltState = BoltState.CAN_MOUNT; }
                if (bAutoMount) { boltState = BoltState.AUTO_MOUNT; }
                break;

            case BoltState.CAN_MOUNT:
                //our bolt is in contact with a nut to which it can be parented.
                if (!CandidateNut)
                {
                    //if we are no longer in range of the nut, return to the UNTHREADED state.
                    boltState = BoltState.UNMOUNT;
                }
                else if (!grabbed)
                {
                    //here, we are in range of a nut and the have released the bolt.
                    //If the bolt is properly aligned with the nut, we mount it to the nut.
                    if (aligned(transform, CandidateNut, MAX_ALIGN_ANGLE))
                    {
                        //Assing the CandidateNut to be the ParentNut.
                        //Once ParentNut is set to a non-null value, the bolt will follow the nut in Update().
                        ParentNut = CandidateNut;
                        isKinematic = true;     //make the bolt kinematic so that it does not move the nut

                        //The user might have set the TargetPosition while we were in the UNTHREADED.
                        //We set it here instead of in the UNMOUNT state to ensure it is properly set when we mount.
                        if (ResetTargetOnMount) TargetPosition = 0f;

                        transform.SetParent(ParentNut, true);
                        transform.localPosition = Vector3.zero;
                        transform.localRotation = Quaternion.identity;
                        boltState = BoltState.MOUNTED;  //move to the MOUNTED state
                    }
                    else
                    {
                        //if we are not aligned, return to the default state
                        boltState = BoltState.UNMOUNT;
                    }
                }
                else    //bolt is still grabbed
                {
                    //here, we are aligned, but the user has not yet released the bolt.
                    //We stay in the CAN_MOUNT state.
                }
                break;

            case BoltState.MOUNTED:
                //Here, the bolt has been mounted on the nut.  This state is very similar
                //to the THREADED state, with the execption that here, we can still grab
                //the bolt to unmount it.  Once we begin to thread it (thread > 0), we
                //will transition to the TREHADED state, where the bolt is no longer grabbable.

                //Note: In this state, the bolt follows the parent nut in Update()
                if (grabbed)
                {
                    //if the user grabs the bolt, unmount it and return to the default state.
                    boltState = BoltState.UNMOUNT;
                }
                else
                {
                    //if the bolt starts threading onto the nut (thread > 0), move to the THREADED
                    //state where the bolt will no longer be grabbable.
                    if (threadPosition != 0f)
                    {
                        //bolt no longer grabbable
                        //transition to THREADED state
                        grabInteractable.enabled = false;
                        isTrigger = true;  //allow the bolt to pass through the nut and other objects
                        boltState = BoltState.THREADED;
                    }
                }
                break;

            case BoltState.THREADED:
                //Note: The bolt follows the parent nut in Update()

                //If the value of thread is less than 0, the bolt has been
                //untreaded from the nut.  We transition to the BACKOUT state
                //where we continue moving the bolt until it is no longer in
                //contact with the nut (CandidateNut == NULL).  This prevents
                //large forces from being applied to the bolt when its
                //collider reverts from beign a trigger to being a non-trigger.
                if (threadPosition <= 0f)
                {
                    boltState = BoltState.BACKOUT;
                }
                break;

            case BoltState.BACKOUT:
                //Note: The bolt follows the parent in Update()

                //here, we backout the bolt until it separates from
                //the nut (CandidateNut == null).
                if (CandidateNut != null)
                {
                    threadPosition -= BACKOUT_RATE;
                }
                else
                {
                    boltState = BoltState.UNMOUNT;
                }
                break;

            case BoltState.AUTO_MOUNT:
                bAutoMount = false; //lower the auto mount flag

                //the parent nut is already set in the Mount() function.
                //the target position is already set in the Mount() function.
                isKinematic = true; //make the bolt kinematic
                threadPosition = TargetPosition;    //jump to the target position
                transform.SetParent(ParentNut, true);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity; 
                boltState = BoltState.MOUNTED;
                break;
        }

        //When the bolt is grabbed, its collider is a trigger and its RigidBody is set to kinematic.
        //When it is released, its collider state is set by isTrigger and the RigidBody's isKinematic value is set to isKinematic.
        boltCollider.isTrigger = grabbed | isTrigger;
        rb.isKinematic = grabbed | isKinematic;
    }

    /// <summary>
    /// Function to directly mount us to a specified vnut.  A vnut
    /// is any GameObject with a collider or trigger tagged as "VNut".
    /// The GameObject's forward axis must be aligned with the bolt's forward
    /// axis within MAX_ALIGN_ANGLE in order to mount the bolt onto the nut.
    /// The bolt must be in the default (UNTREADED) state before invoking
    /// this function.
    /// </summary>
    /// <param name="vnut"></param>
    /// <param name="position"></param>
    public void Mount(Transform vnut, float position)
    {
        ParentNut = vnut;
        TargetPosition = position;
        bAutoMount = true;
    }

    /// <summary>
    /// returns true if the angle between the forward vectors of the
    /// provided transforms is less than maxAngleDeg.  Returns false
    /// otherwise.
    /// </summary>
    /// <param name="t1">Transform 1</param>
    /// <param name="t2">Transform 2</param>
    /// <param name="maxAngleDeg">Threshold angle.  If the angle
    /// between the forward vectors of the 2 transforms is less than
    /// this value, the function returns true.  Otherwise, it
    /// returns false.</param>
    /// <returns></returns>
    bool aligned(Transform t1, Transform t2, float maxAngleDeg)
    {
        return Vector3.Dot(t1.forward, t2.forward) > Mathf.Cos(Mathf.Deg2Rad * maxAngleDeg);
    }

    /// <summary>
    /// When we come into contact with a "VNut" object, we have a candidate
    /// parent nut.
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("VNut")) { CandidateNut = other.transform; }
    }

    /// <summary>
    /// When we lose contact with a "VNut" object, we have no candidate
    /// parent nut.
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("VNut")) { CandidateNut = null; }
    }

    private void OnObjectGrabbed(SelectEnterEventArgs args)
    {
        grabbed = true;
    }

    private void OnObjectReleased(SelectExitEventArgs args)
    {
        grabbed = false;
    }

}
