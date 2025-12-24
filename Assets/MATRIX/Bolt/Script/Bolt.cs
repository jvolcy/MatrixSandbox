using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(ConfigurableJoint))]

public class Bolt : MonoBehaviour
{
    /// <summary>
    /// Requires nuts tagged as NUT.
    /// </summary>

    Transform ParentNut = null;
    Transform CandidateNut = null;
    [SerializeField] float shaftLength = 0.1f;
    [SerializeField] float pitchMetersPerRev = 0.01f;
    float thread = 0f;      //the position 0 to 1 of the nut on the bolt.  1 = full length of the bolt shaft.
    Rigidbody rb;
    XRGrabInteractable grabInteractable;
    Collider boltCollider;
    ConfigurableJoint joint;
    bool grabbed = false;   //true when the bolt has been grabbed;  false, otherwise.


    /// <summary>
    /// BoltState
    /// UNTHREADED - this is the default state.  The bolt is not engaged with any nut
    /// CAN_MOUNT - in this state, the bolt is grabbed and in contact with a nut
    /// MOUNTED - the bolt has been parented to a nut, but can still be grabbed (thread = 0.0)
    /// THREADED - the bolt is parented to a nut, but can not longer be grabbed (0.0 < thread <= 1.0)
    /// UNMOUNT - thread value is zero: this is a transition state to get back to the UNTHREADED state.
    /// </summary>
    enum BoltState { UNTHREADED, CAN_MOUNT, MOUNTED, THREADED, UNMOUNT };
    [SerializeField] BoltState boltState = BoltState.UNMOUNT;


    void OnEnable()
    {
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
        grabInteractable = GetComponent<XRGrabInteractable>();
        boltCollider = GetComponent<Collider>();
        joint = GetComponent<ConfigurableJoint>();
    }

    // Update is called once per frame
    void Update()
    {
        switch (boltState)
        {
            case BoltState.UNMOUNT:
                /// This is a transition state to get us back to the default
                /// UNTHREADED state from any other state.  In the UNTHREADED
                /// state, the rigidbody is non-kinematic, the collider is not
                /// a trigger and the bolt is grabbable.
                rb.isKinematic = false;
                boltCollider.isTrigger = false;
                grabInteractable.enabled = true;
                boltState = BoltState.UNTHREADED;
                ParentNut = null;
                joint.connectedBody = null;
                joint.anchor = Vector3.zero;
                thread = 0f;
                break;

            case BoltState.UNTHREADED:
                if (CandidateNut && grabbed) { boltState = BoltState.CAN_MOUNT; }
                break;

            case BoltState.CAN_MOUNT:
                if (!CandidateNut)
                {
                    //if we are no longer in range of the nut, return to the UNTHREADED state.
                    boltState = BoltState.UNTHREADED;
                }
                else if (!grabbed)
                {
                    //here, we are in range of a nut and the have released the bolt --> need to mount it to the nut if we are aligned

                    if (aligned(transform, CandidateNut, 10f))
                    {
                        rb.isKinematic = true;  //turn off gravity and collision forces
                        boltCollider.isTrigger = true;  //allow the bolt to pass through the nut and other objects
                        grabInteractable.enabled = true;    //we can still grab and remove remove the bolt from the MOUNTED state
                        ParentNut = CandidateNut;
                        transform.position = ParentNut.position;
                        joint.connectedBody = ParentNut.GetComponent<Rigidbody>();
                        joint.anchor = Vector3.zero;
                        thread = 0f;
                        boltState = BoltState.MOUNTED;
                    }
                    else
                    {
                        boltState = BoltState.UNTHREADED;
                    }
                }
                break;

            case BoltState.MOUNTED:
                if (grabbed)
                {
                    ParentNut = null;
                    boltState = BoltState.UNMOUNT;
                }
                // if we come into contact with the driver, we move to the THREADED state.
                break;

            case BoltState.THREADED:
                break;
        }
        
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
    }

    private void OnObjectReleased(SelectExitEventArgs args)
    {
        grabbed = false;
    }

}
