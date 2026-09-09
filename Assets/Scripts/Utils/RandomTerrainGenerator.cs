using UnityEngine;

public class RandomTerrainGenerator : MonoBehaviour
{
    private Terrain _terrain;
    [SerializeField] private int _width = 256;
    [SerializeField] private int _height = 256;
    [SerializeField] private float _depth = 20f;
    [SerializeField] private float _scale = 20f;

    void Start()
    {
        _terrain = GetComponent<Terrain>();

        TerrainData terrainData = _terrain.terrainData;

        terrainData.heightmapResolution = _width + 1;
        terrainData.size = new Vector3(_width, _depth, _height);

        terrainData.SetHeights(0, 0, GenerateHeights(terrainData.heightmapResolution));
    }

    float[,] GenerateHeights(int resolution)
    {
        float[,] heights = new float[resolution, resolution];

        float offsetX = Random.Range(0f, 9999f);
        float offsetY = Random.Range(0f, 9999f);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float xCoord = (float)x / (resolution - 1) * _scale + offsetX;
                float yCoord = (float)y / (resolution - 1) * _scale + offsetY;

                heights[y, x] = Mathf.PerlinNoise(xCoord, yCoord);
            }
        }

        return heights;
    }
}
