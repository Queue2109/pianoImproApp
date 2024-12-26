using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

public class PanelManagerSongList : MonoBehaviour
{
    public List<GameObject> panels;
    public int currentPanel = 0;
    public MidiFileManager midiFileManager;
    public MidiFileNoteReader midiFileNoteReader;
    public string filePath;
    public GameObject pianoKeyboard;
    //public Transform uiPanel;
    //public Transform vrCamera;
    public float distanceFromKeyboard = 0.5f; // Distance from the keyboard to place the panel
    public float distanceFromCamera = 0.3f;
    void Start()
    {
        OpenPanel(currentPanel);
    }

    private void SetThingsUp()
    {
        switch (currentPanel)
        {
            case 0:
                midiFileManager.LogMidiFiles();
                midiFileNoteReader.Setup();
                GameObject.Find("UI Cylinder Song List").SetActive(true);
                GameObject.Find("HeroScreen").SetActive(true);
                break;
            case 1:
                MidiInstrumentChecker.CheckInstruments(filePath);
                break;
            case 2:
                UpdateAndShowPLaySongPanel();
                GameObject.Find("HeroScreen").SetActive(false);
                GameObject.Find("UI Cylinder Song List").SetActive(false);
                panels[2].transform.position = pianoKeyboard.transform.position + new Vector3(0, 0, -0.1f);
                // Get the current rotation of the panel
                Quaternion panelRotation = panels[2].transform.rotation;

                // Get the x rotation from the pianoKeyboard
                float pianoKeyboardRotationY = pianoKeyboard.transform.rotation.eulerAngles.y;

                // Create a new rotation while keeping the panel's original y and z rotations
                Quaternion newRotation = Quaternion.Euler(panelRotation.eulerAngles.x, pianoKeyboardRotationY - 180, panelRotation.eulerAngles.z);

                // Apply the new rotation to the panel
                panels[2].transform.rotation = newRotation;
                break;

        }

    }

    public void OpenPanel(int panelNumber)
    {
        currentPanel = panelNumber;
        for (int i = 0; i < panels.Count; i++)
        {
            if (i == panelNumber)
            {
                panels[i].SetActive(true);
                SetThingsUp();
            }
            else
            {
                panels[i].SetActive(false);
            }
        }
    }

    public void UpdateAndShowPLaySongPanel()
    {
        midiFileNoteReader.StartPlaybackFromBeginning();
        pianoKeyboard.SetActive(true);
        pianoKeyboard.GetComponent<Grabbable>().enabled = false;

    }
}
