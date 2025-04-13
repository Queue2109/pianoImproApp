using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Minis;
using System.Linq;

public class MidiScript : MonoBehaviour
{
    // Event delegates if you still want them:
    public delegate void MidiNoteEvent(int noteNumber);
    public event MidiNoteEvent OnNoteOn;
    public event MidiNoteEvent OnNoteOff;

    [Header("References")]
    public MidiFileNoteReader midiFileNoteReader;
    public GameObject midiListenerUIObject;
    public TextMeshProUGUI textMeshProUGUI;

    // We only care about ONE device:
    private MidiDevice currentMidiDevice = null;

    // We'll store these so we can unsubscribe cleanly:
    private System.Action<MidiNoteControl, float> noteOnHandler;
    private System.Action<MidiNoteControl> noteOffHandler;

    void OnEnable()
    {
        // Subscribe to device change events
        InputSystem.onDeviceChange += OnDeviceChange;

        // If a device is already present at startup, let's attach to it
        var midiDevice = InputSystem.devices.FirstOrDefault(d => d is MidiDevice) as MidiDevice;
        if (midiDevice != null)
        {
            SubscribeToDevice(midiDevice);
        }

        UpdateUI();
    }

    void OnDisable()
    {
        // Unsubscribe from the system event
        InputSystem.onDeviceChange -= OnDeviceChange;

        // Unsubscribe from our device if it still exists
        UnsubscribeCurrentDevice();
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is not Minis.MidiDevice midiDevice)
            return; // We only care about MIDI devices

        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
                // If we don't have a device yet, use this one
                if (currentMidiDevice == null)
                {
                    SubscribeToDevice(midiDevice);
                }
                break;

            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
                // If this is the device we're using, unsubscribe
                if (midiDevice == currentMidiDevice)
                {
                    UnsubscribeCurrentDevice();
                }
                break;
        }

        UpdateUI();
    }

    private void SubscribeToDevice(MidiDevice device)
    {
        if (currentMidiDevice != null)
        {
            // Already have a device; optionally unsubscribe from it
            // if you truly only want exactly one at a time.
            UnsubscribeCurrentDevice();
        }

        currentMidiDevice = device;
        Debug.Log($"MIDI device connected: {device.description.product}");

        // Define the handlers
        noteOnHandler = (note, velocity) =>
        {
            Debug.Log($"Note On: {note.noteNumber}, Vel: {velocity}");
            OnNoteOn?.Invoke(note.noteNumber);
            midiFileNoteReader?.CheckUserNote(note.noteNumber);
        };
        noteOffHandler = (note) =>
        {
            OnNoteOff?.Invoke(note.noteNumber);
        };

        // Subscribe
        currentMidiDevice.onWillNoteOn += noteOnHandler;
        currentMidiDevice.onWillNoteOff += noteOffHandler;
    }

    private void UnsubscribeCurrentDevice()
    {
        if (currentMidiDevice == null)
            return;

        Debug.Log($"MIDI device disconnected: {currentMidiDevice.description.product}");

        // Unsubscribe
        currentMidiDevice.onWillNoteOn -= noteOnHandler;
        currentMidiDevice.onWillNoteOff -= noteOffHandler;

        // Clear references
        currentMidiDevice = null;
        noteOnHandler = null;
        noteOffHandler = null;
    }

    private void UpdateUI()
    {
        bool deviceConnected = (currentMidiDevice != null);
        midiListenerUIObject.SetActive(!deviceConnected);

        if (deviceConnected)
            textMeshProUGUI.text = $"MIDI device connected: {currentMidiDevice.description.product}";
        else
            textMeshProUGUI.text = "No MIDI device connected";
    }
}
