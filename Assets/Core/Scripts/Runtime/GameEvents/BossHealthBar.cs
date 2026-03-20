using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space boss health bar pinned to the top of the screen.
/// Attach to the boss GameObject that has GeneralHitReceiver.
/// Only visible when player is within showDistance meters.
/// Uses RectTransform anchor scaling (no sprite dependency).
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private string bossDisplayName = "TƯỚNG GIẶC ÂN";

    [Header("Visibility")]
    [Tooltip("Only show when player is within this distance (meters).")]
    [SerializeField] private float showDistance = 15f;

    [Header("Bar Layout")]
    [SerializeField] private float barWidthPercent = 0.4f;
    [SerializeField] private float barHeight = 28f;
    [SerializeField] private float topMargin = 25f;

    [Header("Colors")]
    [SerializeField] private Color fillColor = new Color(0.95f, 0.6f, 0.1f);
    [SerializeField] private Color trailColor = new Color(0.6f, 0.2f, 0.05f);
    [SerializeField] private Color bgColor = new Color(0.12f, 0.12f, 0.12f, 0.92f);

    [Header("Trail")]
    [SerializeField] private float trailSpeed = 0.35f;

    private GeneralHitReceiver m_HR;
    private float m_MaxHP;
    private float m_TrailValue;
    private GameObject m_CanvasObj;
    private RectTransform m_FillRT;
    private RectTransform m_TrailRT;
    private Transform m_Player;

    void Start()
    {
        m_HR = GetComponent<GeneralHitReceiver>();
        if (m_HR == null)
        {
            Debug.LogError("[BossHealthBar] GeneralHitReceiver not found!");
            enabled = false;
            return;
        }

        m_MaxHP = m_HR.health;
        m_TrailValue = 1f;

        BuildUI();
        m_CanvasObj.SetActive(false);
    }

    void Update()
    {
        if (m_HR == null || m_FillRT == null) return;

        if (m_HR.isDead)
        {
            if (m_CanvasObj.activeSelf) m_CanvasObj.SetActive(false);
            return;
        }

        if (m_Player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) m_Player = playerObj.transform;
        }

        bool inRange = false;
        if (m_Player != null)
        {
            float dist = Vector3.Distance(transform.position, m_Player.position);
            inRange = dist <= showDistance;
        }

        if (m_CanvasObj.activeSelf != inRange)
            m_CanvasObj.SetActive(inRange);

        if (!inRange) return;

        float ratio = Mathf.Clamp01(Mathf.Max(0, m_HR.health) / m_MaxHP);
        m_FillRT.anchorMax = new Vector2(ratio, 1f);

        m_TrailValue = Mathf.MoveTowards(m_TrailValue, ratio, trailSpeed * Time.deltaTime);
        m_TrailRT.anchorMax = new Vector2(m_TrailValue, 1f);
    }

    void BuildUI()
    {
        m_CanvasObj = new GameObject("BossHealthBarCanvas");
        var canvas = m_CanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        var scaler = m_CanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        m_CanvasObj.AddComponent<GraphicRaycaster>();

        float containerW = 1920f * barWidthPercent;
        float containerH = barHeight + 40f;

        var container = MakeRect("Container", m_CanvasObj.transform,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -topMargin), new Vector2(containerW, containerH));

        // Boss name text (top half)
        var nameRT = MakeRect("BossName", container,
            new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;

        var nameText = nameRT.gameObject.AddComponent<Text>();
        nameText.text = bossDisplayName;
        nameText.fontSize = 24;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = Color.white;
        nameText.fontStyle = UnityEngine.FontStyle.Bold;
        nameText.font = GetFont();

        var outline = nameRT.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Bar area container (bottom half)
        var barArea = MakeRect("BarArea", container,
            new Vector2(0, 0), new Vector2(1, 0.45f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        barArea.offsetMin = Vector2.zero;
        barArea.offsetMax = Vector2.zero;

        // Border (slightly bigger, behind everything)
        var border = MakeRect("Border", barArea,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        border.offsetMin = new Vector2(-3, -3);
        border.offsetMax = new Vector2(3, 3);
        AddImage(border, new Color(0.35f, 0.35f, 0.35f));

        // Background (full width)
        var bg = MakeRect("BG", barArea,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;
        AddImage(bg, bgColor);

        // Trail (dark orange, lags behind main fill)
        m_TrailRT = MakeRect("Trail", barArea,
            Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, Vector2.zero);
        m_TrailRT.offsetMin = new Vector2(2, 2);
        m_TrailRT.offsetMax = new Vector2(-2, -2);
        AddImage(m_TrailRT, trailColor);

        // Health fill (main orange bar, scales via anchorMax.x)
        m_FillRT = MakeRect("Fill", barArea,
            Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, Vector2.zero);
        m_FillRT.offsetMin = new Vector2(2, 2);
        m_FillRT.offsetMax = new Vector2(-2, -2);
        AddImage(m_FillRT, fillColor);
    }

    void AddImage(RectTransform rt, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
    }

    RectTransform MakeRect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return rt;
    }

    Font GetFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) return f;
        f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f != null) return f;
        return Font.CreateDynamicFontFromOSFont("Arial", 24);
    }

    void OnDestroy()
    {
        if (m_CanvasObj != null) Destroy(m_CanvasObj);
    }
}
