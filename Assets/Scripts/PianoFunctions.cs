using System.Collections;
using System.Collections.Generic;
using Melanchall.DryWetMidi.MusicTheory;
using UnityEngine;

public class PianoFunctions : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ColorKey(string key)
    {
        GameObject noteKey = GameObject.Find(key);
        if (noteKey == null)
        {
            return;
        }
        if (noteKey.TryGetComponent<Renderer>(out var renderer))
        {
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                renderer.materials[i].SetColor("_Color", new Color(0.545f, 0.769f, 0.910f, 0.33f));
            }
        }
    }

    public void ResetKeyColor(string key)
    {

        GameObject noteKey = GameObject.Find(key);
        if (noteKey == null)
        {
            return;
        }
        if (noteKey.TryGetComponent<Renderer>(out var renderer))
        {
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                if(key.Contains("Sharp"))
                {
                    renderer.materials[i].SetColor("_Color", new Color(0f, 0f, 0f, 1f));

                } else
                {
                    renderer.materials[i].SetColor("_Color", new Color(1f, 1f, 1f, 1f));

                }
            }
        }
    }

    public string NoteNameToKeyName(string note, string octave)
    {
        // check which note it is
        string newNote = note.Substring(0, 1);
        if(note.Contains("Sharp"))
        {
            newNote += "-Sharp";
        }
        newNote += octave;
        return newNote;
    }


}
