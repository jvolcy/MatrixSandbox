using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Collider))]
//[RequireComponent(typeof(ConfigurableJoint))]

public class Bolt : MonoBehaviour
{
    /// <summary>
    /// Requires nuts tagged as NUT.
    /// </summary>

    Transform ParentNut = null;
    Transform CandidateNut = null;
    [SerializeField] float shaftLength = 0.1f;
    [SerializeField] float pitchMetersPerRev = 0.01f;
    [SerializeField] [Range(0f, 1f)] float thread = 0f;      //the position 0 to 1 of the nut on the bolt.  1 = full length of the bolt shaft.
    Rigidbody rb;
    XRGrabInteractable grabInteractable;
    Collider boltCollider;
    [SerializeField] bool grabbed = false;   //true when the bolt has been grabbed;  false, otherwise.
    bool isTrigger = false;  //whether or not the bolt's collider should be a trigger when it is not grabbed.  We automatically make it a trigger when grabbed.
    bool isKinematic = false;

    const float BACKOUT_RATE = 0.05f;   //the value by which we decrement the thread variable when we are auto-backing out the bolt.
    const float MAX_ALIGN_ANGLE = 10f;  //the maximum allowed alignment angle between nut and bolt

    /// <summary>
    /// BoltState
    /// UNTHREADED - this is the default state.  The bolt is not engaged with any nut
    /// CAN_MOUNT - in this state, the bolt is grabbed and in contact with a nut
    /// MOUNTED - the bolt has been parented to a nut, but can still be grabbed (thread = 0.0)
    /// THREADED - the bolt is parented to a nut, but can not longer be grabbed (0.0 < thread <= 1.0)
    /// BACKOUT - the bolt has been untreaded to the point where thread <= 0.0.  Here, we 
    /// automatically continue backing it out until it is no longer in contact with the nut.
    /// UNMOUNT - thread value is zero: this is a transition state to get back to the UNTHREADED state.
    /// </summary>
    enum BoltState { UNTHREADED, CAN_MOUNT, MOUNTED, THREADED, BACKOUT, UNMOUNT };
    [SerializeField] BoltState boltState = BoltState.UNMOUNT;


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
            //follow the parent
            transform.position = ParentNut.position + shaftLength * thread * ParentNut.forward;
            transform.rotation = ParentNut.rotation;
        }
    }

    void FixedUpdate()
    {
        switch (boltState)
        {
            case BoltState.UNMOUNT:
                Debug.Log("Start UNMOUNT");
                /// This is a transition state to get us back to the default
                /// UNTHREADED state from any other state.  In the UNTHREADED
                /// state, the rigidbody is non-kinematic, the collider is not
                /// a trigger and the bolt is grabbable.
                isTrigger = false;
                isKinematic = false;
                boltState = BoltState.UNTHREADED;
                ParentNut = null;
                grabInteractable.enabled = true;
                thread = 0f;
                Debug.Log("End UNMOUNT");
                break;

            case BoltState.UNTHREADED:
                if (CandidateNut && grabbed) { boltState = BoltState.CAN_MOUNT; }
                break;

            case BoltState.CAN_MOUNT:
                if (!CandidateNut)
                {
                    //if we are no longer in range of the nut, return to the UNTHREADED state.
                    boltState = BoltState.UNMOUNT;
                }
                else if (!grabbed)
                {
                    //here, we are in range of a nut and the have released the bolt --> need to mount it to the nut if we are aligned

                    if (aligned(transform, CandidateNut, MAX_ALIGN_ANGLE))
                    {
                        //Once ParentNut is set to a non-null value, the bolt will follow the nut in Update().
                        ParentNut = CandidateNut;
                        isKinematic = true;
                        boltState = BoltState.MOUNTED;
                    }
                    else
                    {
                        //isTrigger = false;
                        boltState = BoltState.UNMOUNT;
                    }
                }
                else    //bolt is still grabbed
                {
                    //else stay in the CAN_MOUNT state.
                }
                break;

            case BoltState.MOUNTED:
                //Note: The bolt follows the parent nut in Update()
                if (grabbed)
                {
                    boltState = BoltState.UNMOUNT;
                }
                else
                {
                    if (thread > 0f)
                    {
                        //bolt no longer grabbable
                        //transition to THREADED state
                        grabInteractable.enabled = false;
                        isTrigger = true;  //allow the bolt to pass through the nut and other objects
                        boltState = BoltState.THREADED;
                    }
                }
                    // if we come into contact with the driver, we move to the THREADED state.
                break;

            case BoltState.THREADED:
                //Note: The bolt follows the parent nut in Update()
                if (thread <= 0f)
                {
                    boltState = BoltState.BACKOUT;
                    Debug.Log("[1] thread = " + thread);
                }
                break;

            case BoltState.BACKOUT:
                //Note: The bolt follows the parent in Update()
                if (CandidateNut != null)
                {
                    thread -= BACKOUT_RATE;
                }
                else
                {
                    Debug.Log("[2] thread = " + thread);
                    boltState = BoltState.UNMOUNT;
                }
                    break;
        }

        //When the bolt is grabbed, its collider is a trigger.
        //When it is released, its collider state is set by isTrigger.
        boltCollider.isTrigger = grabbed | isTrigger;
        rb.isKinematic = grabbed | isKinematic;
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
    /// When we come into contact with a "Nut" object, we have a candidate
    /// parent nut.
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Nut")) { CandidateNut = other.transform; }
    }

    /// <summary>
    /// When we lose contact with a "Nut" object, we have no candidate
    /// parent nut.
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Nut")) { CandidateNut = null; }
    }

    private void OnObjectGrabbed(SelectEnterEventArgs args)
    {
        grabbed = true;
        //boltCollider.isTrigger = true;  //when we grab the bolt, it becomes a trigger
    }

    private void OnObjectReleased(SelectExitEventArgs args)
    {
        grabbed = false;
        //boltCollider.isTrigger = isTrigger; //when we release the bolt, it reverts to the state specified by isTrigger.
    }

}
