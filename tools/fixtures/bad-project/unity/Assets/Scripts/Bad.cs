using UnityEngine;

public class Bad : MonoBehaviour
{
    void Start()
    {
        var id = SystemInfo.deviceUniqueIdentifier;
        Debug.Log(id);
    }
}
