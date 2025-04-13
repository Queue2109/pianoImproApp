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
        if (!ovrCamera)
        {
            Debug.LogWarning("No camera assigned, cannot align!");
            return;
        }

        // 1) Flatten the camera's forward and right vectors on the horizontal plane.
        //    (i.e., ignore pitch/roll so we stay at the same Y-level.)
        Vector3 forwardNoY = Vector3.ProjectOnPlane(ovrCamera.transform.forward, Vector3.up).normalized;
        Vector3 rightNoY = Vector3.ProjectOnPlane(ovrCamera.transform.right, Vector3.up).normalized;

        // 2) Compute new position:
        //    offset.z units in front of camera, offset.x units to the side, offset.y up/down
        Vector3 desiredPos = ovrCamera.transform.position
            + forwardNoY * position.z
            + rightNoY * position.x
            + new Vector3(0, position.y, 0);

        transform.position = desiredPos;

        // 3) Face the user horizontally:
        //    We'll rotate to look at the camera’s XZ position but keep our own Y.
        Vector3 lookTarget = ovrCamera.transform.position;
        lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget, Vector3.up);
    }
}
