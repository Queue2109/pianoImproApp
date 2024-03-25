using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveObject : MonoBehaviour
{
    Vector3 Vec;   
    Renderer ren;
    void Start()
    {
    }

    // Update is called once per frame
    void FixedUpdate()
    {
      
        Vec = transform.localPosition;  
        // Vec.z -= Input.GetAxis("Jump") * Time.deltaTime * 2;  
        Vec.x += Input.GetAxis("Horizontal") * Time.deltaTime * 10;  
        Vec.y += Input.GetAxis("Vertical") * Time.deltaTime * 10;  
        transform.localPosition = Vec;  
  
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject key = collision.gameObject;
        ren = key.GetComponent<Renderer>();
        if(key.name.Contains("Sharp") == true) {
            ren.material.color = new Color(186, 39, 39);
        } else {
            ren.material.color = new Color(186, 39, 39);
        }
        Debug.Log("Key pressed: " + collision.gameObject.name);
    }

    private void OnCollisionExit(Collision collision)
    {
        GameObject key = collision.gameObject;
        ren = key.GetComponent<Renderer>();
        if(key.name.Contains("Sharp") == true) {
            ren.material.color = new Color(0, 0, 0);
        } else {
            ren.material.color = new Color(255, 255, 255);
        }
    }
}
