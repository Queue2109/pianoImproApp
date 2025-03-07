using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization; // For InvariantCulture

public class moveObject : MonoBehaviour
{
    // Reference to the piano object
    public GameObject pianoKeyboard;

    // Default local offset and rotation relative to the piano (set these in the Inspector)
    public Vector3 defaultLocalOffset = new Vector3(-0.4f, 0.2f, 0);
    // Default local rotation with -45 degrees on the X axis.
    private Quaternion defaultLocalRotation = Quaternion.Euler(-45f, -180f, 0f);

    // Keys for saving local transform data in PlayerPrefs
    private const string PanelPosKey = "PanelLocalPosition";
    private const string PanelRotKey = "PanelLocalRotation";

    public void LoadOnStart()
    {
        Debug.Log("=== START ===");
        Debug.Log("Piano Position: " + pianoKeyboard.transform.position);
        Debug.Log("Piano Rotation: " + pianoKeyboard.transform.rotation);

        if (PlayerPrefs.HasKey(PanelPosKey) && PlayerPrefs.HasKey(PanelRotKey))
        {
            UpdatePanelPositionFromPrefs();
        }
        else
        {
            Debug.Log("No saved local offset/rotation found. Using defaults:");
            Debug.Log("Default Local Offset: " + defaultLocalOffset);
            Debug.Log("Default Local Rotation (should be -45° on X): " + defaultLocalRotation);

            Vector3 newWorldPosition = pianoKeyboard.transform.position + pianoKeyboard.transform.rotation * defaultLocalOffset;
            Quaternion newWorldRotation = pianoKeyboard.transform.rotation * defaultLocalRotation;

            Debug.Log("Calculated Panel World Position using defaults: " + newWorldPosition);
            Debug.Log("Calculated Panel World Rotation using defaults: " + newWorldRotation);

            transform.position = newWorldPosition;
            transform.rotation = newWorldRotation;

            // Save the defaults
            PlayerPrefs.SetString(PanelPosKey, Vector3ToString(defaultLocalOffset));
            PlayerPrefs.SetString(PanelRotKey, QuaternionToString(defaultLocalRotation));
            PlayerPrefs.Save();

            Debug.Log("Defaults saved to PlayerPrefs.");
        }

        Debug.Log("Panel's final Start() Position: " + transform.position);
        Debug.Log("Panel's final Start() Rotation: " + transform.rotation);
    }

    /// <summary>
    /// Recalculate the panel's local offset and rotation relative to the piano, then save them.
    /// </summary>
    public void RecalculateAndSaveOffset()
    {
        Debug.Log("=== RecalculateAndSaveOffset Called ===");
        Debug.Log("Piano Position: " + pianoKeyboard.transform.position);
        Debug.Log("Piano Rotation: " + pianoKeyboard.transform.rotation);
        Debug.Log("Panel Current World Position: " + transform.position);
        Debug.Log("Panel Current World Rotation: " + transform.rotation);

        // Calculate local offset using InverseTransformPoint (equivalent to our manual method)
        Vector3 localOffset = Quaternion.Inverse(pianoKeyboard.transform.rotation) *
                              (transform.position - pianoKeyboard.transform.position);

        // Calculate local rotation relative to the piano
        Quaternion localRotationOffset = Quaternion.Inverse(pianoKeyboard.transform.rotation) *
                                         transform.rotation;

        Debug.Log("Calculated Local Offset: " + localOffset);
        Debug.Log("Calculated Local Rotation Offset: " + localRotationOffset);

        PlayerPrefs.SetString(PanelPosKey, Vector3ToString(localOffset));
        PlayerPrefs.SetString(PanelRotKey, QuaternionToString(localRotationOffset));
        PlayerPrefs.Save();

        Debug.Log("Recalculated local offset and rotation saved to PlayerPrefs.");
    }

    /// <summary>
    /// Update the panel's world transform based on the saved local offset/rotation from PlayerPrefs.
    /// </summary>
    public void UpdatePanelPositionFromPrefs()
    {
        Debug.Log("=== UpdatePanelPositionFromPrefs Called ===");
        Debug.Log("Piano Position: " + pianoKeyboard.transform.position);
        Debug.Log("Piano Rotation: " + pianoKeyboard.transform.rotation);

        if (PlayerPrefs.HasKey(PanelPosKey) && PlayerPrefs.HasKey(PanelRotKey))
        {
            string posString = PlayerPrefs.GetString(PanelPosKey);
            string rotString = PlayerPrefs.GetString(PanelRotKey);
            Debug.Log("Retrieved saved Position String: " + posString);
            Debug.Log("Retrieved saved Rotation String: " + rotString);

            Vector3 savedLocalOffset = StringToVector3(posString);
            Quaternion savedLocalRot = StringToQuaternion(rotString);

            Debug.Log("Retrieved saved local offset: " + savedLocalOffset);
            Debug.Log("Retrieved saved local rotation: " + savedLocalRot);

            Vector3 newWorldPosition = pianoKeyboard.transform.position + pianoKeyboard.transform.rotation * savedLocalOffset;
            Quaternion newWorldRotation = pianoKeyboard.transform.rotation * savedLocalRot;

            Debug.Log("Calculated new Panel World Position: " + newWorldPosition);
            Debug.Log("Calculated new Panel World Rotation: " + newWorldRotation);

            transform.position = newWorldPosition;
            transform.rotation = newWorldRotation;

            Debug.Log("Panel's updated Position: " + transform.position);
            Debug.Log("Panel's updated Rotation: " + transform.rotation);
        }
        else
        {
            Debug.LogWarning("No saved local offset/rotation found in PlayerPrefs!");
        }
    }

    #region Utility Methods (Using InvariantCulture)

    private string Vector3ToString(Vector3 vec)
    {
        return vec.x.ToString(CultureInfo.InvariantCulture) + "," +
               vec.y.ToString(CultureInfo.InvariantCulture) + "," +
               vec.z.ToString(CultureInfo.InvariantCulture);
    }

    private Vector3 StringToVector3(string s)
    {
        string[] values = s.Split(',');
        return new Vector3(
            float.Parse(values[0], CultureInfo.InvariantCulture),
            float.Parse(values[1], CultureInfo.InvariantCulture),
            float.Parse(values[2], CultureInfo.InvariantCulture)
        );
    }

    private string QuaternionToString(Quaternion quat)
    {
        return quat.x.ToString(CultureInfo.InvariantCulture) + "," +
               quat.y.ToString(CultureInfo.InvariantCulture) + "," +
               quat.z.ToString(CultureInfo.InvariantCulture) + "," +
               quat.w.ToString(CultureInfo.InvariantCulture);
    }

    private Quaternion StringToQuaternion(string s)
    {
        string[] values = s.Split(',');
        return new Quaternion(
            float.Parse(values[0], CultureInfo.InvariantCulture),
            float.Parse(values[1], CultureInfo.InvariantCulture),
            float.Parse(values[2], CultureInfo.InvariantCulture),
            float.Parse(values[3], CultureInfo.InvariantCulture)
        );
    }

    #endregion
}
