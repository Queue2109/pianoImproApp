using UnityEngine;

public class PersistentGameObject : MonoBehaviour
{
    private static PersistentGameObject instance;

    void Awake()
    {
        // Check if an instance of this object already exists
        if (instance == null)
        {
            // If not, set this as the instance, mark it as persistent, and initialize it
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            LoadState();
        }
        else
        {
            // If an instance already exists, destroy this one to prevent duplicates
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        SaveState();
    }

    private void OnDisable()
    {
        SaveState();
    }

    public void SaveState()
    {
        // Save position, rotation, and scale of the object
        SaveTransform("Object", transform);

        // Save the state of children if any
        for (int i = 0; i < transform.childCount; i++)
        {
            SaveTransform("Object_Child" + i, transform.GetChild(i));
        }

        PlayerPrefs.Save();
    }

    public void LoadState()
    {
        // Load position, rotation, and scale of the object
        LoadTransform("Object", transform);

        // Load the state of children if any
        for (int i = 0; i < transform.childCount; i++)
        {
            LoadTransform("Object_Child" + i, transform.GetChild(i));
        }
    }

    private void SaveTransform(string prefix, Transform t)
    {
        PlayerPrefs.SetFloat(prefix + "PosX", t.position.x);
        PlayerPrefs.SetFloat(prefix + "PosY", t.position.y);
        PlayerPrefs.SetFloat(prefix + "PosZ", t.position.z);

        PlayerPrefs.SetFloat(prefix + "RotX", t.eulerAngles.x);
        PlayerPrefs.SetFloat(prefix + "RotY", t.eulerAngles.y);
        PlayerPrefs.SetFloat(prefix + "RotZ", t.eulerAngles.z);

        PlayerPrefs.SetFloat(prefix + "ScaleX", t.localScale.x);
        PlayerPrefs.SetFloat(prefix + "ScaleY", t.localScale.y);
        PlayerPrefs.SetFloat(prefix + "ScaleZ", t.localScale.z);
    }

    private void LoadTransform(string prefix, Transform t)
    {
        if (PlayerPrefs.HasKey(prefix + "PosX"))
        {
            float posX = PlayerPrefs.GetFloat(prefix + "PosX");
            float posY = PlayerPrefs.GetFloat(prefix + "PosY");
            float posZ = PlayerPrefs.GetFloat(prefix + "PosZ");

            float rotX = PlayerPrefs.GetFloat(prefix + "RotX");
            float rotY = PlayerPrefs.GetFloat(prefix + "RotY");
            float rotZ = PlayerPrefs.GetFloat(prefix + "RotZ");

            float scaleX = PlayerPrefs.GetFloat(prefix + "ScaleX");
            float scaleY = PlayerPrefs.GetFloat(prefix + "ScaleY");
            float scaleZ = PlayerPrefs.GetFloat(prefix + "ScaleZ");

            t.position = new Vector3(posX, posY, posZ);
            t.eulerAngles = new Vector3(rotX, rotY, rotZ);
            t.localScale = new Vector3(scaleX, scaleY, scaleZ);
        }
    }
}
