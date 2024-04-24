using UnityEngine;

public class SoundRecorder : MonoBehaviour
{
    AudioClip audioClip;

    // Define the standard pitch for A4 (440 Hz)
    private const float StandardPitchA4 = 440f;

    // Define the number of semitones per octave
    private const int SemitonesPerOctave = 12;

    // Define the reference pitch class for A
    private const int ReferencePitchClassA = 9; // A0
    private GameObject frame1;
    private GameObject frame2;
    void Start() {
        StartRecording();
    }

    public void StartRecording()
    {
        frame1 = GameObject.Find("Frame1");
        frame2 = GameObject.Find("Frame2Listening");
        frame1.SetActive(false);
        frame2.SetActive(true);
        // Get the available microphone devices
        string[] microphoneDevices = Microphone.devices;

        // Choose a specific microphone device (for example, the first one in the list)
        string selectedDevice = microphoneDevices.Length > 0 ? microphoneDevices[0] : null;

        // Start recording audio from the selected microphone device
        audioClip = Microphone.Start(selectedDevice, true, 5, AudioSettings.outputSampleRate);
        Debug.Log("Recording started...");

        // Delayed stop recording after 5 seconds
        Invoke("StopRecording", 5f);
    }

    void StopRecording()
    {
        // Stop recording and get the recorded audio data
        Microphone.End(null);
        Debug.Log("Recording stopped.");

        // Analyze the recorded audio data
        AnalyzeSound();
    }

    void AnalyzeSound()
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = audioClip;

        // Analyze the frequency spectrum
        float[] spectrumData = new float[1024];
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        // Find the dominant frequency
        float maxAmplitude = 0;
        int maxIndex = 0;
        for (int i = 0; i < spectrumData.Length; i++)
        {
            if (spectrumData[i] > maxAmplitude)
            {
                maxAmplitude = spectrumData[i];
                maxIndex = i;
            }
        }

        // Calculate the frequency corresponding to the dominant index
        float frequency = maxIndex * AudioSettings.outputSampleRate / spectrumData.Length;

        // Convert frequency to musical pitch (you can use the method described in the previous response)
        Debug.Log($"Dominant frequency: {frequency} Hz");

        // Optionally, convert frequency to musical pitch
        // ...

        // Clean up
        Destroy(audioSource);
    }
}
