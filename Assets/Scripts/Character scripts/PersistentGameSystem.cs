using UnityEngine;

public class PersistentGameSystems : MonoBehaviour
{
    public static PersistentGameSystems Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }
}