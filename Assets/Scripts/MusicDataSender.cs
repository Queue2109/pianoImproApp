using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using UnityEngine.UIElements;

public class MusicDataSender : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(SendMusicData());
    }

    IEnumerator SendMusicData()
    {
        string url = "http://localhost:5000/analyze";
        string jsonData = "{\"notes\": [\"C4\",\"G4\",\"E-5\"]}";  // Correct JSON format

        using UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
        webRequest.SetRequestHeader("Content-Type", "application/json");
        byte[] bytes = Encoding.UTF8.GetBytes(jsonData);
        webRequest.uploadHandler = new UploadHandlerRaw(bytes);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        Debug.Log("Pred yieldom");
        yield return webRequest.SendWebRequest();

        switch (webRequest.result) {
            case UnityWebRequest.Result.InProgress:
                Debug.Log("In progress");
                Debug.Log(webRequest.downloadProgress);
                break;
            case UnityWebRequest.Result.Success:
                Debug.Log("Yayyyy" + webRequest.downloadHandler.text);
                break;
            default:
                Debug.Log("Error: " + webRequest.error);
                break;
        
        }
        yield break;

        //UnityWebRequest www = new UnityWebRequest(url, "POST");
        //byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);
        //www.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        //www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        //www.SetRequestHeader("Content-Type", "application/json");

        //Debug.Log("Sending request...");

        //www.timeout = 10; // Timeout in seconds

        //yield return www.SendWebRequest();

        //Debug.Log("Request sent, awaiting response...");

        //if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        //{
        //    Debug.LogError("Request Failed: " + www.error);
        //}
        //else
        //{
        //    Debug.Log("Response Code: " + www.responseCode);
        //    Debug.Log("Response: " + www.downloadHandler.text);
        //    Debug.Log("Form upload complete!");
        //}
    }
}
