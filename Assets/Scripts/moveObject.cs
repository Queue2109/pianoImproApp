using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveObject : MonoBehaviour
{
    public Transform anchoredObject; // assign your anchored object here
    private const string PanelPosKey = "PanelPosition";
    private const string PanelRotKey = "PanelRotation";

    void Start()
    {
        PlayerPrefs.DeleteKey(PanelRotKey);
        PlayerPrefs.DeleteKey(PanelPosKey);
        if (PlayerPrefs.HasKey(PanelPosKey))
        {
            // Load saved world position and rotation
            Vector3 savedPos = StringToVector3(PlayerPrefs.GetString(PanelPosKey));
            Quaternion savedRot = StringToQuaternion(PlayerPrefs.GetString(PanelRotKey));
            transform.SetParent(null); // Ensure panel is independent
            transform.position = savedPos;
            transform.rotation = savedRot;
        }
    }

    // Call this method when the panel is dropped after being grabbed
    public void OnPanelDrop()
    {
        transform.SetParent(null); // Detach if necessary

        // Save current world position and rotation
        PlayerPrefs.SetString(PanelPosKey, Vector3ToString(transform.position));
        PlayerPrefs.SetString(PanelRotKey, QuaternionToString(transform.rotation));
        PlayerPrefs.Save();
        Debug.Log("WIII here");
    }

    // Utility methods to convert Vector3 and Quaternion to/from string
    private string Vector3ToString(Vector3 vec)
    {
        return vec.x + "," + vec.y + "," + vec.z;
    }

    private Vector3 StringToVector3(string s)
    {
        string[] values = s.Split(',');
        return new Vector3(
            float.Parse(values[0]),
            float.Parse(values[1]),
            float.Parse(values[2])
        );
    }

    private string QuaternionToString(Quaternion quat)
    {
        return quat.x + "," + quat.y + "," + quat.z + "," + quat.w;
    }

    private Quaternion StringToQuaternion(string s)
    {
        string[] values = s.Split(',');
        return new Quaternion(
            float.Parse(values[0]),
            float.Parse(values[1]),
            float.Parse(values[2]),
            float.Parse(values[3])
        );
    }
}
