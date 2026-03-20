using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Script tu dong setup hieu ung UI cho Main Menu game Thanh Giong.
/// Attach vao Canvas GameObject trong scene MainMenu.
/// </summary>
public class MainMenuUISetup : MonoBehaviour
{
    [Header("Panel Background")]
    [SerializeField] private Image panelBackground;
    [SerializeField] private Color panelColor = new Color(0.08f, 0.04f, 0.01f, 0.75f);

    [Header("Button Glow Colors")]
    [SerializeField] private Color batDauNormalColor = new Color(0.6f, 0.35f, 0.05f, 1f);
    [SerializeField] private Color batDauHoverColor = new Color(1f, 0.75f, 0.15f, 1f);
    [SerializeField] private Color thoatNormalColor = new Color(0.45f, 0.2f, 0.05f, 1f);
    [SerializeField] private Color thoatHoverColor = new Color(0.85f, 0.55f, 0.1f, 1f);

    [Header("Breathing Animation")]
    [SerializeField] private float breathingSpeed = 1.2f;
    [SerializeField] private float breathingAmount = 0.03f;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem[] ambientParticles;

    private bool isRunning = true;

    private void Awake()
    {
        if (panelBackground != null)
            panelBackground.color = panelColor;
    }

    private void Start()
    {
        StartCoroutine(BreathingAnimation());
        if (ambientParticles != null)
        {
            foreach (var ps in ambientParticles)
            {
                if (ps != null) ps.Play();
            }
        }
    }

    private void OnDestroy()
    {
        isRunning = false;
    }

    private IEnumerator BreathingAnimation()
    {
        float time = 0f;
        Transform t = this.transform;
        Vector3 originalScale = t.localScale;

        while (isRunning)
        {
            time += Time.deltaTime * breathingSpeed;
            float scale = 1f + Mathf.Sin(time) * breathingAmount;
            t.localScale = originalScale * scale;
            yield return null;
        }
        t.localScale = originalScale;
    }
}