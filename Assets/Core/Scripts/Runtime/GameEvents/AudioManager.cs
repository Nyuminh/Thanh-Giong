using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; // T?o m?t bi?n Singleton ð? g?i ? b?t c? ðâu
    public AudioMixer mainMixer; // Kéo file Mixer vào ðây

    void Awake()
    {
        // Ki?m tra xem ð? có AudioManager nào t?n t?i chýa
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // L?nh "B?t t?": Không b? h?y khi ð?i scene
        }
        else
        {
            Destroy(gameObject); // Xóa b?n sao th?a n?u l? load l?i scene
        }
    }

    void Start()
    {
        // V?a vào game là l?y ngay m?c âm lý?ng ð? lýu ð? ép Mixer nghe theo
        LoadVolume();
    }

    public void LoadVolume()
    {
        // L?y s? li?u ð? lýu (m?c ð?nh là 1 n?u chýa lýu bao gi?)
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        // Ép Mixer ð?i âm lý?ng ngay l?p t?c
        mainMixer.SetFloat("MusicVol", Mathf.Log10(savedVolume) * 20);
    }
}