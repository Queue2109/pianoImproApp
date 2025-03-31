using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

public class ModifyButtonColor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameObject[] panels = GameObject.FindGameObjectsWithTag("ButtonPanel");
        foreach (GameObject panel in panels)
        {
            InteractableColorVisual roundedBox = panel.GetComponent<InteractableColorVisual>();
            if (roundedBox != null)
            {
                roundedBox.enabled = false;
                roundedBox.enabled = true;
            }
        }
    }
}
