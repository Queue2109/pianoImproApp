using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BunnuLogic : MonoBehaviour
{

    public GameObject pianoSettings;
    public NewAnchorManager anchorManager;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CheckIfPianoSettingsIsActive()
    {
        if (pianoSettings.activeSelf == true)
        {
            anchorManager.OnSaveButtonPressed();
            pianoSettings.SetActive(false);
        }
    }
}
