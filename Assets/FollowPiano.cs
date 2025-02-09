using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelFollowPiano : MonoBehaviour
{
    public Transform pianoTransform;   // drag 'customPianoKeys' here in Inspector
    private Vector3 offset;
    private Quaternion rotationOffset;

    void Start()
    {
        // Record the initial difference between panel and piano
        offset = transform.position - pianoTransform.position;

        // (Optional) If you also want the same relative rotation:
        rotationOffset = Quaternion.Inverse(pianoTransform.rotation) * transform.rotation;
    }

    void LateUpdate()
    {
        // Keep the same offset position
        transform.position = pianoTransform.position + offset;

        // (Optional) Keep the same relative rotation
        transform.rotation = pianoTransform.rotation * rotationOffset;
    }
}

