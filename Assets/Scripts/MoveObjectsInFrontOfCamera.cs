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

    }

    public void MoveObjects()
    {

        foreach (GameObject obj in objectsToMove)
        {
            if (obj == null) continue;

            // Get the position in front of the camera
            Vector3 cameraForward = centerEyeAnchor.transform.forward;
            Vector3 newPanelPosition = centerEyeAnchor.transform.position + cameraForward * 0.5f;

            // Get the current rotation of the panel
            Vector3 panelEulerAngles = obj.transform.eulerAngles;

            // Get the Y rotation from the camera's forward directionx
            float targetYRotation = Quaternion.LookRotation(centerEyeAnchor.forward).eulerAngles.y - 1f;

            // Preserve X and Z, but update Y
            Vector3 newRotation = new Vector3(panelEulerAngles.x, targetYRotation, panelEulerAngles.z);

            // Apply the updated rotation
            obj.transform.eulerAngles = newRotation;

            // Set the position and rotation of the UI panel
            obj.transform.position = newPanelPosition;

        }
    }
}
