using UnityEngine;
using UnityEngine.SceneManagement; // Thêm thư viện này để quản lý chuyển cảnh
using UnityEngine.UIElements;

public class FinishLine : MonoBehaviour
{
    [Header("Setup")]
    // Tạo một biến để bạn có thể gõ tên Scene trực tiếp trên Inspector (rất tiện để sửa lỗi chính tả)
    public string afterCreditSceneName = "AfterCrediit";
    public AudioSource victorySound;

    private UIDocument m_GameplayUXML;
    private bool isFinished = false;

    private void Start()
    {
        // Đã xóa bỏ các thiết lập liên quan đến VideoPlayer
    }

    private void FindPlayerUI()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            m_GameplayUXML = player.GetComponentInChildren<UIDocument>();
            if (m_GameplayUXML == null)
            {
                Debug.LogWarning("[FinishLine] Tìm thấy Player nhưng không thấy UIDocument trên Player!");
            }
        }
        else
        {
            Debug.LogError("[FinishLine] Không tìm thấy Object nào có Tag 'Player' trong Scene!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isFinished)
        {
            isFinished = true;
            CompleteGame();
        }
    }

    void CompleteGame()
    {
        FindPlayerUI();
        Debug.Log("Về đích! Đang chuyển qua scene After Credit...");

        // 1. Tắt giao diện UI của người chơi lúc đang chạy
        if (m_GameplayUXML != null)
        {
            m_GameplayUXML.enabled = false;
        }

        // 2. Phát nhạc chiến thắng (nếu có)
        if (victorySound != null)
        {
            victorySound.Play();
        }

        // 3. Chuyển cảnh sang After Credit
        SceneManager.LoadScene(afterCreditSceneName);
    }
}