using UnityEngine;

public class Follower : MonoBehaviour
{
    public Transform target;

    void Start()
    {
        if (!target) return;
    }
    void LateUpdate()
    {
        if (!target) return;

        transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }
}
