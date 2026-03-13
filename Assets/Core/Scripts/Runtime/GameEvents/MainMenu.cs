using UnityEngine;
using UnityEngine.SceneManagement; // Th? vi?n ?? chuy?n c?nh

public class MainMenu : MonoBehaviour
{
    // Hàm này s? ch?y khi ng??i dùng nh?n nút "B?T ??U"
    public void BatDauGame()
    {
        // Chuy?n sang c?nh ti?p theo (th??ng là Level 1 ho?c Intro)
        // B?n c?n ??m b?o ?? thêm Scene vào Build Settings
        SceneManager.LoadScene("Map1");
        Debug.Log("?ang t?i tr? ch?i...");
    }

    // Hàm này s? ch?y khi ng??i dùng nh?n nút "THOÁT"
    public void ThoatGame()
    {
        // Thoát ?ng d?ng
        Application.Quit();

        // D?ng này ch? ?? ki?m tra trong môi tr??ng phát tri?n (Editor)
        Debug.Log("?? thoát game!");
    }
}