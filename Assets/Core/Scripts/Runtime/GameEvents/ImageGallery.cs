using UnityEngine;
using UnityEngine.Video; // BẮT BUỘC để dùng VideoPlayer
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections;

public class VideoGallery : MonoBehaviour
{
    [Header("Video Display")]
    public VideoPlayer videoPlayer; // Kéo VideoPlayer vào đây

    [Header("List Video Clips")]
    public VideoClip[] videoList;

    [Header("Load Scene")]
    public string nextSceneName ;

    private int currentIndex = 0;

    void Start()
    {
        // 1. Mở khóa chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (videoPlayer != null)
        {
            // Đăng ký sự kiện: Khi video chạy hết thì gọi hàm OnVideoFinished
            videoPlayer.loopPointReached += OnVideoFinished;
            PlayCurrentVideo();
        }
    }
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            NextVideo();
        }
    }
    // Hàm này tự động gọi khi video chạy đến giây cuối cùng
    private void OnVideoFinished(VideoPlayer source)
    {
        NextVideo();
    }

    public void NextVideo()
    {
        currentIndex++;

        if (currentIndex >= videoList.Length)
        {
            // Đã xem hết toàn bộ video -> Chuyển Map
            LoadNextScene();
        }
        else
        {
            PlayCurrentVideo();
        }
    }

    private void PlayCurrentVideo()
    {
        if (videoList.Length > 0 && currentIndex < videoList.Length)
        {
            videoPlayer.clip = videoList[currentIndex];
            videoPlayer.Play();
        }
    }

    private void LoadNextScene()
    {
        Debug.Log("Kết thúc Cutscene, đang dọn dẹp để vào Map chiến đấu...");
        StartCoroutine(ResetAndLoadRoutine());
    }

    private IEnumerator ResetAndLoadRoutine()
    {
        // 1. Ngắt kết nối Network an toàn (Tránh lỗi Respawn ở Map cũ)
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            yield return null;
            yield return null;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.gameObject != null)
            {
                Destroy(NetworkManager.Singleton.gameObject);
            }
        }

        // 2. Xóa các Manager cũ để Map mới tự khởi tạo lại từ đầu
        DestroyOldManager("GameManager");
        DestroyOldManager("GameNetworkManager");

        yield return null;

        // 3. Tải Map mới
        SceneManager.LoadScene(nextSceneName);
    }

    private void DestroyOldManager(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null) Destroy(obj);
    }

    // Hủy đăng ký sự kiện khi Object bị xóa để tránh lỗi bộ nhớ
    private void OnDestroy()    
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}