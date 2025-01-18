using UnityEngine;
using System;
using System.Collections.Generic;
using Meta.XR.BuildingBlocks;

public class SpatialAnchorManager : MonoBehaviour
{
    public GameObject targetPrefab; // The prefab with which we will work
    public SpatialAnchorCoreBuildingBlock spatialAnchorCore;

    private Guid currentAnchorUuid; // To track the existing anchor's UUID

    private void Start()
    {
        if (spatialAnchorCore == null)
        {
            Debug.LogError("SpatialAnchorCoreBuildingBlock not found!");
            return;
        }

        // Attempt to load any existing anchors
        LoadExistingAnchor();
    }

    private void LoadExistingAnchor()
    {
        if (currentAnchorUuid != Guid.Empty)
        {
            List<Guid> anchorUuids = new List<Guid> { currentAnchorUuid };
            spatialAnchorCore.LoadAndInstantiateAnchors(targetPrefab, anchorUuids);
        }
        else
        {
            Debug.LogWarning("No valid anchor UUID to load.");
        }
    }

    private void OnAnchorLoadCompleted(List<OVRSpatialAnchor> loadedAnchors)
    {
        if (loadedAnchors.Count > 0)
        {
            Debug.Log("Anchor loaded successfully.");
            OVRSpatialAnchor loadedAnchor = loadedAnchors[0];

            // Update the current anchor UUID
            currentAnchorUuid = loadedAnchor.Uuid;

            // Transfer the anchor to the original GameObject
            if (targetPrefab != null)
            {
                // Detach the anchor from the clone and attach it to the original
                loadedAnchor.transform.SetParent(targetPrefab.transform, false);
                loadedAnchor.transform.localPosition = Vector3.zero;
                loadedAnchor.transform.localRotation = Quaternion.identity;

                Debug.Log("Anchor successfully attached to the original prefab.");
            }
            else
            {
                Debug.LogError("Original prefab not found.");
            }
        }
        else
        {
            Debug.LogWarning("No anchors found to load.");
        }
    }

    public void InstantiateNewAnchor()
    {
        if (spatialAnchorCore == null)
        {
            Debug.LogError("SpatialAnchorCoreBuildingBlock is not initialized!");
            return;
        }

        if (currentAnchorUuid != Guid.Empty)
        {
            spatialAnchorCore.EraseAnchorByUuid(currentAnchorUuid);
            Debug.Log("Existing anchor erased to allow reanchoring.");
        }

        // Instantiate a new anchor at the prefab's position and rotation
        spatialAnchorCore.InstantiateSpatialAnchor(targetPrefab, targetPrefab.transform.position, targetPrefab.transform.rotation);
    }

    public void OnAnchorCreateCompleted(OVRSpatialAnchor newAnchor, OVRSpatialAnchor.OperationResult result)
    {
        if (result == OVRSpatialAnchor.OperationResult.Success)
        {
            Debug.Log("Anchor created successfully.");
            currentAnchorUuid = newAnchor.Uuid;
        }
        else
        {
            Debug.LogError($"Failed to create a new anchor. Result: {result}");
        }
    }

    public void UpdateAnchorFromUI()
    {
        Vector3 newAnchorPosition = targetPrefab.transform.position;
        Quaternion newAnchorRotation = targetPrefab.transform.rotation;

        UpdateAnchor(newAnchorPosition, newAnchorRotation);
    }

    public void UpdateAnchor(Vector3 newPosition, Quaternion newRotation)
    {
        var spatialAnchor = targetPrefab.GetComponent<OVRSpatialAnchor>();

        if (spatialAnchor == null)
        {
            Debug.LogWarning("SpatialAnchor is missing. Creating a new anchor.");
            InstantiateNewAnchor();
            return;
        }

        if (!spatialAnchor.enabled)
        {
            spatialAnchor.enabled = true;
            Debug.Log("SpatialAnchor was disabled. It has been enabled.");
        }

        targetPrefab.transform.position = newPosition;
        targetPrefab.transform.rotation = newRotation;
    }


    public void DeleteAnchor()
    {
        if (spatialAnchorCore == null || currentAnchorUuid == Guid.Empty)
        {
            Debug.LogError("No anchor to delete!");
            return;
        }

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

    public void DetachOVRSpatialAnchorComponent()
    {
        var spatialAnchor = targetPrefab.GetComponent<OVRSpatialAnchor>();
        if (spatialAnchor != null && spatialAnchor.enabled)
        {
            Destroy(spatialAnchor); // Detach the anchor
            Debug.Log("Anchor detached. Object can now be moved.");
        }
        else
        {
            Debug.LogWarning("No active SpatialAnchor component to detach.");
        }
    }
}
