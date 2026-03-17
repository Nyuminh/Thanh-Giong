using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // B?T BU?C thêm d?ng này ð? dùng l?nh chuy?n Scene
using Unity.Netcode;
using System.Collections;
public class ImageGallery : MonoBehaviour
{
    [Header("ImageDisplay")]
    public Image displayImage;

    [Header("ListImage")]
    public Sprite[] imageList;

    [Header("LoadScene")]
    public string nextSceneName = "Map1"; // Ð?t tên Scene b?n mu?n chuy?n qua ? ðây

    private int currentIndex = 0;

    void Start()
    {
        // 1. M? khóa chu?t (tránh vi?c chu?t b? k?t ? gi?a màn h?nh)
        Cursor.lockState = CursorLockMode.None;

        // 2. Hi?n th? chu?t lên
        Cursor.visible = true;
        UpdateDisplay();
    }

    // Hàm g?i khi b?m nút Next (Ti?n)
    public void NextImage()
    {
        currentIndex++;

        // Ki?m tra xem ð? xem qua h?nh cu?i cùng chýa
        if (currentIndex >= imageList.Length)
        {
            // Ð? xem h?t -> Load qua Scene Map 1
            LoadNextScene();
        }
        else
        {
            // Chýa h?t -> C?p nh?t hi?n th? h?nh ti?p theo
            UpdateDisplay();
        }
    }

 

    // C?p nh?t h?nh ?nh lên màn h?nh
    private void UpdateDisplay()
    {
        if (imageList.Length > 0 && currentIndex < imageList.Length)
        {
            displayImage.sprite = imageList[currentIndex];
        }
    }

    // Hàm x? l? chuy?n Scene
    private void LoadNextScene()
    {
        Debug.Log("B?t ð?u ti?n tr?nh ng?t m?ng và Reset an toàn...");
        // Kh?i ch?y ti?n tr?nh Reset có ð? tr?
        StartCoroutine(ResetAndLoadRoutine());
    }

    private IEnumerator ResetAndLoadRoutine()
    {
        // 1. NG?T M?NG AN TOÀN
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();

            // QUAN TR?NG NH?T: Ð?i 2 khung h?nh ð? h? th?ng Netcode ng?m d?n d?p xong
            yield return null;
            yield return null;

            // Sau khi Netcode d?n xong, ta m?i phá h?y tàn tích c?a nó
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.gameObject != null)
            {
                Destroy(NetworkManager.Singleton.gameObject);
            }
        }

        // 2. D?N D?P GAME MANAGER C?A MAP 1-1
        DestroyOldManager("GameManager");
        DestroyOldManager("GameNetworkManager");

        // Ð?i thêm 1 khung h?nh n?a cho ch?c ch?n m?i th? ð? b? xóa s?ch kh?i b? nh?
        yield return null;

        // 3. T?I MAP 1 (Gióng L?n xu?t hi?n)
        Debug.Log("Ð? d?n d?p xong! Ðang t?i Scene: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }

    // Hàm h? tr? t?m và xóa Manager c?
    private void DestroyOldManager(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null)
        {
            Destroy(obj);
        }
    }
}