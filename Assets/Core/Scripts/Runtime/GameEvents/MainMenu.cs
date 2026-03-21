using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using Blocks.Gameplay.Core; // Để dùng PauseMenuController

public class MainMenu : MonoBehaviour
{
    [Header("Cấu hình Âm thanh")]
    public AudioMixer mainMixer; // Kéo file Audio Mixer vào đây
    public GameObject settingsPanel; // Kéo cái Panel Cài Đặt vào đây
    public Slider volumeSlider;
    public Button continueButton; // Kéo nút "Tiếp Tục" vào đây trong Inspector
    // --- CÁC HÀM CŨ CỦA BẠN ---

    void Start()
    {
        // Chỉ việc cập nhật thanh Slider cho khớp với số đã lưu
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
        }
        if (continueButton != null)
        {
            continueButton.interactable = PauseMenuController.HasSaveData();
        }
    }
    public void BatDauGame()
    {
        SceneManager.LoadScene("Intro");
        Debug.Log("Đang tải trò chơi...");
    }
    public void TiepTucGame()
    {
        if (PauseMenuController.HasSaveData())
        {
            PauseMenuController.LoadSavedGame();
        }
        else
        {
            Debug.Log("Chưa có dữ liệu lưu!");
        }
    }
    public void ThoatGame()
    {
        Application.Quit();
        Debug.Log("Đã thoát game!");
    }

    // --- CÁC HÀM MỚI ---

    // Hàm mở/đóng Panel
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
    // Hàm để Slider gọi tới khi kéo
    public void SetVolume(float volume)
    {
        // "MusicVol" phải trùng tên với tham số bạn đã Expose trong Mixer
        // Công thức Mathf.Log10 giúp âm thanh giảm mượt mà hơn theo tai người
        mainMixer.SetFloat("MusicVol", Mathf.Log10(volume) * 20);
    }
}