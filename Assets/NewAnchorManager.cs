using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static OVRSpatialAnchor;

public class NewAnchorManager : MonoBehaviour
{
    [Header("Assign the GameObject to attach the anchor")]
    public GameObject go;
    public PianoFunctions pianoFunctions;
    public PanelManagerSongList panelManagerSongList;
    public moveObject moveObjectScript;

    private Guid anchorUuid = Guid.Empty;
    private OVRSpatialAnchor anchor;
    private List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();

    private async void Start()
    {
        // Load any existing UUID from PlayerPrefs
        pianoFunctions.LoadPianoPropertiesFromPlayerPrefs();
        LoadAnchorUuid();

        if (anchorUuid != Guid.Empty)
        {
            // If we have a saved UUID, attempt to load that anchor
            var anchorsToLoad = new List<Guid> { anchorUuid };
            await LoadAnchorsByUuidAsync(anchorsToLoad);
        }
        else
        {
            // No previously saved anchor, so create a new one and save it
            pianoFunctions.BringPianoCloser();
            await CreateSpatialAnchorAsync();
            await SaveCurrentAnchorAsync();
        }
        pianoFunctions.AdjustCollider();
        moveObjectScript.LoadOnStart();
    }

    /// <summary>
    /// Creates an OVRSpatialAnchor if none exists yet. Waits for creation to complete.
    /// </summary>
    private async Task CreateSpatialAnchorAsync()
    {
        // Check if we already have a valid anchor
        if (anchor != null && anchor.Created)
        {
            Debug.Log("Anchor already exists and is created.");
            return;
        }

        // Either get or add OVRSpatialAnchor
        anchor = go.GetComponent<OVRSpatialAnchor>();
        if (anchor == null)
        {
            anchor = go.AddComponent<OVRSpatialAnchor>();
            Debug.Log("Added OVRSpatialAnchor to GameObject.");
        }

        // Wait until OVRSpatialAnchor reports it is created
        while (!anchor.Created)
        {
            Debug.Log("Waiting for anchor to be created...");
            await Task.Delay(500);
        }

        Debug.Log($"Anchor created with UUID: {anchor.Uuid}");
    }

    /// <summary>
    /// Saves the current anchor if it exists and is created.
    /// </summary>
    private async Task SaveCurrentAnchorAsync()
    {
        if (anchor == null)
        {
            Debug.LogError("No anchor to save.");
            return;
        }

        Debug.Log("Attempting to save anchor...");
        var result = await anchor.SaveAnchorAsync();
        if (result.Success)
        {
            anchorUuid = anchor.Uuid;
            SaveAnchorUuid(anchorUuid);
            Debug.Log($"Anchor {anchor.Uuid} saved successfully.");
        }
        else
        {
            Debug.LogError($"Anchor {anchor.Uuid} failed to save with error {result.Status}");
        }
    }

    /// <summary>
    /// Loads and binds anchors based on given UUIDs.
    /// </summary>
    private async Task LoadAnchorsByUuidAsync(IEnumerable<Guid> uuids)
    {
        if (uuids == null)
        {
            Debug.LogWarning("No UUIDs provided to load.");
            return;
        }

        var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, _unboundAnchors);
        if (result.Success)
        {
            Debug.Log("Anchors loaded successfully.");
            foreach (var unboundAnchor in _unboundAnchors)
            {
                // Localize
                bool localized = await unboundAnchor.LocalizeAsync();
                if (localized)
                {
                    Debug.Log($"Anchor {unboundAnchor.Uuid} localized successfully.");

                    // Bind to our single anchor instance (avoid duplicates)
                    // Check if we already have a valid anchor
                    anchor = go.GetComponent<OVRSpatialAnchor>();
                    if (anchor == null)
                    {
                        anchor = go.AddComponent<OVRSpatialAnchor>();
                        Debug.Log("Added OVRSpatialAnchor for binding.");
                    }

                    unboundAnchor.BindTo(anchor);
                    Debug.Log($"Anchor {unboundAnchor.Uuid} bound to GameObject.");
                }
                else
                {
                    Debug.LogError($"Localization failed for anchor {unboundAnchor.Uuid}");
                }
            }
        }
        else
        {
            Debug.LogError($"Anchor load failed with error {result.Status}.");
        }
    }

    /// <summary>
    /// Erases the current anchor (if any) from the system and clears the UUID.
    /// </summary>
    public async void OnEraseButtonPressed()
    {
        if (anchor == null)
        {
            Debug.LogWarning("No anchor to erase. Clearing saved UUID.");
            anchorUuid = Guid.Empty;
            PlayerPrefs.DeleteKey("AnchorUuid");

            return;
        }

        var result = await anchor.EraseAnchorAsync();
        if (result.Success)
        {
            
            Destroy(go.GetComponent<OVRSpatialAnchor>());
            anchor = null;
            anchorUuid = Guid.Empty;
            PlayerPrefs.DeleteKey("AnchorUuid");
            Debug.Log("Successfully erased anchor and cleared UUID.");
        }
        else
        {
            Debug.LogError($"Failed to erase anchor {anchor.Uuid} with result {result.Status}");
        }
    }

    /// <summary>
    /// Persists the current anchor's UUID in PlayerPrefs.
    /// </summary>
    private void SaveAnchorUuid(Guid uuid)
    {
        PlayerPrefs.SetString("AnchorUuid", uuid.ToString());
        PlayerPrefs.Save();
        Debug.Log($"Anchor UUID {uuid} saved to PlayerPrefs.");
    }

    /// <summary>
    /// Loads a previously saved anchor UUID from PlayerPrefs.
    /// </summary>
    private void LoadAnchorUuid()
    {
        if (PlayerPrefs.HasKey("AnchorUuid"))
        {
            string savedString = PlayerPrefs.GetString("AnchorUuid");
            if (Guid.TryParse(savedString, out Guid parsed))
            {
                anchorUuid = parsed;
                Debug.Log($"Loaded Anchor UUID: {anchorUuid}");
            }
            else
            {
                Debug.LogWarning($"Could not parse saved anchor UUID {savedString}. Resetting to empty.");
                anchorUuid = Guid.Empty;
            }
        }
        else
        {
            Debug.LogWarning("No saved anchor UUID found in PlayerPrefs.");
        }
    }

    /// <summary>
    /// Public method to manually create and save an anchor at runtime (e.g., from a UI button).
    /// </summary>
    public async void OnSaveButtonPressed()
    {
        await CreateSpatialAnchorAsync();
        await SaveCurrentAnchorAsync();
        pianoFunctions.SavePianoPropertiesToPlayerPrefs();
    }
}
