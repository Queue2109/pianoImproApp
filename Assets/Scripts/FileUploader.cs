#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class FileUploader : MonoBehaviour
{

    private void Start()
    {
    }

    public void OnUploadButtonClick()
    {
        #if UNITY_EDITOR
                string path = EditorUtility.OpenFilePanel("Upload MIDI File", "", "midi");
                if (!string.IsNullOrEmpty(path))
                {
                    UploadFile(path);
                }
        #elif UNITY_STANDALONE_WIN
                string path = OpenFileDialog();
                if (!string.IsNullOrEmpty(path))
                {
                    UploadFile(path);
                }
        #else
                Debug.Log("File upload is not supported on this platform.");
        #endif
    }

    private string OpenFileDialog()
    {
        // This method should open a file dialog and return the selected file path.
        // Implement this based on the specific requirements and platform.

        return string.Empty; // Placeholder for actual implementation
    }

    private void UploadFile(string filePath)
    {
        string destinationPath = Path.Combine(Application.persistentDataPath, Path.GetFileName(filePath));
        File.Copy(filePath, destinationPath, true);
        Debug.Log($"File uploaded successfully: {destinationPath}");
    }
}
