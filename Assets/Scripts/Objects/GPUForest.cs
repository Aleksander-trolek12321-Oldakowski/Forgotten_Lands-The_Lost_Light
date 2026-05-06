using UnityEngine;

public class GPUForest : MonoBehaviour
{
    public Mesh treeMesh;
    public Material treeMaterial;
    public int count = 1023;

    Matrix4x4[] matrices;

    void Start()
    {
        matrices = new Matrix4x4[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-200, 200),
                0,
                Random.Range(-200, 200)
            );

            matrices[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
        }
    }

    void Update()
    {
        Graphics.DrawMeshInstanced(treeMesh, 0, treeMaterial, matrices);
    }
}
