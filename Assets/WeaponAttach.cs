using UnityEngine;

public class WeaponAttach : MonoBehaviour
{
    [Header("Rig rąk (root obiektu z animacją)")]
    public Transform armsRig;

    [Header("Rig / obiekt broni")]
    public Transform weaponRig;

    [Header("Nazwy kości")]
    public string handBoneName = "sticky_hand_R";   
    public string weaponBoneName = "sword_bone";    

    [Header("Offset")]
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    void Start()
    {
        AttachWeaponToHand();
    }

    void AttachWeaponToHand()
    {
        Transform handBone = FindChildRecursive(armsRig, handBoneName);

        if (handBone == null)
        {
            Debug.LogError("Nie znaleziono kości ręki: " + handBoneName);
            return;
        }

        Transform weaponBone = FindChildRecursive(weaponRig, weaponBoneName);

        // jeśli nie znajdzie bone w broni → używa roota
        Transform attachTarget = (weaponBone != null) ? weaponBone : weaponRig;

        // 🔥 NAJWAŻNIEJSZE
        weaponRig.SetParent(handBone);

        weaponRig.localPosition = positionOffset;
        weaponRig.localRotation = Quaternion.Euler(rotationOffset);
        weaponRig.localScale = Vector3.one;

        Debug.Log("Broń przypięta do: " + handBone.name);
    }

    Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }
}