using UnityEngine;

public class AlignAfterMount : MonoBehaviour
{

    public Vector3 position;
    public Camera ovrCamera;
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

    private void AlignMyStaticProps()
    {
        transform.position = ovrCamera.transform.TransformPoint(position);
        Vector3 lookTarget = ovrCamera.transform.position;
        lookTarget.y = transform.position.y;
        //  ^ sets the "target" Y to match the object's Y,
        //    so we don't tilt up or down.

        // 3) LookAt that target
        transform.LookAt(lookTarget, Vector3.up);

    }
}
