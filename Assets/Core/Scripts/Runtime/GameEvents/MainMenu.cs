using UnityEngine;
using UnityEngine.SceneManagement; // Th? vi?n ?? chuy?n c?nh

public class MainMenu : MonoBehaviour
{
    // Hàm này s? ch?y khi ng??i dùng nh?n nút "B?T ??U"
    public void BatDauGame()
    {

        SceneManager.LoadScene("Intro");

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