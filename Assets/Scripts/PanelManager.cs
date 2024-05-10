using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class PanelManager : MonoBehaviour
{
    public List<GameObject> panels; // List to hold all your panels

    public int currentPanelIndex = 0; // To keep track of the current panel
    public MidiScript midiScript;
    public PianoSetup pianoSetup; 
    private bool isListeningForNote = false; // To ensure we don't add multiple listeners
    public TMP_Text lowestNoteText;
    public TMP_Text highestNoteText;



    void Start()
    {
        SetPanelActive();
    }

    private void Update()
    {
        switch(currentPanelIndex)
        {
           case 0: 
            if (midiScript.ListenForDevice())
            {
                this.NextPanel();
            }
                break;
            case 1:
                if (!isListeningForNote) // Check if we are not already listening
                {
                    midiScript.OnNoteOn += HandleNoteOn;
                    isListeningForNote = true; // Set flag to true to avoid multiple subscriptions
                }
                break;
            case 3: 
                if (!isListeningForNote)
                {
                    midiScript.OnNoteOn += HandleNoteOn;
                    isListeningForNote = true; // Set flag to true to avoid multiple subscriptions

                }
                break;
            case 5:
                pianoSetup.Setup();
                break;

        }
    }


    void SetPanelActive()
    {
        Debug.Log("Panel index" + currentPanelIndex);
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(i == currentPanelIndex);
        }
    }

    public void NextPanel()
    {
        if (currentPanelIndex < panels.Count - 1)
        {
            currentPanelIndex++;
            SetPanelActive();
        }
    }
    public void PreviousPanel()
    {
        if (currentPanelIndex > 0)
        {
            currentPanelIndex--;
            SetPanelActive();
        }
    }

    public void SetPanel(int index)
    {
        currentPanelIndex = index;
        SetPanelActive();
    }

    public void GetLowestNote()
    {
        if (midiScript != null)
        {
            midiScript.OnNoteOn += HandleNoteOn;
        }

    }

    public void GetHighestNote()
    {
        if (midiScript != null)
        {
            midiScript.OnNoteOn += HandleNoteOn;
            Debug.Log("Neki");
        }

    }
    private void HandleNoteOn(int noteNumber)
    {
        if(noteNumber < 40)
        {
            pianoSetup.lowestNote = midiScript.NoteNameConverter(noteNumber); // Assuming NoteNameConverter is a valid method
            lowestNoteText.text = pianoSetup.lowestNote.ToString();
        } else
        {
            pianoSetup.highestNote = midiScript.NoteNameConverter(noteNumber);
            highestNoteText.text = pianoSetup.highestNote.ToString();
        }
        midiScript.OnNoteOn -= HandleNoteOn; // Unsubscribe from the event to prevent repeated calls
        isListeningForNote = false; // Reset the flag
        NextPanel(); // Move to the next panel
    }

    void OnDestroy()
    {
        if (isListeningForNote)
        {
            midiScript.OnNoteOn -= HandleNoteOn; // Ensure we unsubscribe when the object is destroyed
        }
    }
}
