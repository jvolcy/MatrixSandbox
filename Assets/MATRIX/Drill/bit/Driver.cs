using UnityEngine;

public class Driver : MonoBehaviour
{

    public enum DriverState { NO_BOLT, ALIGNED_BOLT, UNALIGNED_BOLT }
    [Tooltip("Read-only state variable.  This tell us the status of the driver relative to any bolt.")]
    public DriverState state = DriverState.NO_BOLT;

    [Tooltip("The maximum allowed agle between the bolt and driver axes.")]
    public float MAX_ALIGN_ANGLE = 10f;  //the maximum allowed alignment angle between driver and bolt

    [Tooltip("Set to true to enable verbose console messages.")]
    public bool debug = false;

    bool bHasBolt = false;
    [Tooltip("Read-only: the transform of the bolt when the state is either ALIGNED_BOLT or UNALIGNED_BOLT.")]
    public VBolt bolt = null;

    //The driver is spun by the Drill object using an animation.  The animation's default speed is 1 RPS.
    [Tooltip("The rate at which the 'Drill' animator is spinning the driver.  This information is used to sync up engaged VBolt objects.")]
    public float AnimationRPS = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case DriverState.NO_BOLT:
                if (bHasBolt)
                {
                    if (aligned(transform, bolt.transform, MAX_ALIGN_ANGLE))
                    {
                        state = DriverState.ALIGNED_BOLT;
                    }
                    else
                    {
                        state = DriverState.UNALIGNED_BOLT;
                    }
                }
                break;
            case DriverState.UNALIGNED_BOLT:
                bolt.revPerSec = 0;
                if (aligned(transform, bolt.transform, MAX_ALIGN_ANGLE))
                {
                    state = DriverState.ALIGNED_BOLT;
                }
                break;
            case DriverState.ALIGNED_BOLT:
                bolt.revPerSec = AnimationRPS;
                //if the drive is set for CCW, our target bolt position is zero (fully untreaded).
                //if the drive is set for CW rotation, our target bolt position is one (fully threaded).
                bolt.TargetPosition = bolt.revPerSec > 0 ? 0 : 1;       //positive RPS = CCW
                if (!aligned(transform, bolt.transform, MAX_ALIGN_ANGLE))
                {
                    state = DriverState.UNALIGNED_BOLT;
                }
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("VBolt"))
        {
            bHasBolt = true;
            bolt = other.GetComponent<VBolt>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("VBolt"))
        {
            bolt.revPerSec = 0;
            bHasBolt = false;
            bolt = null;
            state = DriverState.NO_BOLT;
        }
    }


    void Message(string arg)
    {
        if (!debug) return;

        System.Diagnostics.StackTrace stackTrace = new();
        Debug.Log(name + ":" + stackTrace.GetFrame(1).GetMethod().Name + "(): " + arg);
    }

}
