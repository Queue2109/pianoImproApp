using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetPianoPositionDialogHandler : MonoBehaviour
{
    [SerializeField] private NewAnchorManager anchorManager;
    [SerializeField] private PianoFunctions pianoFunctions;
    [SerializeField] private GameObject resetPianoPositionDialog;
    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject pianoKeyboard;
    [SerializeField] private RelativeTransformSaver relativeTransformSaverUI;
    [SerializeField] private RelativeTransformSaver relativeTransformSaverPlayControlPanel;
    [SerializeField] private GameObject playControlPanel;
    [SerializeField] private GameObject pianoSettingsButton;
    [SerializeField] private GameObject pianoSettingsPanel;
    [SerializeField] private MidiFileNoteReader midiFileNoteReader;
    [SerializeField] private GameObject handGrabInteraction;

    bool isUIActive = false;
    bool isPlayControlPanelActive = false;

    public void OnBunnyPoseDetected()
    {
        resetPianoPositionDialog.SetActive(true);
        isUIActive = UI.activeSelf;
        UI.SetActive(false);
        isPlayControlPanelActive = playControlPanel.activeSelf;
        playControlPanel.SetActive(false);

        if (pianoSettingsPanel.activeSelf)
        {
            anchorManager.OnSaveButtonCLicked();
            pianoSettingsPanel.SetActive(false);
            pianoSettingsButton.SetActive(true);
        }

        if (midiFileNoteReader.isPlaying)
        {
            midiFileNoteReader.TogglePlayPause();
        }

    }

    public void OnConfirmButtonPressed()
    {
        Debug.Log("Pressed confirm");

        anchorManager.EraseAndCreateAnchor();

        resetPianoPositionDialog.SetActive(false);
        relativeTransformSaverUI.UpdatePanelPositionFromPrefs();
        relativeTransformSaverPlayControlPanel.UpdatePanelPositionFromPrefs();
        UI.SetActive(isUIActive);
        playControlPanel.SetActive(isPlayControlPanelActive);
        handGrabInteraction.SetActive(false);
        
    }

    public void OnCancelButtonPressed()
    {
        Debug.Log("Pressed cancel");    
        resetPianoPositionDialog.SetActive(false);
        UI.SetActive(isUIActive);
        playControlPanel.SetActive(isPlayControlPanelActive);
    }
}
