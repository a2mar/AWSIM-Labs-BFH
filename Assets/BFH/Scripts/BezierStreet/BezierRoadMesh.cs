// Creates a simple road segment mesh based on sampled cubic Bezier curves.
// 
// TODO: 
// - create collider
// - make sampling rate of material dynamic (adjust with curve length)
// 
// Author: Ammar Hammad

using UnityEngine;

public class BezierRoadMesh : MonoBehaviour
{
    // material for generative road mesh. can be swapped in inspector, but default will be loaded in this script
    public Material roadMaterial;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private BoxCollider boxCollider;
    public void GenerateRoadMesh(Vector3[] leftPoints, Vector3[] rightPoints)
    {
        if (leftPoints == null || rightPoints == null || leftPoints.Length != rightPoints.Length)
        {
            Debug.LogError("Left and Right points must have the same number of elements!");
            return;
        }

        AssignMeshComponents();

        if (meshFilter.sharedMesh != null)
        {
            DestroyImmediate(meshFilter.sharedMesh);
        }

        int numVerts = leftPoints.Length * 2;
        Vector3[] vertices = new Vector3[numVerts];
        Vector2[] uvs = new Vector2[numVerts];
        int[] triangles = new int[(leftPoints.Length - 1) * 6];

        // define repeats:
        float textureRepeatsY = 10.0f;

        for (int i = 0; i < leftPoints.Length; i++)
        {
            int vertIndex = i * 2;
            vertices[vertIndex] = leftPoints[i];
            vertices[vertIndex + 1] = rightPoints[i];

            float uvY = i / (float)(leftPoints.Length - 1) * textureRepeatsY;
            uvs[vertIndex] = new Vector2(0, uvY);
            uvs[vertIndex + 1] = new Vector2(1, uvY);
        }

        int triIndex = 0;
        for (int i = 0; i < leftPoints.Length - 1; i++)
        {
            int vertIndex = i * 2;
            triangles[triIndex] = vertIndex;
            triangles[triIndex + 1] = vertIndex + 2;
            triangles[triIndex + 2] = vertIndex + 1;

            triangles[triIndex + 3] = vertIndex + 1;
            triangles[triIndex + 4] = vertIndex + 2;
            triangles[triIndex + 5] = vertIndex + 3;

            triIndex += 6;
        }

        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        CreateBoundingBox(vertices);
    }


    void AssignMeshComponents()
    {
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;
        // automatically search for "Road-Realistic.mat" in Resources
        if (roadMaterial == null)
        {
            roadMaterial = Resources.Load<Material>("Materials/BezierRoad/Road-Realistic");

            if (roadMaterial == null)
            {
                Debug.LogError("Road.mat not found in Resources folder! Assign a material manually.");
            }
        }

        if (roadMaterial != null)
        {
            meshRenderer.material = roadMaterial;
        }
    }

        void CreateBoundingBox(Vector3[] vertices)
    {
        if (vertices == null || vertices.Length == 0) return;

        Vector3 min = vertices[0];
        Vector3 max = vertices[0];

        foreach (Vector3 v in vertices)
        {
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }

        Vector3 center = (min + max) / 2f;
        Vector3 size = max - min;

        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        boxCollider.center = this.transform.InverseTransformPoint(center);
        boxCollider.size = size;
    }
}