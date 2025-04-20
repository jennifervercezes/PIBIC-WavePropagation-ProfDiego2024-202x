using System.Collections;
using UnityEngine;
public class WavebyGrid : MonoBehaviour
{
    [Header("Simulation Grid")]
    public int width = 100;
    public int height = 100;
    public float spacing = 0.1f;

    [Header("Wave Settings")]
    public float c = 1f;
    public float deltat = 0.01f;
    public float deltax = 0.1f;

    [Header("Time Steps - Update Interval")]
    public float stepTime = 0.01f;

    private GameObject[,] grid;
    private float[,] fAnterior;
    private float[,] fAtual;
    private float[,] fFuturo;

    private float dt2dx2;

    public GameObject pointPrefab; 
    //A small quad mesh prefab to simulate a dot in the grid

    void Start()
    {
        grid = new GameObject[width, height];
        fAnterior = new float[width, height];
        fAtual = new float[width, height];
        fFuturo = new float[width, height];

        dt2dx2 = Mathf.Pow(c * deltat / deltax, 2f); 
        // c^2 * (dt^2 / dx^2)

        GenerateGrid();
        InitializeWave();

        StartCoroutine(UpdateGrid());
    }

    void GenerateGrid()
    // Create a grid of GameObjects to represent the wave points
    // Each point is a small quad that will be instantiated in the scene
    {
        Vector3 origin = transform.position;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector3 pos = origin + new Vector3(i * spacing, j * spacing, 0f);
                GameObject point = Instantiate(pointPrefab, pos, Quaternion.identity, transform);
                point.transform.localScale = Vector3.one * spacing * 0.9f; // um pouco menor que o espaçamento
                grid[i, j] = point;
            }
        }
    }

    void InitializeWave()
    // Initialize the wave with a Gaussian distribution centered in the grid
    {
        int cx = width / 2;
        int cy = height / 2;

        float sigma = 5f;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float dx = i - cx;
                float dy = j - cy;
                float valor = Mathf.Exp(-(dx * dx + dy * dy) / sigma);
                fAtual[i, j] = valor;
                fAnterior[i, j] = valor;
            }
        }
    }

    IEnumerator UpdateGrid()
    // Update the wave simulation in a coroutine to allow for frame updates
    {
        while (true)
        {
            for (int i = 1; i < width - 1; i++)
            {
                for (int j = 1; j < height - 1; j++)
                {
                    fFuturo[i, j] = 2f * fAtual[i, j] - fAnterior[i, j] + dt2dx2 * (
                        fAtual[i + 1, j] + fAtual[i - 1, j] +
                        fAtual[i, j + 1] + fAtual[i, j - 1] -
                        4f * fAtual[i, j]
                    );
                }
            }

            // Update the colors of the grid based on the wave values
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    float intensity = Mathf.Clamp01(Mathf.Abs(fAtual[i, j]));
                    Color color = Color.Lerp(Color.white, Color.black, intensity);
                    grid[i, j].GetComponent<SpriteRenderer>().color = color;
                }
            }

            // Waits to update the next frame
            yield return new WaitForSeconds(stepTime);

            // Swap arrays
            var temp = fAnterior;
            fAnterior = fAtual;
            fAtual = fFuturo;
            fFuturo = temp;
        }
    }
}

