using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class Drill : MonoBehaviour
{
    [SerializeField] Animator buttonAnimator;
    [SerializeField] Animator bitAnimator;
    public Driver driver;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    float drillSpeed = 0f;

    //A reference to the controller's (left or right) Near-Far Interactor
    //valid if the drill has been grabbed; null when the drill is released
    NearFarInteractor interactor = null;

    AudioSource audioSource;
    void Start()
    {
        interactor = null;
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        float pitch = 0f;

        if (interactor)
        {
            //read the trigger (Activate) value.  Use it to set the drill speed.
            drillSpeed = interactor.activateInput.ReadValue();

            if (drillSpeed > 0f) { pitch = 0.5f + drillSpeed / 2; }
        }
        else
        {
            drillSpeed = 0f;
        }

        audioSource.pitch = pitch;
        driver.AnimationRPS = drillSpeed;

        //float drillSpeed = buttonAnimator.GetLayerWeight(1);
        buttonAnimator.SetLayerWeight(1, drillSpeed);
        bitAnimator.speed = drillSpeed;
    }

    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        //Get a reference to the controller's Near-Far Interactor component.
        //From here, we will be able to read the activeInput and activeSelect values as necessary.
        interactor = args.interactorObject.transform.GetComponent<NearFarInteractor>();

        //Debug.Log("Selected by interactor " + interactor.name);
    }

    public void OnSelectExited(SelectExitEventArgs args)
    {
        //once we drop the drill, set the interactor to null.
        interactor = null;
    }

}
