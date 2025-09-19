using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(AudioSource))]

/// <summary>
/// The GameObject this component is attached to must have a <Renderer>
/// component.  Further, the material associated with that <Renderer> must
/// have emissions enabled.  Also, the emission's 'Global Illumination'
/// must be set to 'None'.";
/// </summary>
public class MagicColorUpdate : MonoBehaviour
{
    [SerializeField] string comment = "The GameObject this component is attached to " +
        "must have a <Renderer> component.  Further, the material associated " +
        "with that <Renderer> must have emissions enabled.  Also, the " +
        "emission's 'Global Illumination' must be set to 'None'.";

    public Color targetColor;
    public GameObject ParticleSystemPrefab;
    public AudioClip magicSound;
    Material mat;
    Animator animator;
    ParticleSystem particles;
    public Transform particlesPosition;

    void Start()
    {
        //audioSource = GetComponent<AudioSource>();
        mat = GetComponentInChildren<Renderer>().material;
        animator = GetComponent<Animator>();

        if (ParticleSystemPrefab)
        {
            var obj = Instantiate(ParticleSystemPrefab, transform);
            particles = obj.GetComponent<ParticleSystem>();
            if (particlesPosition)
            {
                obj.transform.position = particlesPosition.position;
            }
        }
    }

     /*
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        { MagicUpdate(); }
    }
     */

    public void MagicUpdate(Color clr)
    {
        targetColor = clr;
        MagicUpdate();
    }

    public void MagicUpdate()
    {
        animator.SetTrigger("updateColor");
    }

    public void MagicUpdateAudioCallback()
    {
        AudioSource.PlayClipAtPoint(magicSound, transform.position, 0.25f);
        //audioSource.PlayOneShot(magicSound);
    }

    public void MagicUpdateParticleCallback()
    {
        if (particles)
        {
            particles.Play();
        }
    }

    public void MagicUpdateColorCallback()
    {
        mat.color = targetColor;
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
