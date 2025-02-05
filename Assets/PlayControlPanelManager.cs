using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayControlPanelManager : MonoBehaviour
{

    public GameObject pianoKeyboard;

    void Start()
    {
        MovePanelPosition();
    }

    private void Awake()
    {
        MovePanelPosition();
    }

    public void MovePanelPosition()
    {
        // Preserve the original X-axis rotation of the gameObject
        Vector3 originalRotation = gameObject.transform.eulerAngles;

        // Get the reference rotation from pianoKeyboard
        Vector3 referenceRotation = pianoKeyboard.transform.eulerAngles;

        // Create a new rotation that matches the reference but keeps the original X-axis
        Vector3 newRotation = new Vector3(originalRotation.x, referenceRotation.y - 135f, referenceRotation.z);

        // Apply the new rotation
        gameObject.transform.eulerAngles = newRotation;

        // Position the gameObject relative to the pianoKeyboard with an offset
        gameObject.transform.position = pianoKeyboard.transform.position + new Vector3(1.17f, 0.42f, 0f); 
    }

}
