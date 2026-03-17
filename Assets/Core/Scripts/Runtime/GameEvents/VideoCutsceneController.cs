using Blocks.Gameplay.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.Video; // Thư viện để điều khiển Video
public class VideoCutsceneController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject playerObject;   // Nhân vật của bạn
    public GameObject gameplayUI;     // Máu, bản đồ, vv...
    public AudioSource characterAudio;
    void Start()
    {
        // 1. Khóa mọi thứ khi bắt đầu phát video
        playerObject.SetActive(false);
        gameplayUI.SetActive(false);
        
        // 2. Đăng ký sự kiện: Khi video chạy đến khung hình cuối cùng
        videoPlayer.loopPointReached += OnVideoFinished;
    }
    void Update()
    {
        // Kiểm tra phím ESC bằng Input System mới
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SkipVideo();
        }
    }

    public void SkipVideo()
    {
        Debug.Log("Skip Video bằng Input System!");
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        OnVideoFinished(videoPlayer);
    }
    void OnVideoFinished(VideoPlayer source)
    {
        Debug.Log("Video kết thúc! Bắt đầu chơi game.");
        if (characterAudio != null)
        {
            characterAudio.Play();
        }
        // 3. Mở khóa nhân vật và UI
        playerObject.SetActive(true);
        gameplayUI.SetActive(true);
        if (VillageQuestManager.Instance != null)
        {
            VillageQuestManager.Instance.RefreshQuestUI();
        }

        // 4. Tắt Video Player để giải phóng bộ nhớ (hoặc tắt Object này đi)
        gameObject.SetActive(false);

        // Hủy đăng ký để tránh lỗi
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    // Thêm tính năng nhấn ESC để bỏ qua video nhanh
    
}