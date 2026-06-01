using UnityEngine;

public class PlayerRotation : MonoBehaviour
{
    public Transform cameraTransform;

    void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        Vector3 cameraEuler = cameraTransform.eulerAngles;
        transform.rotation = Quaternion.Euler(cameraEuler.x, cameraEuler.y, 0f);
    }
}
