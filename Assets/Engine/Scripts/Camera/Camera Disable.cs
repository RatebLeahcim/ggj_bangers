using UnityEngine;

public class CameraDisable : MonoBehaviour
{
    public Camera backupCamera;
    public void DisableCamera()
    {
        backupCamera.enabled = false;
    }

    public void EnableCamera()
    {
        backupCamera.enabled = true;
    }
}
