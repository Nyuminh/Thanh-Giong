using UnityEngine;
using System.Collections;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Settings")]
    public AudioSource bgmSource;
    public float maxVolume = 0.5f;    // Âm lượng lúc bình thường
    public float lowVolume = 0.1f;    // Âm lượng khi đang nói chuyện (nhỏ lại)
    public float fadeDuration = 1.5f; // Tốc độ to/nhỏ dần
    public float idleDelay = 5.0f;    // Chờ 5s sau khi nói xong

    private Coroutine _fadeCoroutine;
    private Coroutine _timerCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Gọi hàm này khi BẮT ĐẦU nói chuyện
    public void LowerBGM()
    {
        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        FadeTo(lowVolume); // Giảm xuống mức nhỏ thay vì tắt hẳn
    }

    // Gọi hàm này khi KẾT THÚC nói chuyện
    public void RestoreBGMWithDelay()
    {
        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        _timerCoroutine = StartCoroutine(WaitAndRestore());
    }

    private IEnumerator WaitAndRestore()
    {
        yield return new WaitForSeconds(idleDelay);
        FadeTo(maxVolume); // Trả lại âm lượng ban đầu
    }

    private void FadeTo(float targetVolume)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(targetVolume));
    }

    private IEnumerator FadeRoutine(float targetVolume)
    {
        float startVolume = bgmSource.volume;
        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / fadeDuration);
            yield return null;
        }
        bgmSource.volume = targetVolume;
    }
}