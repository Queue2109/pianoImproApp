using System.Collections;
using Melanchall.DryWetMidi.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CountDownTimer : MonoBehaviour
{
    [Header("Timer UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject timerUI;
    [SerializeField] private GameObject mainContent;
    [SerializeField] private Image timerCircle;   // Image -> Type = Filled
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clip;

    public IEnumerator StartTimer(TempoMap tempoMap, double speed)
    {
        // ----- how many beats? ------------------------------------------------
        var timeSig = tempoMap.GetTimeSignatureAtTime(new MetricTimeSpan(0));
        int beatsPerBar = timeSig.Numerator;            // 4 in 4/4, 3 in 3/4 …
        int totalBeats = beatsPerBar * 2;           // e.g. 8

        double beatSec = TimeConverter
            .ConvertTo<MetricTimeSpan>(MusicalTimeSpan.Quarter, tempoMap)
            .TotalMicroseconds / 1_000_000.0;

        Debug.Log($"beat sec {beatSec} total beats {totalBeats} speed {speed}" );

        mainContent.SetActive(false);
        timerUI.SetActive(true);
        beatSec /= speed;
        

        for (int beat = totalBeats; beat > 0; beat--)
        {
            timerText.text = beat.ToString();           // 8‑7‑…‑1
            timerCircle.fillAmount = beat / (float)totalBeats;  // shrink ring
            audioSource.Play(0);                   // click

            yield return new WaitForSecondsRealtime((float)beatSec);
        }

        CountdownFinished();    
    }

    private void CountdownFinished()
    {
        mainContent.SetActive(true);
        timerUI.SetActive(false);
        timerCircle.fillAmount = 0f;
        Debug.Log("Countdown finished!");
    }
}

