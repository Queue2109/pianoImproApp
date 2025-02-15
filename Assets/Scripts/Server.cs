using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Server : MonoBehaviour
{
    private Process flaskProcess;
    private string pythonExecutable = "python";  // Change to "python3" if needed
    private string scriptPath;

    void Start()
    {
        // Set script path, assuming it's inside StreamingAssets
        scriptPath = Path.Combine(Application.streamingAssetsPath, "serverFlask.py");

        StartFlaskServer();
        StartCoroutine(TestServer());
    }

    void StartFlaskServer()
    {
        flaskProcess = new Process();
        flaskProcess.StartInfo.FileName = pythonExecutable;
        flaskProcess.StartInfo.Arguments = $"\"{scriptPath}\"";  // Ensure correct path formatting
        flaskProcess.StartInfo.RedirectStandardOutput = true;
        flaskProcess.StartInfo.RedirectStandardError = true;
        flaskProcess.StartInfo.UseShellExecute = false;
        flaskProcess.StartInfo.CreateNoWindow = true;

        flaskProcess.Start();
        flaskProcess.BeginOutputReadLine();
        flaskProcess.BeginErrorReadLine();
    }

    void OnApplicationQuit()
    {
        if (flaskProcess != null && !flaskProcess.HasExited)
        {
            flaskProcess.Kill();
            flaskProcess.Dispose();
        }
    }

    IEnumerator TestServer()
    {
        yield return new WaitForSeconds(3); // Give Flask time to start

        UnityWebRequest request = UnityWebRequest.Post("http://127.0.0.1:5000/analyze", "{\"notes\": [\"C4\", \"E4\", \"G4\"]}", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.Log("Server response: " + request.downloadHandler.text);
        }
        else
        {
            UnityEngine.Debug.LogError("Error contacting server: " + request.error);
        }
    }
}
