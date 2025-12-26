using UnityEngine;

public class TestNutAndBolt : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public Transform vnut;
    public VBolt vbolt;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            vbolt.Mount(vnut, 0.5f);
        }
    }
}
