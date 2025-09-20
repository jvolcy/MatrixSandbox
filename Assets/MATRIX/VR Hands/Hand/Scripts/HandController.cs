using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

//[RequireComponent(typeof(ActionBasedController))]
[RequireComponent(typeof(Animator))]

public class HandController : MonoBehaviour
{

    //[SerializeField] XRInputValueReader<Vector2> m_XRI_StickInput;
    [SerializeField] XRInputValueReader<float> m_XRI_ActivateInput;
    [SerializeField] XRInputValueReader<float> m_XRI_SelectInput;

    Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        animator.SetBool("activate", m_XRI_ActivateInput.ReadValue() > 0.5f);
        animator.SetBool("select", m_XRI_SelectInput.ReadValue() > 0.5f);
    }

    /// <summary>
    /// Helper function that prepends source file name and line number to
    /// messages that target the Unity console.  Replace Debug.Log() calls
    /// with calls to debug() to use this feature.
    /// </summary>
    /// <param name="msg">The msg to send to the console.</param>
    void debug(string msg)
    {
        var stacktrace = new System.Diagnostics.StackTrace(true);
        string currentFile = System.IO.Path.GetFileName(stacktrace.GetFrame(1).GetFileName());
        int currentLine = stacktrace.GetFrame(1).GetFileLineNumber();  //frame 1 = caller
        Debug.Log(currentFile + "[" + currentLine + "]: " + msg);
    }
}
