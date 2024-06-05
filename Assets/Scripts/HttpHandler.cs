using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using System.Text;
using System;
using TMPro;

public class HttpHandler : MonoBehaviour
{
    // Start is called before the first frame update

    string url = "http://localhost:5000/analyze";
    TMP_Text text;
    void Start()
    {
        text = GameObject.Find("ChordName").GetComponent<TMP_Text>();
        
    }
    public void getChordName(List<int> notes)
    {
        StartCoroutine(PostRequest(notes));
    }
    IEnumerator PostRequest(List<int> notes)
    {
        string notesToJson = JsonUtility.ToJson(new NotesData { notes = notes.ToArray() });
        Debug.Log(notesToJson);

        using UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
        webRequest.SetRequestHeader("Content-Type", "application/json");
        byte[] bytes = Encoding.UTF8.GetBytes(notesToJson);
        Debug.Log(bytes); 
        webRequest.uploadHandler = new UploadHandlerRaw(bytes);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + webRequest.error);
        }
        else
        {
            string responseText = webRequest.downloadHandler.text;

            var responseData = JsonUtility.FromJson<ResponseData>(responseText);
            Debug.Log("Result: " + responseData.result);
            text.text = responseData.result;

        }

    }

    [System.Serializable]
    public class ResponseData
    {
        public string result;
    }

    [System.Serializable]
    public class NotesData
    {
        public int[] notes;
    }

}
