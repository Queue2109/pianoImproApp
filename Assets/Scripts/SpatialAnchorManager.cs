using UnityEngine;
using Oculus;
using UnityEngine.XR.ARFoundation;

public class SpatialAnchorManager : MonoBehaviour
{

    void Start()
    {
           gameObject.AddComponent<ARAnchor>();
    }

    [System.Obsolete]
    public void SaveAnchor()
    {
        //    if (spatialAnchor != null)
        //    {
        //        spatialAnchor.Save((success, uuid) =>
        //        {
        //            if (success)
        //            {
        //                PlayerPrefs.SetString("SavedAnchorUUID", uuid.ToString());
        //                PlayerPrefs.Save();
        //                Debug.Log($"Spatial Anchor saved with UUID: {uuid}");
        //            }
        //            else
        //            {
        //                Debug.LogError("Failed to save spatial anchor.");
        //            }
        //        });
        //    }
    }
}
