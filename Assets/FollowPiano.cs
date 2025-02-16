using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelFollowPiano : MonoBehaviour
{
    public Transform target; // assign the piano's transform in the Inspector
    public Vector3 offset;   // desired positional offset relative to the piano

    private void Start()
    {
        UpdatePosition();
    }
    public void UpdatePosition()
    {
        if (target != null)
        {
            // Update panel position relative to the target (piano)
            transform.position = target.position + offset;

            // Optional: match rotation (or only certain axes)
            float targetY = target.rotation.eulerAngles.y;
            float targetZ = target.rotation.eulerAngles.z;
            transform.rotation = Quaternion.Euler(-45, 0, - targetZ);
        }
    }
}

