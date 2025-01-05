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

            float distanceFromCamera = 1.5f;

            float heightOffset = 0.5f;

            if (obj.name == "PianoSetup")
            {
                heightOffset = -1f;
                distanceFromCamera = 0.5f;
            }
            // Position the object in front of the camera
            Vector3 forwardPosition = centerEyeAnchor.position + centerEyeAnchor.forward * distanceFromCamera;
            forwardPosition.y += heightOffset; // Adjust height

            obj.transform.position = forwardPosition;

            Vector3 cameraForward = centerEyeAnchor.forward;
            cameraForward.y = 0; // Zero out the Y component to constrain rotation to the Y-axis
            obj.transform.rotation = Quaternion.LookRotation(cameraForward, Vector3.up);

        }
    }
}
