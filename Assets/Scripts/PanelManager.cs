using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
    public List<GameObject> panels; // List to hold all your panels

    public int currentPanelIndex = 0; // To keep track of the current panel

    void Start()
    {
        SetPanelActive();
        panels[0].SetActive(true); // Set the first panel active
    }

    void SetPanelActive()
    {
        Debug.Log("Panel index" + currentPanelIndex);
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(i == currentPanelIndex);
        }
    }

    public void NextPanel()
    {
        if (currentPanelIndex < panels.Count - 1)
        {
            currentPanelIndex++;
            SetPanelActive();
        }
    }
    public void PreviousPanel()
    {
        if (currentPanelIndex > 0)
        {
            currentPanelIndex--;
            SetPanelActive();
        }
    }

    public void SetPanel(int index)
    {
        currentPanelIndex = index;
        SetPanelActive();
    }
}
