using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using System.Text;
using System;
using TMPro;
using System.Threading;
using System.Globalization;

public class HttpHandler : MonoBehaviour
{
    // Start is called before the first frame update

    string url = "http://localhost:5000/analyze";
    TMP_Text text;
    Dictionary<string, string> chordDictionary;
    void Start()
    {
        chordDictionary = new Dictionary<string, string>
         {
             { "diminished triad", "dim" },
             { "augmented triad", "aug" },
             { "major triad", "" }, // has no notations other than the base note
             { "minor triad", "m" },
             { "major seventh chord", "maj\u2077" },
             { "minor seventh chord", "m\u2077" },
             { "dominant seventh chord", "\u2077" },
             { "diminished seventh chord", "dim\u2077" }, // dim7
             { "half-diminished seventh chord", "m\u2077(\u1D47\u2075)" }, // m7(b5)
             { "augmented seventh chord", "aug\u2077" },
             { "minor-augmented tetrachord", "mM\u2077" },
            // { "French augmented sixth chord", "" }, //???
             { "quartal trichord", "sus2" },
             { "incomplete major-seventh chord", "sus4"},
            // { "incomplete minor-seventh chord", ""}, //????? c, d, f
             { "dominant-ninth", "\u2079"},
             { "minor-ninth chord", "m\u2079"},
             { "minor-major ninth chord", "mM\u2079"},
            // { "perfect fourth tetramirror", "" },
             { "augmented major tetrachord", "aug\u2077" }, // c e as h
            // { "major-diminished tetrachord", "" }, // major triad with a diminished 7th?
             { "minor-diminished ninth chord", "m\u2077(\u1D47\u2079)" }, // flat 9
             { "major-ninth chord", "maj\u2079" },
             { "major-augmented ninth chord", "maj\u2077(\u266F\u2079)" }, // c e g h dis
            // { "whole-tone pentachord", "" }, // ninth augmented fifth chord, ninth flat fifth chord
             { "Neapolitan pentachord", "\u2077\u266F\u2079" }, // dominant seventh sharp nine
             { "dominant-eleventh", "\u2071\u2071" },
             { "augmented-eleventh", "\u2077(\u266F\u2071\u2017)" }, // dominant 7, sharp 11
             { "flat-ninth pentachord", "\u2077(\u1D47\u2079)" }, // 7(b9)
            // { "major pentatonic", "" }, // major sixth ninth chord c69
             { "locrian hexachord", "maj\u2071\u2017" }, //a major eleventh chord
            // { "enigmatic pentachord", "" }, // major seventh sharp eleventh chord ?? nevem
             { "Guidonian hexachord", "m\u2071\u2071" }, // minor eleventh chord   
          // { "major scale", "" }, // major thirteenth chord, minor thirtheenth chord
             { "Perfect Fifth with octave doublings", "\u2075" },
             { "phrygian hexamirror", "maj\u2079(\u266F\u2075)" }, // c e g h d fis
             { "Hirajoshi pentatonic", "maj\u2077add#11" }, // c e g h fis
         };
        text = GameObject.Find("ChordName").GetComponent<TMP_Text>();
        
    }
    public void getChordName(List<int> notes)
    {
        if(notes.Count < 3)
        {
            return;
        }
        StartCoroutine(PostRequest(notes));
    }
    IEnumerator PostRequest(List<int> notes)
    {
        string notesToJson = JsonUtility.ToJson(new NotesData { notes = notes.ToArray() });
       // Debug.Log(notesToJson);

        using UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
        webRequest.SetRequestHeader("Content-Type", "application/json");
        byte[] bytes = Encoding.UTF8.GetBytes(notesToJson);
      //  Debug.Log(bytes); 
        webRequest.uploadHandler = new UploadHandlerRaw(bytes);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + webRequest.error);
        }
        else
        {
            string responseText = webRequest.downloadHandler.text;

            var responseData = JsonUtility.FromJson<ResponseData>(responseText);
            if(chordDictionary.ContainsKey(responseData.result)) {
                text.text = responseData.rootNote + chordDictionary[responseData.result];
            } else
            {

                text.text = "Weird chord ddetected";
            }

            Debug.Log("Result: " + responseData.result);

        }

    }

    [System.Serializable]
    public class ResponseData
    {
        public string result;
        public string rootNote;
    }

    [System.Serializable]
    public class NotesData
    {
        public int[] notes;
    }

    

}
