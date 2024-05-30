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
    // midi note number of lowest key in your midi device
    // 21: A0
    public delegate void MidiNoteEvent(int noteNumber);
    public event MidiNoteEvent OnNoteOn;
    public event MidiNoteEvent OnNoteOff;
    private List<string> noteOrder = new List<string> { "A", "A-Sharp", "B", "C", "C-Sharp", "D", "D-Sharp", "E", "F", "F-Sharp", "G", "G-Sharp" };
    private GameObject pianoKeyboard;
    // this is used for visualizing the notes
    //[SerializeField] GameObject barManager;

    void Start()
    {
        pianoKeyboard = GameObject.FindGameObjectWithTag("Piano");
        Color colorBlue = new Color(0.545f, 0.769f, 0.910f, 0.33f);

        InputSystem.onDeviceChange += (device, change) =>
        {
            if (change != InputDeviceChange.Added) return;
            var midiDevice = device as Minis.MidiDevice;
            if (midiDevice == null) return;

            midiDevice.onWillNoteOn += (note, velocity) =>
            {
                Debug.Log($"Note On: {note.noteNumber}");
                OnNoteOn?.Invoke(note.noteNumber);
                string noteName = this.NoteNameConverter(note.noteNumber);

                Renderer renderer = GameObject.Find(noteName)?.GetComponent<Renderer>();
                if (renderer != null)
                {
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        renderer.materials[i].SetColor("_Color", new Color(0.545f, 0.769f, 0.910f, 0.33f));
                    }
                    Debug.Log("Material colors changed to green.");
                }
            };


            midiDevice.onWillNoteOff += (note) =>
            {
                Debug.Log($"Note Off: {note.noteNumber}");
                OnNoteOff?.Invoke(note.noteNumber);
                string noteName = this.NoteNameConverter(note.noteNumber);

                Renderer renderer = GameObject.Find(noteName)?.GetComponent<Renderer>();
                if (renderer != null)
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
                    Debug.Log("Material colors changed to green.");
                }
            };
        };
    }

    void Listen()
    {
        InputSystem.onDeviceChange += (device, change) =>
        {
            if (change != InputDeviceChange.Added) return;
            var midiDevice = device as Minis.MidiDevice;
            if (midiDevice == null) return;

            midiDevice.onWillNoteOn += (note, velocity) => {
                Debug.Log(string.Format(
                    "Note On #{0} ({1}) vel:{2:0.00} ch:{3} dev:'{4}'",
                    note.noteNumber,
                    note.noteNumber.GetType(),
                    note.shortDisplayName,
                    velocity,
                    velocity.GetType(),
                    (note.device as Minis.MidiDevice)?.channel,
                    note.device.description.product
                ));

                //barManager.GetComponent<BarScript>().onNoteOn(note.noteNumber - keyOffset, velocity);
            };

            midiDevice.onWillNoteOff += (note) => {
                Debug.Log(string.Format(
                    "Note Off #{0} ({1}) ch:{2} dev:'{3}'",
                    note.noteNumber,
                    note.shortDisplayName,
                    (note.device as Minis.MidiDevice)?.channel,
                    note.device.description.product
                ));

                //barManager.GetComponent<BarScript>().onNoteOff(note.noteNumber - keyOffset);
            };
        };
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

        // Check if there are any MIDI devices connected
        if (midiDeviceCount > 0)
        {
            Debug.Log("Connected MIDI devices found: " + midiDeviceCount);
            return true; // There are one or more MIDI devices connected
        }
        else
        {
            Debug.Log("No MIDI devices detected.");
            return false; // No MIDI devices are connected
        }
    }
    public string NoteNameConverter(int midiNote)
    {
        // midi number 21 = A0, there are 
        // my child objects of keyboard have either a name A0 or A-Sharp0
        int octaveNumber = (int) Mathf.Floor((midiNote - 24) / 12);

        string noteName = this.noteOrder[(midiNote - 21) % 12].ToString();
        noteName += octaveNumber;
        Debug.Log("Note name" + noteName);
        return noteName;
    }

}