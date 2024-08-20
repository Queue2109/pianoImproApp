using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelManagerSongList : MonoBehaviour
{
    public List<GameObject> panels;
    public int currentPanel;
    public MidiFileManager midiFileManager;
    public MidiFileNoteReader midiFileNoteReader;
    public string filePath;
    void Start()
    {
        OpenPanel(currentPanel);
    }

    private void SetThingsUp()
    {
        switch (currentPanel) {
            case 0:
                midiFileManager.LogMidiFiles();
                break;
            case 1:
                MidiInstrumentChecker.CheckInstruments(filePath);
                break; 
            case 2:
                midiFileNoteReader.PlayMidiFunction();
                Debug.Log("In the set things up function playmidi");
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
                Debug.Log("Panel " + i + " is active.");
            } else
            {
                panels[i].SetActive(false);
            }
        }
    }
}
