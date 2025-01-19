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
    public PlayControlPanelManager panelManager;
    void Start()
    {
        //OpenPanel(currentPanel);
        midiFileManager.LogMidiFilesAsync();
    }

    private void SetThingsUp()
    {
        switch (currentPanel)
        {
            case 0:
                panelManager.MovePanelPosition();
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

    public void UpdateAndShowPLaySongPanel()
    {
        midiFileNoteReader.StartPlaybackFromBeginning();
        pianoKeyboard.SetActive(true);
    }
}
