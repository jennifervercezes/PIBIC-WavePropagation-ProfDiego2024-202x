using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WavebyMesh : MonoBehaviour
{
    [Header("Simulation Grid")]
    public int resolution = 128;
    public float spacing = 0.1f;

    [Header("Wave Settings")]
    public float c = 1f;
    //public float deltat = 0.01f;
    //public float deltax = 0.1f;
    public float waveSpeed = 2f;
    public float wavelength = 1f;

    [Header("Time Steps - Update Interval")]
    //public float stepTime = 0.01f;
    public float amplitude = 1f;
    public int waveCount = 3;

    [Header("Color Settings")]
    public Gradient thermalGradient;

    private Mesh mesh;
    private Vector3[] vertices,
                fAnterior, fAtual, fFuturo;

    private float dt2dx2;
    private Color[] colors;
    private float[] intensities;

    void Start()
    {
        GenerateMesh();
    }

    void Update()
    {
        InitializeWave();
        UpdateMesh();
    }

    void GenerateMesh()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        int vertCount = resolution * resolution;
        vertices = new Vector3[vertCount];
        colors = new Color[vertCount];
        intensities = new float[vertCount];

        int[] indices = new int[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        int index = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float xpos = x * spacing;
                float ypos = y * spacing;
                vertices[index] = new Vector3(xpos, ypos, 0);
                indices[index] = index;
                uvs[index] = new Vector2((float)x / resolution, (float)y / resolution);
                index++;
            }
        }

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
    }

    void InitializeWave()
    {
        float time = Time.time;
        Vector2 center = new Vector2((resolution * spacing) / 2f, (resolution * spacing) / 2f);
        float angleStep = 360f / waveCount;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vert = vertices[i];
            float total = 0f;

            for (int w = 0; w < waveCount; w++)
            {
                float angle = angleStep * w * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2f;
                Vector2 source = center + offset;

                float dist = Vector2.Distance(new Vector2(vert.x, vert.y), source);
                float wave = Mathf.Sin((2 * Mathf.PI * dist / wavelength) - (waveSpeed * time));
                total += wave;
            }

            float intensity = Mathf.Clamp01((total / waveCount) * amplitude * 0.5f + 0.5f);
            intensities[i] = intensity;
        }
    }

    void UpdateMesh()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            colors[i] = thermalGradient.Evaluate(intensities[i]);
        }

        mesh.colors = colors;
    }
}
