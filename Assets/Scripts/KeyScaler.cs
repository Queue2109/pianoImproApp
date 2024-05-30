using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeyScaler : MonoBehaviour
{
    private List<Transform> blackKeys; // Array to hold all the black keys
    private List<Transform> whiteKeys; // Array to hold all the black keys
    private GameObject pianoKeyboard;
    public float value;


    private void Start()
    {
        pianoKeyboard = GameObject.FindGameObjectWithTag("Piano");
        blackKeys = pianoKeyboard.GetComponentsInChildren<Transform>().Where(child => child.name.Contains("Sharp")).ToList();
        blackKeys = blackKeys.OrderBy(key => key.position.x).ToList();
        whiteKeys = pianoKeyboard.GetComponentsInChildren<Transform>().Where(child => !child.name.Contains("Sharp")).ToList();
        whiteKeys = whiteKeys.OrderBy(key => key.position.x).ToList();
    }
    public void ScaleBlackKeysHeight(float value)
    {
        float scaleFactor = (float)value;
        foreach (Transform key in blackKeys)
        {

            Vector3 localScale = key.localScale;

            float newHeight = localScale.y * scaleFactor;

            key.localScale = new Vector3(localScale.x, newHeight, localScale.z);

            float ratioZtoY = 0.08f / 14.01483f;

            float changeInHeight = newHeight - localScale.y;

            float newZPosition = key.localPosition.z + (changeInHeight * ratioZtoY);
            key.localPosition = new Vector3(key.localPosition.x, key.localPosition.y, newZPosition);
        }
    }

    public void ScaleBlackKeysWidth(float value)
    {
        if (value <= 0)
        {
            return;
        }

        for(int i = 1; i  < blackKeys.Count; i++)
        {
            Vector3 localScale = blackKeys[i].localScale;
            float newWidth = localScale.x * value;
            blackKeys[i].localScale = new Vector3(newWidth, localScale.y, localScale.z); // Optionally scale z as needed

        }
    }


    public void ScaleWhiteKeysHeight(float value)
    {
        float scaleFactor = value;
        Debug.Log(scaleFactor);
        foreach (Transform key in whiteKeys)
        {

            Vector3 localScale = key.localScale;
            if (localScale.y < 0)
            {
                return;
            }

            float newHeight = localScale.y * scaleFactor;

            key.localScale = new Vector3(localScale.x, newHeight, localScale.z);

            float ratioZtoY = 0.08f / 14.01483f;

            float changeInHeight = newHeight - localScale.y;

            float newZPosition = key.localPosition.z + (changeInHeight * ratioZtoY);
            key.localPosition = new Vector3(key.localPosition.x, key.localPosition.y, newZPosition);
        }
    }


    public void ScaleWhiteKeysWidth(float value)
    {
        float xScaleFactor = value - 1; // Calculate the difference from the original scale
        float moveKey = 0; // Initialize moveKey to track cumulative movement
        int blackKeyCount = 0;
        for (int i = 0; i < whiteKeys.Count; i++)
        {
            // Scale each key
            Vector3 scale = whiteKeys[i].localScale;
            whiteKeys[i].localScale = new Vector3(scale.x * value, scale.y, scale.z);

            // Adjust position to maintain spacing
            if (i > 0) // Skip the first key
            {
                // Calculate movement for this step
                float stepMove = scale.x / 50 * xScaleFactor + (scale.x/100 * value) / 3; // Adjust to half of the increase per key to maintain even spacing
                moveKey += stepMove;

                // Apply movement
                whiteKeys[i].localPosition += new Vector3(moveKey, 0, 0);

                // Adjust black keys if necessary
                if (blackKeys.Count > blackKeyCount && whiteKeys[i].name[0] == blackKeys[blackKeyCount].name[0])
                {
                    blackKeys[blackKeyCount].localPosition += new Vector3(moveKey, 0, 0);
                    blackKeyCount++;
                }
            }
        }
    }

}
