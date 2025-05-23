using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class RelativeTransformSaver : MonoBehaviour
{
    [Tooltip("Reference object to which this object is relative")]
    public GameObject referenceObject;

    [Tooltip("Default local offset relative to the reference object")]
    public Vector3 defaultLocalOffset;

    [Tooltip("Default local rotation relative to the reference object")]
    public Vector3 defaultLocalEuler; // editable in Inspector
    private Quaternion defaultLocalRotation => Quaternion.Euler(defaultLocalEuler);

    private string PanelPosKey => gameObject.name + "_LocalPosition";
    private string PanelRotKey => gameObject.name + "_LocalRotation";
    private Vector3 lastReferencePosition;
    private Quaternion lastReferenceRotation;

    public void LoadOnStart()
    {
        if (referenceObject == null)
        {
            Debug.LogError("Reference Object not set.");
            return;
        }

        if (PlayerPrefs.HasKey(PanelPosKey) && PlayerPrefs.HasKey(PanelRotKey))
        {
            UpdatePanelPositionFromPrefs(); 
        }
        else
        {
            ApplyDefault();
        }
    }

    private void ApplyDefault()
    {
        Vector3 newWorldPosition = referenceObject.transform.position + referenceObject.transform.rotation * defaultLocalOffset;
        Quaternion newWorldRotation = referenceObject.transform.rotation * defaultLocalRotation;

        transform.position = newWorldPosition;
        transform.rotation = newWorldRotation;

        PlayerPrefs.SetString(PanelPosKey, Vector3ToString(defaultLocalOffset));
        PlayerPrefs.SetString(PanelRotKey, QuaternionToString(defaultLocalRotation));
        PlayerPrefs.Save();
    }

    public void RecalculateAndSaveOffset()
    {
        if (referenceObject == null)
        {
            Debug.LogError("Reference Object not set.");
            return;
        }

        Vector3 localOffset = Quaternion.Inverse(referenceObject.transform.rotation) *
                              (transform.position - referenceObject.transform.position);

        Quaternion localRotationOffset = Quaternion.Inverse(referenceObject.transform.rotation) *
                                         transform.rotation;

        PlayerPrefs.SetString(PanelPosKey, Vector3ToString(localOffset));
        PlayerPrefs.SetString(PanelRotKey, QuaternionToString(localRotationOffset));
        PlayerPrefs.Save();
    }

    public void UpdatePanelPositionFromPrefs()
    {
        if (referenceObject == null)
        {
            Debug.LogError("Reference Object not set.");
            return;
        }

        if (PlayerPrefs.HasKey(PanelPosKey) && PlayerPrefs.HasKey(PanelRotKey))
        {
            Vector3 savedLocalOffset = StringToVector3(PlayerPrefs.GetString(PanelPosKey));
            Quaternion savedLocalRot = StringToQuaternion(PlayerPrefs.GetString(PanelRotKey));

            transform.position = referenceObject.transform.position + referenceObject.transform.rotation * savedLocalOffset;
            transform.rotation = referenceObject.transform.rotation * savedLocalRot;
            lastReferencePosition = referenceObject.transform.position;
            lastReferenceRotation = referenceObject.transform.rotation;
        }
    }

    #region Utility Methods (Using InvariantCulture)

    private string Vector3ToString(Vector3 vec)
    {
        return $"{vec.x.ToString(CultureInfo.InvariantCulture)},{vec.y.ToString(CultureInfo.InvariantCulture)},{vec.z.ToString(CultureInfo.InvariantCulture)}";
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
        return $"{quat.x.ToString(CultureInfo.InvariantCulture)},{quat.y.ToString(CultureInfo.InvariantCulture)},{quat.z.ToString(CultureInfo.InvariantCulture)},{quat.w.ToString(CultureInfo.InvariantCulture)}";
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
