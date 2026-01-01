using UnityEngine;

public class DoorHinge : MonoBehaviour
{
    public float HingeJointMinLocked = -1f;
    public float HingeJointMinUnlocked = 180f;

    enum DoorHingeState { LOCKED, UNLOCKED }
    DoorHingeState doorHingeState = DoorHingeState.LOCKED;

    public HandlePivot handlePivot;

    HingeJoint m_hingeJoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_hingeJoint = GetComponent<HingeJoint>();
        SetJointLimit(HingeJointMinLocked);     //door is locked by default
    }

    // Update is called once per frame
    void Update()
    {
        switch (doorHingeState)
        {
            case DoorHingeState.LOCKED:
                break;
            case DoorHingeState.UNLOCKED:
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
}
