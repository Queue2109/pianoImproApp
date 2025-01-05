using UnityEngine;
using Oculus;

public class SpatialAnchorManager : MonoBehaviour
{
    private OVRSpatialAnchor spatialAnchor;

    void Start()
    {
        // Attach an OVRSpatialAnchor component to the GameObject
        if (spatialAnchor == null)
        {
            spatialAnchor = gameObject.AddComponent<OVRSpatialAnchor>();
        }
    }

    [System.Obsolete]
    public void SaveAnchor()
    {
        if (spatialAnchor != null)
        {
            spatialAnchor.Save((success, uuid) =>
            {
                if (success)
                {
                    PlayerPrefs.SetString("SavedAnchorUUID", uuid.ToString());
                    PlayerPrefs.Save();
                    Debug.Log($"Spatial Anchor saved with UUID: {uuid}");
                }
                else
                {
                    Debug.LogError("Failed to save spatial anchor.");
                }
            });
        }
    }
}
