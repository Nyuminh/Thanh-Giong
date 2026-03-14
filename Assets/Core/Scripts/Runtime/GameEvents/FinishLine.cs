using UnityEngine;

public class FinishLine : MonoBehaviour
{
    [Header("Setup")]
    public GameObject victoryUI;      // Kéo cái Panel chúc mừng vào đây
    public AudioSource victorySound; // Kéo Audio Source vào đây

    private bool isFinished = false;

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem có đúng là Player nhảy vào không
        if (other.CompareTag("Player") && !isFinished)
        {
            isFinished = true;
            CompleteGame();
        }
    }

    void CompleteGame()
    {
        Debug.Log("Game Finished!");

        // 1. Hiện UI chúc mừng
        if (victoryUI != null) victoryUI.SetActive(true);

        // 2. Phát nhạc
        if (victorySound != null) victorySound.Play();

        // 3. (Tùy chọn) Dừng thời gian hoặc vô hiệu hóa điều khiển nhân vật
        // Time.timeScale = 0f; 
        // Cursor.lockState = CursorLockMode.None;
    }
}