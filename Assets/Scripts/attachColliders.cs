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
            AddMaterials(child.gameObject);
          
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
