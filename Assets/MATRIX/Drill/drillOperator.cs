using UnityEditor.Animations;
using UnityEngine;

public class drillOperator : MonoBehaviour
{
    [SerializeField] Animator buttonAnimator;
    
    [SerializeField] Animator bitAnimation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        float drillSpeed = buttonAnimator.GetLayerWeight(1);
        bitAnimation.speed = drillSpeed;
    }
}
