using UnityEngine;
using UnityEngine.Video; // Thêm thư viện này để điều khiển Video
using UnityEngine.UIElements;
public class FinishLine : MonoBehaviour
{
    [Header("Setup")]
    public VideoPlayer victoryVideo; // Kéo Object có Video Player vào đây
    public GameObject victoryUI;     // (Tùy chọn) Hiện UI sau khi video chạy xong
    public AudioSource victorySound;
    private UIDocument m_GameplayUXML;
    private bool isFinished = false;

    private void Start()
    {
        // Đảm bảo video không tự phát lúc bắt đầu
        if (victoryVideo != null)
        {
            victoryVideo.Stop();
            // Đăng ký sự kiện: Khi video chạy xong thì hiện UI chúc mừng
            victoryVideo.loopPointReached += OnVideoFinished;
        }
       
    }
    private void FindPlayerUI()
    {
        // Cách 1: Tìm theo Tag "Player" (Cách nhanh nhất)
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
        Debug.Log("Về đích! Đang phát video...");
        if (m_GameplayUXML != null)
        {
            m_GameplayUXML.enabled = false;
        }
        // 1. Phát Video
        if (victoryVideo != null)
        {
            // Bật Object chứa video nếu nó đang bị tắt
            victoryVideo.gameObject.SetActive(true);
            victoryVideo.Play();
        }

        // 2. Phát nhạc (nếu có)
        if (victorySound != null) victorySound.Play();

        // 3. Khóa điều khiển nhân vật (tùy chọn)
        // other.GetComponent<CoreMovement>().enabled = false;
    }

    // Hàm này tự động gọi khi video kết thúc
    void OnVideoFinished(VideoPlayer source)
    {
        if (victoryUI != null) victoryUI.SetActive(true);
        Debug.Log("Video kết thúc, hiện bảng điểm/nút chơi lại.");
    }
}