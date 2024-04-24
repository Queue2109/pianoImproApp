using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
public class Login : MonoBehaviour
{
    public TMP_InputField nameField;
    public TMP_InputField passwordField;

    public Button submitButton;

    public void CallLogin() {
        StartCoroutine(LoginCoroutine());
    }

    IEnumerator LoginCoroutine() {
        WWWForm form = new WWWForm();
        form.AddField("name", nameField.text);
        form.AddField("password", passwordField.text);
        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost/sqlconnect/login.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success) {
                // Check the response text
                if (www.downloadHandler.text[0] == '0') {
                    DBManager.username = nameField.text;
                } else {
                    Debug.Log("User login failed; Error no " + www.downloadHandler.text);
                }
            } else if (www.result == UnityWebRequest.Result.ConnectionError || 
                    www.result == UnityWebRequest.Result.ProtocolError) {
                // Handle errors
                Debug.Log("Error: " + www.error);
            } else {
                // Handle other types of failures
                Debug.Log("Request failed: " + www.result);
            }

        } 
    }

    public void VerifyInputs() {
        // the submit button will not work if these conditions aren't met
        submitButton.interactable = (nameField.text.Length >= 6 && passwordField.text.Length >= 8);

    }
}
