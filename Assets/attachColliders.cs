using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachColliders : MonoBehaviour
{
    void Start()
    {
        foreach (Transform child in transform)
        {
            Debug.Log("Processing: " + child.gameObject.name);
            
            if (child.gameObject.GetComponent<BoxCollider>() == null)
            {
                BoxCollider newCollider = child.gameObject.AddComponent<BoxCollider>();
                newCollider.size = new Vector3(0.001f, 0.01f, 0.001f);
                Debug.Log("Added collider to: " + child.gameObject.name + " with size " + newCollider.size);
            }
            else
            {
                Debug.Log("Collider already exists on: " + child.gameObject.name);
            }

            if (child.gameObject.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = child.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                
                rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ;
                   // rb.constraints = RigidbodyConstraints.FreezePosition;
                // rb.isKinematic = true;
                Debug.Log("Added rigid body " + rb);
            }
        }
    }

     
}
