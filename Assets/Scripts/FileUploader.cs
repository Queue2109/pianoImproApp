using UnityEngine;

public class FileUploader : MonoBehaviour
{
    private AndroidJavaObject activity;

    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }
    }

    public void OnUploadButtonClick()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            activity.Call("startActivity", new AndroidJavaObject("android.content.Intent", "com.example.filepickerlibrary.FilePickerActivity"));
        }
        else
        {
            Debug.Log("File upload is not supported on this platform."); 
        }
    }

    public void OnFileSelected(string filePath)
    {
        Debug.Log("File selected: " + filePath);
        // Handle the file path received from the Android activity
    }
}
