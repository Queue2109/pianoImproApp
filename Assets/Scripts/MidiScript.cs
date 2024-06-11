using System.Collections;
using System.Collections.Generic;
using Minis;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;

public class MidiScript : MonoBehaviour
{

    public delegate void MidiNoteEvent(int noteNumber);
    public event MidiNoteEvent OnNoteOn;
    public event MidiNoteEvent OnNoteOff;
    private readonly List<string> noteOrder = new() { "A", "A-Sharp", "B", "C", "C-Sharp", "D", "D-Sharp", "E", "F", "F-Sharp", "G", "G-Sharp" };
    private GameObject pianoKeyboard;
    private bool _midiDeviceConnected;
    private float _lastCheckTime;
    private const float CheckInterval = 1.0f; // Check every second
    private List<int> pressedNotes = new List<int>();
    private HttpHandler httpHandler;
    // this is used for visualizing the notes
    //[SerializeField] GameObject barManager;

    void Start()
    {
        pianoKeyboard = GameObject.FindGameObjectWithTag("Piano");
        Color colorBlue = new(0.545f, 0.769f, 0.910f, 0.33f);
        httpHandler = GameObject.Find("HttpHandler").GetComponent<HttpHandler>();

        InputSystem.onDeviceChange += (device, change) =>
        {
            if (change != InputDeviceChange.Added) return;
            var midiDevice = device as Minis.MidiDevice;
            if (midiDevice == null) return;

            midiDevice.onWillNoteOn += (note, velocity) =>
            {
                //Debug.Log($"Note On: {note.noteNumber}");
                OnNoteOn?.Invoke(note.noteNumber);
                AddNoteToPressedList(note.noteNumber);
                string noteName = this.NoteNameConverter(note.noteNumber);
                GameObject noteKey = GameObject.Find(noteName);
                if(noteKey == null) {
                    return;
                }
                if (noteKey.TryGetComponent<Renderer>(out var renderer))
                {
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        renderer.materials[i].SetColor("_Color", new Color(0.545f, 0.769f, 0.910f, 0.33f));
                    }
                   // Debug.Log("Material colors changed to green.");
                }
            };


            midiDevice.onWillNoteOff += (note) =>
            {
              //  Debug.Log($"Note Off: {note.noteNumber}");
                OnNoteOff?.Invoke(note.noteNumber);
                RemoveNoteFromPressedList(note.noteNumber);
                string noteName = this.NoteNameConverter(note.noteNumber);

                GameObject noteKey = GameObject.Find(noteName);
                if(noteKey == null) {
                    return;
                }
                if (noteKey.TryGetComponent<Renderer>(out var renderer))
                {
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        if(noteName.Contains("Sharp"))
                        {
                            renderer.materials[i].SetColor("_Color", new Color(0, 0, 0, 0.8f));
                        } else
                        {
                            renderer.materials[i].SetColor("_Color", new Color(1, 1, 1, 0.2509f));
                        }
                    }
                    //Debug.Log("Material colors changed to green.");
                }
            };
        };
    }

    void AddNoteToPressedList(int noteNumber)
    {
        if (!pressedNotes.Contains(noteNumber))
        {
            pressedNotes.Add(noteNumber);
            //Debug.Log("Note added to pressed list: " + noteNumber);
            httpHandler.getChordName(pressedNotes);
        }
    }

    void RemoveNoteFromPressedList(int noteNumber)
    {
        if (pressedNotes.Contains(noteNumber))
        {
            pressedNotes.Remove(noteNumber);
           // Debug.Log("Note removed from pressed list: " + noteNumber);
            httpHandler.getChordName(pressedNotes);
        }
    }

    public bool ListenForDevice()
    {
        // Get all available MIDI devices
        var devices = InputSystem.devices;

        // Filter and count MIDI devices only
        int midiDeviceCount = 0;
        foreach (var device in devices)
        {
            if (device is Minis.MidiDevice)
                midiDeviceCount++;
        }

        bool midiDeviceCurrentlyConnected = midiDeviceCount > 0;

        // Check if the state has changed or enough time has passed to log again
        if (Time.time - _lastCheckTime > CheckInterval || _midiDeviceConnected != midiDeviceCurrentlyConnected)
        {
            _lastCheckTime = Time.time;
            _midiDeviceConnected = midiDeviceCurrentlyConnected;

            if (midiDeviceCurrentlyConnected)
            {
                Debug.Log("Connected MIDI devices found: " + midiDeviceCount);
            }
            else
            {
                Debug.Log("No MIDI devices detected.");
            }
        }

        return midiDeviceCurrentlyConnected;
    }
    public string NoteNameConverter(int midiNote)
    {
        // midi number 21 = A0, there are 
        // my child objects of keyboard have either a name A0 or A-Sharp0
        int octaveNumber = (int) Mathf.Floor((midiNote - 24) / 12);

        string noteName = this.noteOrder[(midiNote - 21) % 12].ToString();
        noteName += octaveNumber;
       // Debug.Log("Note name" + noteName);
        return noteName;
    }
    
}


// pofiksaj piano positioning (da od dalec zagrabis)
// da dela uvod
// da se shrani za naprej
// post call response
// accompany 
// piano roll
// nalozi midi files in da pol ko cez igras da bo kul
// 