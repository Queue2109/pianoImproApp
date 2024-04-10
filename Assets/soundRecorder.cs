using UnityEngine;
using System.Linq;

public class PianoKeyFrequencyDetector : MonoBehaviour
{
    private bool isRecording = false;
    private float[] audioData;
    private AudioClip clip;
    private const int SAMPLE_RATE = 44100;
    private const int FREQUENCY_WINDOW = 1024; // Size of the FFT window
    public GameObject soundAnimation;
    public GameObject button;
    public void StartRecording()
    {
        audioData = new float[SAMPLE_RATE * 5]; // 5 seconds of audio at 44100 samples per second
        isRecording = true;
        Microphone.Start(null, false, 5, SAMPLE_RATE);
        Debug.Log("Recording started...");
        Invoke("StopRecording", 5); // Stop recording after 5 seconds
        
        button.gameObject.SetActive(false);
        soundAnimation.gameObject.SetActive(true);
        // Start the show soundAnimation
    }
 
    void StopRecording()
    {
        if (!isRecording) return;

        Microphone.End(null);   
        isRecording = false;
        Debug.Log("Recording stopped.");

        clip = Microphone.Start(null, false, 5, SAMPLE_RATE);
        clip.GetData(audioData, 0);

        AnalyzeSound();
        button.gameObject.SetActive(true);
    }

    void AnalyzeSound()
    {
        // This is a very basic frequency analysis using FFT
        // For more accurate results, consider using a third-party library

        float maxMagnitude = 0;
        int maxIndex = 0;

        // Perform FFT on the data
        for (int i = 0; i < audioData.Length; i += FREQUENCY_WINDOW)
        {
            float magnitude = audioData.Skip(i).Take(FREQUENCY_WINDOW).Select((val, index) => val * Mathf.Sin(Mathf.PI * index / FREQUENCY_WINDOW)).Sum();
            if (magnitude > maxMagnitude)
            {
                maxMagnitude = magnitude;
                maxIndex = i;
            }
        }

        float frequency = maxIndex * SAMPLE_RATE / audioData.Length;
        Debug.Log($"Detected frequency: {frequency} Hz");
    }
}
