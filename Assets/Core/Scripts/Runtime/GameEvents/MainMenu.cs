using UnityEngine;
using UnityEngine.SceneManagement; // Thý vi?n ð? chuy?n c?nh

public class MainMenu : MonoBehaviour
{
    // Hàm này s? ch?y khi ngý?i dùng nh?n nút "B?T Ð?U"
    public void BatDauGame()
    {
        // Chuy?n sang c?nh ti?p theo (thý?ng là Level 1 ho?c Intro)
        // B?n c?n ð?m b?o ð? thêm Scene vào Build Settings
        SceneManager.LoadScene("Map1");
        Debug.Log("Ðang t?i tr? chõi...");
    }

    // Hàm này s? ch?y khi ngý?i dùng nh?n nút "THOÁT"
    public void ThoatGame()
    {
        // Thoát ?ng d?ng
        Application.Quit();

        // D?ng này ch? ð? ki?m tra trong môi trý?ng phát tri?n (Editor)
        Debug.Log("Ð? thoát game!");
    }
}