using UnityEngine;

/// <summary>
/// Minimal singleton MonoBehaviour used to start coroutines from code that may
/// run on inactive GameObjects. A single GameObject is created on demand and
/// marked DontDestroyOnLoad so it persists.
/// </summary>
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;
    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[CoroutineRunner]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<CoroutineRunner>();
            }
            return _instance;
        }
    }
}
