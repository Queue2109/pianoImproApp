using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Melanchall.DryWetMidi.MusicTheory;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PianoFunctions : MonoBehaviour
{

    private List<Transform> blackKeys; // Array to hold all the black keys
    private List<Transform> whiteKeys; // Array to hold all the white keys
    private GameObject pianoKeyboard;
    public float blackKeyScaleFactor = 1f;
    public float whiteKeyScaleFactor = 1f;
    private int keyNumber = 61;

    public Color whiteKeyDefaultColor;
    public Color blackKeyDefaultColor;

    // A nice blue with partial transparency
    public Color whiteKeyHighlightColorRight;
    // A darker blue-black with partial transparency for sharps
    public Color blackKeyHighlightColorRight;


    // A nice blue with partial transparency
    public Color whiteKeyHighlightColorLeft;
    // A darker blue-black with partial transparency for sharps
    public Color blackKeyHighlightColorLeft;

    private void Awake()
    {
        //SaveDefaultPianoPropertiesToPlayerPrefs();
        pianoKeyboard = GameObject.FindGameObjectWithTag("Piano");
        blackKeys = pianoKeyboard.GetComponentsInChildren<Transform>().Where(child => child.name.Contains("Sharp")).ToList();
        blackKeys = blackKeys.OrderBy(key => key.position.x).ToList();
        whiteKeys = pianoKeyboard.GetComponentsInChildren<Transform>().Where(child => !child.name.Contains("Sharp")).ToList();
        whiteKeys = whiteKeys.OrderBy(key => key.position.x).ToList();

        Debug.Log(whiteKeys);
        Debug.Log(blackKeys);
    }
    public void ColorKey(string key, bool isLeftHand)
    {
        GameObject noteKey = GameObject.Find(key);
        Debug.Log("Key name is " + noteKey);
        if (noteKey == null)
        {
            return;
        }

        if (noteKey.TryGetComponent<Renderer>(out var renderer))
        {
            Color highlightColor = Color.white;
            if(isLeftHand)
            {
                highlightColor = key.Contains("Sharp")
                ? blackKeyHighlightColorLeft
                : whiteKeyHighlightColorLeft;
            } else
            {
                highlightColor = key.Contains("Sharp")
                ? blackKeyHighlightColorRight
                : whiteKeyHighlightColorRight;
            }
            // Decide which highlight color to use: black key or white key?

            foreach (var mat in renderer.materials)
            {
                mat.SetColor("_Color", highlightColor);
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
            // Decide which default color to use: black key or white key?
            Color defaultColor = key.Contains("Sharp")
                ? blackKeyDefaultColor
                : whiteKeyDefaultColor;

            foreach (var mat in renderer.materials)
            {
                mat.SetColor("_Color", defaultColor);
            }
        }
    }

    public void ResetSelectedKeysToDefaultColors(HashSet<int> keyIndices)
    {
        // Reset black keys
        for (int i = 0; i < blackKeys.Count; i++)
        {
            if (keyIndices.Contains(i)) // Only reset keys in the provided list
            {
                if (blackKeys[i].TryGetComponent<Renderer>(out var renderer))
                {
                    foreach (var mat in renderer.materials)
                    {
                        mat.SetColor("_Color", blackKeyDefaultColor);
                    }
                }
            }
        }

        // Reset white keys
        for (int i = 0; i < whiteKeys.Count; i++)
        {
            if (keyIndices.Contains(i)) // Only reset keys in the provided list
            {
                if (whiteKeys[i].TryGetComponent<Renderer>(out var renderer))
                {
                    foreach (var mat in renderer.materials)
                    {
                        mat.SetColor("_Color", whiteKeyDefaultColor);
                    }
                }
            }
        }
    }

    public void ResetAllKeysToDefaultColor()
    {
        for (int i = 0; i < blackKeys.Count; i++)
        {
            if (blackKeys[i].TryGetComponent<Renderer>(out var renderer))
            {
                foreach (var mat in renderer.materials)
                {
                    mat.SetColor("_Color", blackKeyDefaultColor);
                }
            }
            
        }

        // Reset white keys
        for (int i = 0; i < whiteKeys.Count; i++)
        {
            if (whiteKeys[i].TryGetComponent<Renderer>(out var renderer))
            {
                foreach (var mat in renderer.materials)
                {
                    mat.SetColor("_Color", whiteKeyDefaultColor);
                }
            }
        }
    }

    public string NoteNameToKeyName(string note, string octave)
    {
        // check which note it is
        string newNote = note.Substring(0, 1);
        if (note.Contains("Sharp"))
        {
            newNote += "-Sharp";
        }
        newNote += octave;
        return newNote;
    }

    public void SetKeyCount(int keyCount)
    {
        // Validate input
        if (keyCount != 49 && keyCount != 61 && keyCount != 72 && keyCount != 88)
        {
            Debug.LogError("Invalid key count! Please provide 49, 61, 72, or 88.");
            return;
        }
        keyNumber = keyCount;
        // Define starting notes for different key counts
        string startNote = keyCount switch
        {
            49 => "C2",
            61 => "C2",
            72 => "E1",
            88 => "A0",
            _ => "A0" // Default, though validation prevents reaching here
        };

        // Create the expected set of keys
        List<string> allKeys = GenerateKeys(startNote, keyCount);

        // Set keys active or inactive based on required key count
        foreach (Transform child in transform)
        {
            if (allKeys.Contains(child.name) || child.name.Contains("Hand") || child.name.Contains("Chord") || child.name.Contains("PlayControl"))
            {
                child.gameObject.SetActive(true);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }

        AdjustColliderPrecisely();

        Debug.Log($"Piano adjusted to {keyCount} keys starting from {startNote}.");
    }
    private List<string> GenerateKeys(string startNote, int keyCount)
    {
        string[] notes = { "C", "C-Sharp", "D", "D-Sharp", "E", "F", "F-Sharp", "G", "G-Sharp", "A", "A-Sharp", "B" };
        List<string> keys = new List<string>();

        // Parse the starting note
        string baseNote = startNote.Contains("-Sharp") ? startNote.Split('-')[0] + "-Sharp" : startNote.Substring(0, startNote.Length - 1);
        int startIndex = System.Array.IndexOf(notes, baseNote);
        if (startIndex == -1)
        {
            Debug.LogError($"Invalid start note: {startNote}. Make sure it follows the format (e.g., 'C2' or 'A0').");
            return keys;
        }

        int startOctave = int.Parse(startNote.Substring(startNote.Length - 1));

        // Generate the keys
        for (int i = 0; i < keyCount; i++)
        {
            string note = notes[(startIndex + i) % notes.Length];
            int octave = startOctave + (startIndex + i) / notes.Length;
            keys.Add(note + octave);
        }

        return keys;
    }


    public void MoveObject(Vector3 direction)
    {
        transform.position += direction;
    }

    public void ScaleObject(float scaleFactor)
    {
        transform.localScale = new Vector3(transform.localScale.x * scaleFactor, transform.localScale.y * scaleFactor, transform.localScale.z * scaleFactor);
        AdjustColliderPrecisely();
    }
    public void ScaleWhiteKeys(float scaleFactor)
    {
        whiteKeyScaleFactor *= scaleFactor;
        foreach (Transform key in whiteKeys)
        {

            Vector3 localScale = key.localScale;
            if (localScale.y < 0)
            {
                return;
            }

            float newHeight = localScale.y * scaleFactor;

            key.localScale = new Vector3(localScale.x, newHeight, localScale.z);

            float ratioZtoY = 0.08f / 14.01483f;

            float changeInHeight = newHeight - localScale.y;

            float newZPosition = key.localPosition.z + (changeInHeight * ratioZtoY);
            key.localPosition = new Vector3(key.localPosition.x, key.localPosition.y, newZPosition);
            AdjustColliderPrecisely();
        }
    }

    public void ScaleBlackKeys(float scaleFactor)
    {
        blackKeyScaleFactor *= scaleFactor;
        foreach (Transform key in blackKeys)
        {

            Vector3 localScale = key.localScale;

            float newHeight = localScale.y * scaleFactor;

            key.localScale = new Vector3(localScale.x, newHeight, localScale.z);

            float ratioZtoY = 0.08f / 14.01483f;

            float changeInHeight = newHeight - localScale.y;

            float newZPosition = key.localPosition.z + (changeInHeight * ratioZtoY);
            key.localPosition = new Vector3(key.localPosition.x, key.localPosition.y, newZPosition);
        }
    }

    public void MoveForward()
    {
        MoveObject(-transform.forward * 0.01f);
    }

    public void MoveBackward()
    {
        MoveObject(transform.forward * 0.01f);
    }

    public void MoveLeft()
    {
        MoveObject(transform.right * 0.01f);
    }

    public void MoveRight()
    {
        MoveObject(-transform.right * 0.01f);
    }

    public void RotateLeft()
    {
        transform.Rotate(transform.up, -1f, Space.Self);
    }

    // Function to rotate the object to the right (clockwise)
    public void RotateRight()
    {
        transform.Rotate(transform.up, 1f, Space.Self);
    }

    public void MovePianoUp()
    {
        MoveObject(transform.up * 0.01f);
    }

    public void MovePianoDown()
    {
        MoveObject(-transform.up * 0.01f);
    }

    public void AdjustColliderPrecisely()
    {
        if (pianoKeyboard == null)
        {
            Debug.LogError("Piano GameObject not found!");
            return;
        }

        BoxCollider boxCol = pianoKeyboard.GetComponent<BoxCollider>();
        if (boxCol == null)
        {
            Debug.LogError("BoxCollider component not found on the piano keyboard object.");
            return;
        }

        // Grab ALL active Renderers (white/black keys), 
        // excluding any "Hand", "PlayControl", or "Chord" objects.
        var keyRenderers = pianoKeyboard.GetComponentsInChildren<Renderer>()
            .Where(r => r.gameObject.activeSelf
                        && !r.name.Contains("Hand")
                        && !r.name.Contains("Chord"))
            .ToList();

        if (keyRenderers.Count == 0)
        {
            Debug.LogWarning("No active key renderers found. Collider not adjusted.");
            return;
        }

        // Initialize bounding box min/max in local space
        Vector3 minLocal = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxLocal = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        // We'll keep the existing collider center
        Vector3 originalCenter = (minLocal + maxLocal) * 0.5f;

        // Calculate the bounding box of all renderers in local coordinates
        foreach (var rend in keyRenderers)
        {
            // Bounds are in world space
            Bounds worldBounds = rend.bounds;

            // Convert the 8 corners of the world bounding box to local space
            Vector3[] corners =
            {
            new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z),
            new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.max.z),
            new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.min.z),
            new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.max.z),
            new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.min.z),
            new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.max.z),
            new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.min.z),
            new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z)
        };

            foreach (Vector3 corner in corners)
            {
                Vector3 localPos = pianoKeyboard.transform.InverseTransformPoint(corner);

                // Update local min/max
                minLocal = Vector3.Min(minLocal, localPos);
                maxLocal = Vector3.Max(maxLocal, localPos);
            }
        }

        // New size = difference between local max and min
        Vector3 newSize = maxLocal - minLocal;

        // Assign the new size to the collider
        // We do NOT change center — we keep whatever was in boxCol.center
        boxCol.size = newSize;

        // (Optional) if you want to confirm or log the results:
        Debug.Log($"AdjustColliderPrecisely: size={newSize}, centerPreserved={originalCenter}");
    }

    public void BringPianoCloser()
    {

        OVRCameraRig cameraRig = FindAnyObjectByType<OVRCameraRig>();
        if (cameraRig == null)
        {
            Debug.LogError("OVRCameraRig not found in the scene!");
            return;
        }

        Transform ovrHand = cameraRig.leftControllerAnchor;

        if (ovrHand == null)
        {
            Debug.LogError("Right hand transform not available!");
            return;
        }

        // Get the current position of the object and the hand
        Vector3 currentPosition = gameObject.transform.position;
        Vector3 handPosition = ovrHand.position;

        // Keep only the desired axis (e.g., Z-axis)
        currentPosition.z = handPosition.z;

        // Apply the updated position
        gameObject.transform.position = currentPosition;
    }

    public void SavePianoPropertiesToPlayerPrefs()
    {

        Vector3 scale = transform.localScale;
        PlayerPrefs.SetFloat("ObjectScaleX", scale.x);
        PlayerPrefs.SetFloat("ObjectScaleY", scale.y);
        PlayerPrefs.SetFloat("ObjectScaleZ", scale.z);

        PlayerPrefs.SetFloat("BlackKeyScale", blackKeyScaleFactor);
        PlayerPrefs.SetFloat("WhiteKeyScale", whiteKeyScaleFactor);
        PlayerPrefs.SetInt("KeyNumber", keyNumber);

        // Persist
        PlayerPrefs.Save();
    }

    public void LoadPianoPropertiesFromPlayerPrefs()
    {
        float sx = PlayerPrefs.GetFloat("ObjectScaleX", 0.08f);
        float sy = PlayerPrefs.GetFloat("ObjectScaleY", 0.06f);
        float sz = PlayerPrefs.GetFloat("ObjectScaleZ", 0.1f);
        transform.localScale = new Vector3(sx, sy, sz);

        SetKeyCount(PlayerPrefs.GetInt("KeyNumber", 61));

        blackKeyScaleFactor = PlayerPrefs.GetFloat("BlackKeyScale", 1f);
        whiteKeyScaleFactor = PlayerPrefs.GetFloat("WhiteKeyScale", 1f);
        ApplyWhiteKeyScale();
        ApplyBlackKeyScale();
    }

    private void ApplyWhiteKeyScale()
    {
        foreach (var key in whiteKeys)
        {
            // Start from the original Y scale we captured in Awake()
            float originalY = key.localScale.y;
            float newY = originalY * whiteKeyScaleFactor;

            key.localScale = new Vector3(key.localScale.x, newY, key.localScale.z);

            // If you want to re-calculate localPosition the same way:
            // because we've gone from originalY -> newY
            float ratioZtoY = 0.08f / 14.01483f;
            float changeInHeight = newY - originalY;
            float newZPosition = key.localPosition.z + (changeInHeight * ratioZtoY);
            key.localPosition = new Vector3(key.localPosition.x, key.localPosition.y, newZPosition);
        }
    }

    private void ApplyBlackKeyScale()
    {
        foreach (var key in blackKeys)
        {
            float originalY = key.localScale.y;
            float newY = originalY * blackKeyScaleFactor;

            key.localScale = new Vector3(key.localScale.x, newY, key.localScale.z);

            // Position offset
            float ratioZtoY = 0.08f / 14.01483f;
            float changeInHeight = newY - originalY;
            float newZPosition = key.localPosition.z + (changeInHeight * ratioZtoY);
            key.localPosition = new Vector3(key.localPosition.x, key.localPosition.y, newZPosition);
        }
    }
}