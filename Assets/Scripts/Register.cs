using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
public class Register : MonoBehaviour
{
    public TMP_InputField nameField;
    public TMP_InputField passwordField;

    public Button submitButton;

    public void CallRegister() {
        StartCoroutine(RegisterCoroutine());
    }

    IEnumerator RegisterCoroutine() {
        WWWForm form = new WWWForm();
        form.AddField("name", nameField.text);
        form.AddField("password", passwordField.text);
        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost/sqlconnect/register.php", form))
        {
            yield return www.SendWebRequest(); // Send the request and wait for the response

            if (www.result != UnityWebRequest.Result.Success) // Check for errors
            {
                Debug.Log("User creation failed. Error: " + www.error);
            }
            else
            {
                if (www.downloadHandler.text == "0")
                {
                    Debug.Log("User created successfully!");
                    UnityEngine.SceneManagement.SceneManager.LoadScene(0);
                }
                else
                {
                    Debug.Log("User creation failed. Error number " + www.downloadHandler.text);
                }
            }
        } 
    }

    public void VerifyInputs() {
        // the submit button will not work if these conditions aren't met
        submitButton.interactable = (nameField.text.Length >= 6 && passwordField.text.Length >= 8);

    }
}
