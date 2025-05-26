using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using static OVRSpatialAnchor;

public class NewAnchorManager : MonoBehaviour
{
    public GameObject go;
    public PianoFunctions pianoFunctions;
    public PanelManagerSongList panelManagerSongList;
    public RelativeTransformSaver relativeTransformSaver;
    public RelativeTransformSaver relativeTransformSaver2;

    private Guid anchorUuid = Guid.Empty;
    private OVRSpatialAnchor anchor;

    private bool isDebugMode = true;

    bool anchorReady = false;
    private bool isAnchorOperationRunning = false;
    private List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();

    private async void Start()
    {
        await WaitForTrackingAsync();
        pianoFunctions.LoadPianoPropertiesFromPlayerPrefs();
        LoadAnchorUuid();

        if (anchorUuid != Guid.Empty)
        {
            var anchorsToLoad = new List<Guid> { anchorUuid };
            anchorReady = await LoadAnchorsByUuidAsync(anchorsToLoad);

            if (!anchorReady)
            {
                LogWarning("Failed to load existing anchor. Clearing UUID and creating a new one.");
                ClearAnchorUuid();
            }
        }

        if (!anchorReady)
        {
            pianoFunctions.BringPianoCloser();
            anchorReady = await CreateSpatialAnchorAsync();
            if (anchorReady)
            {
                await SaveCurrentAnchorAsync();
            }
        }

        pianoFunctions.AdjustColliderPrecisely();

        if (anchorReady)
        {
            await Task.Delay(1000);
            Log("Anchor is ready.");
            relativeTransformSaver.LoadOnStart();
            relativeTransformSaver2.LoadOnStart();
        }
        else
        {
            LogError("Failed to load or create a valid anchor. UI may not be positioned.");
        }
    }

    private void ClearAnchorUuid()
    {
        anchorUuid = Guid.Empty;
        PlayerPrefs.DeleteKey("AnchorUuid");
        Log("Cleared Anchor UUID from PlayerPrefs.");
    }

    private async Task WaitForTrackingAsync()
    {
        while (!OVRManager.isHmdPresent || !OVRManager.tracker.isPositionTracked)
        {
            Log("Waiting for headset tracking...");
            await Task.Delay(500);
        }
        Log("Headset position tracking is active.");
    }


    /// <summary>
    /// Creates an OVRSpatialAnchor if none exists yet. Waits for creation to complete.
    /// </summary>
    private async Task<bool> CreateSpatialAnchorAsync()
    {
        if (isAnchorOperationRunning) return false;
        isAnchorOperationRunning = true;

        try
        {
            if (anchor != null && anchor.Created)
            {
                Log("Anchor already exists.");
                return true;
            }

            anchor = go.GetComponent<OVRSpatialAnchor>() ?? go.AddComponent<OVRSpatialAnchor>();

            while (!anchor.Created)
            {
                await Task.Delay(500);
            }
            anchorUuid = anchor.Uuid;
            Log($"Anchor created with UUID: {anchor.Uuid}");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Error creating anchor: {ex.Message}");
            return false;
        }
        finally
        {
            isAnchorOperationRunning = false;
        }
    }

    /// <summary>
    /// Saves the current anchor if it exists and is created.
    /// </summary>
    private async Task SaveCurrentAnchorAsync()
    {
        if (anchor == null || anchor.Uuid == Guid.Empty)
        {
            LogError("Invalid anchor, cannot save.");
            return;
        }

        var result = await anchor.SaveAnchorAsync();
        if (result.Success)
        {
            SaveAnchorUuid(anchorUuid);
            Log($"Anchor {anchorUuid} saved successfully.");
        }
        else
        {
            LogError($"Failed to save anchor: {result.Status}");
        }
    }

    /// <summary>
    /// Loads and binds anchors based on given UUIDs.
    /// </summary>
    private async Task<bool> LoadAnchorsByUuidAsync(IEnumerable<Guid> uuids)
    {
        if (uuids == null)
        {
            Debug.LogWarning("No UUIDs provided to load.");
            return false;
        }

        var result = await LoadUnboundAnchorsAsync(uuids, _unboundAnchors);
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
                    return true;
                }
                else
                {
                    Debug.LogError($"Localization failed for anchor {unboundAnchor.Uuid}");
                    return false;
                }
            }
            return false;
        }
        else
        {
            Debug.LogError($"Anchor load failed with error {result.Status}.");
            return false;
        }
    }

    /// <summary>
    /// Erases the current anchor (if any) from the system and clears the UUID.
    /// </summary>
    /// 
    public void OnEraseButtonClicked()
    {
        OnEraseButtonPressed();
    }
    private async void OnEraseButtonPressed()
    {
        await OnEraseButtonPressedAsync();
    }
    private async Task<bool> OnEraseButtonPressedAsync()
    {
        if (anchor == null)
        {
            LogWarning("No anchor to erase. Clearing saved UUID.");
            ClearAnchorUuid();
            return false;
        }

        var result = await anchor.EraseAnchorAsync();
        if (result.Success)
        {
            Destroy(go.GetComponent<OVRSpatialAnchor>());
            anchor = null;
            ClearAnchorUuid();
            Log("Successfully erased anchor and cleared UUID.");
            return true;
        }
        else
        {
            LogError($"Failed to erase anchor {anchorUuid} with result {result.Status}");
            return false;
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
    public void LoadAnchorUuid()
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

    public void EraseAndCreateAnchor()
    {
        EraseAndCreateAnchorAsync();
    }

    private async void EraseAndCreateAnchorAsync()
    {
        await OnEraseButtonPressedAsync();
        pianoFunctions.BringPianoCloser();
        await Task.Delay(500);
        await OnSaveButtonPressedAsync();

    }

    public void OnSaveButtonCLicked()
    {
        OnSaveButtonPressed();
    }
    private async void OnSaveButtonPressed()
    {
        await OnSaveButtonPressedAsync();
    }

    /// <summary>
    /// Public method to manually create and save an anchor at runtime (e.g., from a UI button).
    /// </summary>
    private async Task<bool> OnSaveButtonPressedAsync()
    {
        if (anchor == null)
        {
            LogWarning("No anchor exists. Creating a new one before saving.");
            anchorReady = await CreateSpatialAnchorAsync();
        }

        if (anchorReady)
        {
            await SaveCurrentAnchorAsync();
            pianoFunctions.SavePianoPropertiesToPlayerPrefs();
            go.GetComponent<OVRSpatialAnchor>().enabled = false;

            return true;
        }
        else
        {
            LogError("Anchor creation failed. Cannot save.");
            return false;
        }
    }

    private void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }

    private void LogError(string message)
    {
        if (isDebugMode) Debug.LogError(message);
    }

    private void LogWarning(string message)
    {
        if (isDebugMode) Debug.LogWarning(message);
    }
}
