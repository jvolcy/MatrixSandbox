using UnityEngine;

public class DoorHinge : MonoBehaviour
{
    public float HingeJointMinBolted = 0f;      //door doesn't budge when bolted
    public float HingeJointMinLocked = -1f;     //door can move 1 degree when unbolted but latched
    public float HingeJointMinUnlocked = -180f; //door can freely move 180 degrees when unbolted and unlatched

    enum DoorHingeState { INIT, BOLTED, LATCHED, UNLATCHED }
    DoorHingeState doorHingeState = DoorHingeState.INIT;
    //int initCounter = 100;

    public HandlePivot handlePivot;
    AudioSource audioSource;
    public float angle;

    HingeJoint m_hingeJoint;

    [Tooltip("Set to true to enable verbose console messages.")]
    public bool debug = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_hingeJoint = GetComponent<HingeJoint>();
        SetJointLimit(HingeJointMinBolted);     //door is bolted by default

        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        angle = transform.localEulerAngles.y;
        if (angle > 180) { angle -= 360; }      //force angle to -180 to 180

        switch (doorHingeState)
        {
            case DoorHingeState.INIT:
                //wait for handlePivot to finish initializing
                if (handlePivot.isBolted())
                {
                    doorHingeState = DoorHingeState.BOLTED;
                    Message("INIT->BOLTED");
                }
                break;
            case DoorHingeState.BOLTED:
                if (!(handlePivot.isBolted()))
                {
                    Message("BOLTED->LOCKED");
                    SetJointLimit(HingeJointMinLocked);
                    doorHingeState = DoorHingeState.LATCHED;
                }
                break;
            case DoorHingeState.LATCHED:
                if (handlePivot.handleState == HandlePivot.HandleState.UNLATCHED)
                {
                    SetJointLimit(HingeJointMinUnlocked);
                    Message("LOCKED->UNLOCKED");
                    doorHingeState = DoorHingeState.UNLATCHED;
                }
                break;
            case DoorHingeState.UNLATCHED:
                if (handlePivot.handleState == HandlePivot.HandleState.LATCHED && angle > -1f)
                {
                    //play audio sound
                    audioSource.Play();
                    SetJointLimit(HingeJointMinLocked);
                    Message("UNLOCKED->LOCKED");
                    doorHingeState = DoorHingeState.LATCHED;
                }
                break;
        }
    }

    void SetJointLimit(float limit)
    {
        JointLimits jointLimits = new JointLimits();
        jointLimits.min = limit;
        jointLimits.max = 0;
        m_hingeJoint.limits = jointLimits;
    }

    void Message(string arg)
    {
        if (!debug) return;

        System.Diagnostics.StackTrace stackTrace = new();
        Debug.Log(name + ":" + stackTrace.GetFrame(1).GetMethod().Name + "(): " + arg);
    }
}
