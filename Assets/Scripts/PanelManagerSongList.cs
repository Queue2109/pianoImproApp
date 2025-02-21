using System.Collections;
using System.Collections.Generic;
using Meta.XR.BuildingBlocks;
using Oculus.Interaction;
using UnityEngine;

public class PanelManagerSongList : MonoBehaviour
{
    public List<GameObject> panels;
    public int currentPanel = 0;
    public MidiFileManager midiFileManager;
    public MidiFileNoteReader midiFileNoteReader;
    public GameObject pianoKeyboard;
    void Start()
    {
        //OpenPanel(currentPanel);
        panels[0].SetActive(false);
        midiFileManager.LogMidiFilesAsync();
    }

    private void SetThingsUp()
    {
        switch (currentPanel)
        {
            case 0:
                midiFileNoteReader.Setup();
                UpdateAndShowPLaySongPanel();
                break;

        }

    }

    public void OpenPanel(int panelNumber)
    {
        if(panelNumber == 0 && midiFileNoteReader.fileName == "")
        {
            return;
        }
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
    
    public void MovePanel()
    {
        panels[currentPanel].transform.position = pianoKeyboard.transform.position + new Vector3(0.4f, 0.2f, 0);
    }

    public void UpdateAndShowPLaySongPanel()
    {
        midiFileNoteReader.StartFullSongPlayback();
        pianoKeyboard.SetActive(true);
    }
}
