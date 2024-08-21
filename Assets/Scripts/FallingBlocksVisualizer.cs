using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using Melanchall.DryWetMidi.MusicTheory;
using System.Collections;
using Note = Melanchall.DryWetMidi.Interaction.Note;

public class FallingBlocksVisualizer : MonoBehaviour
{
    public GameObject fallingBlockPrefab;  // The prefab for the falling block
    public float fallDuration = 1.0f;      // Time for the block to fall to the key
    public Playback playback;
    private Dictionary<string, Transform> noteToKeyMapping;
    public GameObject pianoKeyboard;

    void Start()
    {
    }

    public void InitializeKeyMappings()
    {
        PersistentGameObject persistentGameObject = FindObjectOfType<PersistentGameObject>();
        if (persistentGameObject)
        {
            pianoKeyboard = persistentGameObject.gameObject;
        }
        else
        {
            Debug.Log("No PersistentGameObject found in the scene. in the 0");
        }
        noteToKeyMapping = new Dictionary<string, Transform>();
        for (int i = 0; i < pianoKeyboard.transform.childCount; i++)
        {
            Transform keyTransform = pianoKeyboard.transform.GetChild(i);
            noteToKeyMapping.Add(pianoKeyboard.transform.GetChild(i).name, keyTransform);
        }
    }


    public void ScheduleFallingBlock(string note)
    {

        if (noteToKeyMapping.TryGetValue(note, out Transform keyTransform))
        {

            // You might want to change the delay logic or simply skip it
            SpawnFallingBlock(keyTransform.position, 3); // Using 0 delay for simplicity
        }
        else
        {
            Debug.LogWarning($"No key mapping found for note: {note}");
        }
    }

    private void SpawnFallingBlock(Vector3 targetPosition, double delay)
    {
        // Instantiate the block slightly above the screen or keys
        Vector3 spawnPosition = new Vector3(targetPosition.x, targetPosition.y + 1, targetPosition.z);

        GameObject block = Instantiate(fallingBlockPrefab, spawnPosition, Quaternion.identity);
        // Start the block falling
        StartCoroutine(FallToPosition(block, targetPosition, delay));
    }

    private IEnumerator FallToPosition(GameObject block, Vector3 targetPosition, double delay)
    {
        float timeElapsed = 0;

        Vector3 startPosition = block.transform.position;

        while (timeElapsed < fallDuration)
        {
            // Interpolate between the start and target positions over the fall duration
            block.transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / fallDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the block lands exactly on the key position
        block.transform.position = targetPosition;

        // Optionally, destroy the block after it lands
        Destroy(block);
    }
}
