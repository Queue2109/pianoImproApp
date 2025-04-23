using UnityEngine;

public class AlignAfterMount : MonoBehaviour
{

    Vector3 position = new Vector3(-0.5f, 0.5f, 0.5f);
    private void OnEnable()
    {
        OVRManager.HMDMounted += OnHMDMounted;
    }

    private void OnDisable()
    {
        OVRManager.HMDMounted -= OnHMDMounted;
    }

    private void OnHMDMounted()
    {
        AlignMyStaticProps();
    }

    public void AlignMyStaticProps()
    {
        OVRManager.display.RecenterPose();
        Vector3 thisRotation = transform.eulerAngles;
        transform.position = position;
    }
}
