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
    private GameObject pianoKeyboard;
    private FallingBlocksVisualizer fallingBlocksVisualizer;
    void Start()
    {
        OpenPanel(currentPanel);
    }

    private void SetThingsUp()
    {
        switch (currentPanel) {
            case 0:
                midiFileManager.LogMidiFiles();
                PersistentGameObject persistentGameObject = FindObjectOfType<PersistentGameObject>();
                if (persistentGameObject)
                {
                    pianoKeyboard = persistentGameObject.gameObject;
                    fallingBlocksVisualizer = GameObject.Find("FallingBlocksVisualizer").GetComponent<FallingBlocksVisualizer>();
                    fallingBlocksVisualizer.pianoKeyboard = pianoKeyboard;
                }
                else
                {
                    Debug.Log("No PersistentGameObject found in the scene. in the 0");
                }
                pianoKeyboard.GetComponent<Grabbable>().enabled = false;
                pianoKeyboard.SetActive(false);
                break;
            case 1:
                MidiInstrumentChecker.CheckInstruments(filePath);
                pianoKeyboard.SetActive(false);
                break; 
            case 2:
                midiFileNoteReader.PlayMidiFunction();
                pianoKeyboard.SetActive(true);
                break;
        
        }

    }

    public void OpenPanel(int panelNumber)
    {
        currentPanel = panelNumber;
        for(int i = 0; i < panels.Count; i++)
        {
            if(i == panelNumber)
            {
                panels[i].SetActive(true);
                SetThingsUp();
            } else
            {
                panels[i].SetActive(false);
            }
        }
    }
}
