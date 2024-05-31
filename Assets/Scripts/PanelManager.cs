using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Animations;
using Oculus.Interaction;
using UnityEngine.EventSystems;

public class PanelManager : MonoBehaviour
{
    private List<GameObject> panels; // List to hold all your panels
    public int currentPanelIndex = 0; // To keep track of the current panel
    public MidiScript midiScript;
    public PianoSetup pianoSetup;
    private bool isListeningForNote = false; // To ensure we don't add multiple listeners
    [SerializeField] private TMP_Text noteTextLow;
    [SerializeField] private TMP_Text noteTextHigh;
    bool wroteLowerNote = false;
    bool setupCompleted = false;
    private Dictionary<int, System.Action> panelActions;

    void Start()
    {
        InitializePanels();
        InitializePanelActions();
        SetPanelActive();
    }

    void Update()
    {
        if (panelActions.ContainsKey(currentPanelIndex))
        {
            panelActions[currentPanelIndex]();
        }
    }

    private void InitializePanels()
    {
        GameObject canvas = GameObject.Find("Canvas");
        panels = new List<GameObject>();
        if (canvas != null)
        {
            foreach (Transform child in canvas.transform)
            {
                if (child.gameObject.CompareTag("Panel"))
                {
                    panels.Add(child.gameObject);
                }
            }
        }
    }

    private void InitializePanelActions()
    {
        panelActions = new Dictionary<int, System.Action>
        {
            { 0, HandleDeviceCheck },
            { 1, ListenForNote },
            { 3, ListenForNote },
            { 5, SetupPiano },
            { 6, DisablePianoInteraction }
        };
    }

    void HandleDeviceCheck()
    {
        if (midiScript.ListenForDevice())
        {
            NextPanel();
        }
    }

    void ListenForNote()
    {
        if (!isListeningForNote)
        {
            isListeningForNote = true;
            midiScript.OnNoteOn += HandleNoteOn;
        }
    }

    void SetupPiano()
    {
       
            //pianoSetup.lowestNote = this.noteTextLow.ToString();
            //pianoSetup.highestNote = this.noteTextHigh.ToString();
            pianoSetup.Setup();
        if (!setupCompleted)
        {
            pianoSetup.AssignMaterials();
            setupCompleted = true; // Ensure setup only happens once

        }
    }

    void DisablePianoInteraction()
    {
        // Uncomment and adapt based on your specific needs
        pianoSetup.DoneSetup();
    }

    public void Done()
    {
        GameObject.Find("UI Cylinder").SetActive(false);

    }

    void SetPanelActive()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(i == currentPanelIndex);
        }
        Debug.Log("Panel index: " + currentPanelIndex);
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
            if(currentPanelIndex == 1)
            {
                wroteLowerNote = false;
            }
            if(currentPanelIndex == 3)
            {
                wroteLowerNote = true;
            }
        }
    }

    public void SetPanel(int index)
    {
        if (index < panels.Count)
        {
            currentPanelIndex = index;
            SetPanelActive();
        }
    }

    private void HandleNoteOn(int noteNumber)
    {
        if (!wroteLowerNote)
        {
            noteTextLow.text = midiScript.NoteNameConverter(noteNumber);
            wroteLowerNote = true;
        }
        else
        {
            noteTextHigh.text = midiScript.NoteNameConverter(noteNumber);
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
