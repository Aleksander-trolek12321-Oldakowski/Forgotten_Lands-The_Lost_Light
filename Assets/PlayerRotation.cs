using UnityEngine;

public class PlayerRotation : MonoBehaviour
{
    public Transform cameraTransform;

    void LateUpdate()
    {
        Vector3 cameraEuler = cameraTransform.eulerAngles;

        transform.rotation = Quaternion.Euler(0f, cameraEuler.y, 0f);
    }
}