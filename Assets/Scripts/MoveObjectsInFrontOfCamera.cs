using Unity.VisualScripting;
using UnityEngine;

public class MoveObjectsInFrontOfCamera : MonoBehaviour
{
    [Tooltip("Assign the objects to be moved in front of the camera.")]
    public GameObject[] objectsToMove;

    [Tooltip("The OVRCameraRig in your scene.")]
    public Transform centerEyeAnchor;

    [Header("Settings")]
    public float distanceFromCamera = 2f;


    void Start()
    {

        MoveObjects();
    }

    public void MoveObjects()
    {
    
        if (centerEyeAnchor == null)
        {
            Debug.Log("CenterEyeAnchor not found in OVRCameraRig. Ensure the structure is correct.");
            return;
        }

        foreach (GameObject obj in objectsToMove)
        {
            if (obj == null) continue;

            // Get the position in front of the camera
            Vector3 cameraForward = centerEyeAnchor.transform.forward;
            Vector3 newPanelPosition = centerEyeAnchor.transform.position + cameraForward * 0.5f;

            // Get the current rotation of the panel
            Vector3 panelEulerAngles = obj.transform.eulerAngles;

            // Get the Y rotation from the camera's forward directionx
            float targetYRotation = Quaternion.LookRotation(centerEyeAnchor.forward).eulerAngles.y;

            // Preserve X and Z, but update Y
            Vector3 newRotation = new Vector3(panelEulerAngles.x, targetYRotation, panelEulerAngles.z);

            // Apply the updated rotation
            obj.transform.eulerAngles = newRotation;

            // Set the position and rotation of the UI panel
            obj.transform.position = newPanelPosition;

        }
    }
    private void LateUpdate()
    {
        if (centerEyeAnchor == null) return;

        transform.position = centerEyeAnchor.position + centerEyeAnchor.forward * distanceFromCamera;

        transform.LookAt(centerEyeAnchor);


        Vector3 euler = transform.eulerAngles;
        euler.x = 0f;
        //if (lockYRotation) euler.y = 0f;
        //if (lockZRotation) euler.z = 0f;
        transform.eulerAngles = euler;
    }
}
