using UnityEngine;

public class PlayerRotation : MonoBehaviour
{
    public Transform cameraTransform;

    void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        Vector3 cameraEuler = cameraTransform.eulerAngles;
        float pitch = cameraEuler.x;
        if (pitch > 180f)
            pitch -= 360f;

        pitch = Mathf.Clamp(pitch, -70f, 40f);

        transform.rotation = Quaternion.Euler(pitch, cameraEuler.y, 0f);
    }
}
