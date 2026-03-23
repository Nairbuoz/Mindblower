using UnityEngine;
using UnityEngine.SceneManagement;

public class ShortkeyHacker : MonoBehaviour
{
  // Update is called once per frame
  void Update()
  {
    if (Input.GetKeyDown(KeyCode.Q))
    {
      SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
  }
}
