using Unity.VisualScripting;
using UnityEngine;

public class MoveObjectsInFrontOfCamera : MonoBehaviour
{
    [Tooltip("Assign the objects to be moved in front of the camera.")]
    public GameObject[] objectsToMove;

    [Tooltip("The OVRCameraRig in your scene.")]
    public Transform centerEyeAnchor;

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

            // Get the Y rotation from the camera's forward direction
            float targetYRotation = Quaternion.LookRotation(centerEyeAnchor.forward).eulerAngles.y;

            // Preserve X and Z, but update Y
            Vector3 newRotation = new Vector3(panelEulerAngles.x, targetYRotation, panelEulerAngles.z);

            // Apply the updated rotation
            obj.transform.eulerAngles = newRotation;

            // Set the position and rotation of the UI panel
            obj.transform.position = newPanelPosition;

        }
    }
}
