using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachColliders : MonoBehaviour
{

    public Material[] materials;

    void Start()
    {
        foreach (Transform child in transform)
        {
            Debug.Log("Processing: " + child.gameObject.name);
            AddMaterials(child.gameObject);
            
            if (child.gameObject.GetComponent<BoxCollider>() == null)
            {
                BoxCollider newCollider = child.gameObject.AddComponent<BoxCollider>();
                newCollider.size = new Vector3(0.001f, 0.01f, 0.001f);
            }

            if (child.gameObject.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = child.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                
                rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ;
                Debug.Log("Added rigid body " + rb);
            }
        }
    }

    void AddMaterials(GameObject obj) {
        
         if(obj.name.Contains("Sharp") == true) {
            obj.GetComponent<Renderer>().material = materials[1];
        } else {   
            obj.GetComponent<Renderer>().material = materials[0];
            obj.GetComponent<Renderer>().material.color = new Color(255, 255, 255);
           }
    }

}
