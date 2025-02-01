using UnityEngine;
using System;
using System.Collections.Generic;
using Meta.XR.BuildingBlocks;
using System.Collections;
using UnityEngine.XR.ARFoundation;

public class SpatialAnchorManager : MonoBehaviour
{
    public GameObject targetPrefab; // The prefab with which we will work
    public SpatialAnchorCoreBuildingBlock spatialAnchorCore;

    private Guid currentAnchorUuid; // To track the existing anchor's UUID

    private void Start()
    {
        ////DeleteAnchor();
        //LoadAnchorUuid(); // Load the saved UUID from PlayerPrefs

        //if (spatialAnchorCore == null)
        //{
        //    Debug.LogError("SpatialAnchorCoreBuildingBlock not found!");
        //    return;
        //}

        //UpdateAnchorFromUI();

        StartCoroutine(TestAnchorFlow());
    }

    private void LoadExistingAnchor()
    {
        LoadAnchorUuid();
        Debug.Log($"Attempting to load anchor with UUID: {currentAnchorUuid}");

        if (currentAnchorUuid != Guid.Empty)
        {
            List<Guid> anchorUuids = new List<Guid> { currentAnchorUuid };

            // Load anchors using the target prefab and UUIDs
            spatialAnchorCore.LoadAndInstantiateAnchors(targetPrefab, anchorUuids);

            Debug.Log("Anchor load request sent.");
        }
        else
        {
            Debug.LogWarning("No valid anchor UUID found. Positioning object at default location.");
        }
    }


    private void AttachAnchorToObject(OVRSpatialAnchor loadedAnchor)
    {
        Debug.Log("Attaching anchor to the existing object.");

        // Transfer the anchor to the original GameObject
        loadedAnchor.transform.SetParent(targetPrefab.transform, false);
        loadedAnchor.transform.localPosition = Vector3.zero;
        loadedAnchor.transform.localRotation = Quaternion.identity;


        Debug.Log("Anchor successfully attached to the original object.");
    }

    public void UpdateAnchorFromUI()
    {
        Vector3 newAnchorPosition = targetPrefab.transform.position;
        Quaternion newAnchorRotation = targetPrefab.transform.rotation;

        UpdateAnchor(newAnchorPosition, newAnchorRotation);
    }

    public void UpdateAnchor(Vector3 newPosition, Quaternion newRotation)
    {
        Debug.Log($"Updating anchor position to {newPosition} and rotation to {newRotation}.");

        var spatialAnchor = targetPrefab.GetComponent<OVRSpatialAnchor>();

        if (spatialAnchor == null)
        {
            targetPrefab.AddComponent<OVRSpatialAnchor>();
            spatialAnchor = targetPrefab.GetComponent<OVRSpatialAnchor>();
            Debug.Log("Added OVRSpatialAnchor to the target prefab.");
        }

        // Update the object's position and rotation
        targetPrefab.transform.position = newPosition;
        targetPrefab.transform.rotation = newRotation;

        if (spatialAnchor.Localized) // Correct property check
        {
            // Save the updated anchor data
            spatialAnchor.Save((success, result) =>
            {
                if (success)
                {
                    Debug.Log("Anchor updated and saved successfully.");
                    currentAnchorUuid = spatialAnchor.Uuid; // Ensure the UUID is updated
                    SaveAnchorUuid(); // Persist the UUID
                }
                else
                {
                    Debug.LogError($"Failed to save the anchor. Error: {result}");
                }
            });
        }
        else
        {
            Debug.LogWarning("SpatialAnchor is not localized. Skipping save.");
        }
    }


    public void DetachOVRSpatialAnchorComponent()
    {
        var spatialAnchor = targetPrefab.GetComponent<OVRSpatialAnchor>();
        if (spatialAnchor != null)
        {
            Destroy(spatialAnchor); // Detach the anchor
            Debug.Log("Anchor detached. Object can now be moved freely.");
        }
        else
        {
            Debug.LogWarning("No OVRSpatialAnchor component found to detach.");
        }
    }

    private void SaveAnchorUuid()
    {
        if (currentAnchorUuid != Guid.Empty)
        {
            PlayerPrefs.SetString("AnchorUuid", currentAnchorUuid.ToString());
            PlayerPrefs.Save();
            Debug.Log($"Anchor UUID {currentAnchorUuid} saved.");
        }
        else
        {
            Debug.LogWarning("Attempted to save an empty anchor UUID.");
        }
    }

    private void LoadAnchorUuid()
    {
        if (PlayerPrefs.HasKey("AnchorUuid"))
        {
            string savedUuid = PlayerPrefs.GetString("AnchorUuid");
            if (Guid.TryParse(savedUuid, out Guid uuid))
            {
                currentAnchorUuid = uuid;
                Debug.Log($"Loaded Anchor UUID: {currentAnchorUuid}");
            }
            else
            {
                Debug.LogWarning("Failed to parse saved Anchor UUID.");
            }
        }
        else
        {
            Debug.LogWarning("No saved Anchor UUID found.");
        }
    }

    public void DeleteAnchor()
    {
        PlayerPrefs.DeleteKey("AnchorUuid");
        if (spatialAnchorCore == null || currentAnchorUuid == Guid.Empty)
        {
            Debug.LogError("No anchor to delete!");
            return;
        }
        Debug.Log("Delete anchor!");

        spatialAnchorCore.EraseAnchorByUuid(currentAnchorUuid);
    }

    public void OnAnchorEraseCompleted(OVRSpatialAnchor erasedAnchor, OVRSpatialAnchor.OperationResult result)
    {
        if (result == OVRSpatialAnchor.OperationResult.Success)
        {
            Debug.Log("Anchor erased successfully.");
            currentAnchorUuid = Guid.Empty;
        }
        else
        {
            Debug.LogError("Failed to erase the anchor.");
        }
    }

    private IEnumerator TestAnchorFlow()
    {
        Debug.Log("Starting anchor test...");

        // Step 1: Load an existing anchor
        Debug.Log("Attempting to load existing anchor...");
        LoadExistingAnchor();
        LogPianoKeysTransform("After loading anchor");

        yield return new WaitForSeconds(2f); // Give some time to load

        // Step 2: If no anchor exists, create a new one
        if (PlayerPrefs.HasKey("AnchorUuid"))
        {
            Debug.Log("Existing anchor found.");
        }
        else
        {
            Debug.Log("No existing anchor found. Creating a new one...");
            UpdateAnchorFromUI();
            LogPianoKeysTransform("After creating new anchor");
            yield return new WaitForSeconds(2f);
        }

        // Step 3: Wait and update the pianoKeys position
        yield return new WaitForSeconds(3f);
        targetPrefab.transform.position += new Vector3(0.1f, 0, 0); // Slightly move pianoKeys
        Debug.Log("Updating anchor with new pianoKeys position...");
        UpdateAnchorFromUI();
        LogPianoKeysTransform("After updating anchor");

        yield return new WaitForSeconds(2f); // Allow time for saving

        // Step 4: Reload the anchor
        Debug.Log("Reloading anchor to verify persistence...");
        LoadExistingAnchor();
        yield return new WaitForSeconds(2f);
        LogPianoKeysTransform("After reloading anchor");

        Debug.Log("Anchor test completed.");
    }

    private void LogPianoKeysTransform(string context)
    {
        Debug.Log($"{context} - PianoKeys Position: {targetPrefab.transform.position}, Rotation: {targetPrefab.transform.rotation}");
    }
}
