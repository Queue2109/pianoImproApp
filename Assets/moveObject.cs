using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveObject : MonoBehaviour
{
    
    Vector3 Vec;  
    // Start is called before the first frame update
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
        Debug.Log("Collided");
        Debug.Log("Key pressed: " + collision.gameObject.name);
     }
}
