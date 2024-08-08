using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelManagerSongList : MonoBehaviour
{
    public List<GameObject> panels;
    public int currentPanel;
    public MidiFileManager midiFileManager;
    public string filePath;
    void Start()
    {
        OpenPanel(currentPanel);
    }

    // Update is called once per frame
    void Update()
    {
        SetThingsUp();
        
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
                OpenPanel(2);
                break;
        
        }

    }

    public void OpenPanel(int panelNumber)
    {
        for(int i = 0; i < panels.Count; i++)
        {
            if(i == panelNumber)
            {
                panels[i].SetActive(true);
            } else
            {
                panels[i].SetActive(false);
            }
        }
    }
}
