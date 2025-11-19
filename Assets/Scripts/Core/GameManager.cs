using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeGame();

        if (mapGenerator != null)
        {
            Vector3 spawnPos = mapGenerator.GetPlayerSpawnPosition();
            transform.position = spawnPos;
            Debug.Log($"Player spawned at {spawnPos}");
        }
    }

    private void InitializeGame()
    {
        Debug.Log("Game initialized");
    }

    public MapGenerator GetMapGenerator()
    {
        return mapGenerator;
    }
}
