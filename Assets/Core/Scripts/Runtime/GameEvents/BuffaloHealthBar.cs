using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space orange health bar floating above the buffalo's head.
/// Only visible when the player is within visibility range.
/// Reads catch progress from BuffaloAI to display remaining HP.
/// </summary>
public class BuffaloHealthBar : MonoBehaviour
{
    [Header("Bar Settings")]
    [SerializeField] private float heightOffset = 2.5f;
    [SerializeField] private float barWidth = 1.2f;
    [SerializeField] private float barHeight = 0.1f;

    [Header("Visibility")]
    [Tooltip("Distance at which the health bar becomes visible.")]
    [SerializeField] private float visibilityRange = 10f;

    [Header("Colors")]
    [SerializeField] private Color fillColor = new Color(1f, 0.55f, 0f);
    [SerializeField] private Color trailColor = new Color(0.6f, 0.2f, 0.05f);
    [SerializeField] private Color bgColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);

    [Header("Trail")]
    [SerializeField] private float trailSpeed = 0.4f;

    private BuffaloAI m_Buffalo;
    private Camera m_Cam;
    private Transform m_Player;
    private GameObject m_CanvasObj;
    private RectTransform m_FillRT;
    private RectTransform m_TrailRT;
    private float m_TrailValue = 1f;

    void Start()
    {
        m_Buffalo = GetComponent<BuffaloAI>();
        if (m_Buffalo == null)
        {
            Debug.LogError($"[BuffaloHealthBar] No BuffaloAI on {gameObject.name}");
            enabled = false;
            return;
        }

        m_Cam = Camera.main;
        Build();
        m_CanvasObj.SetActive(false);
    }

    void LateUpdate()
    {
        if (m_Buffalo == null || m_CanvasObj == null) return;

        if (m_Buffalo.IsCaught)
        {
            if (m_CanvasObj.activeSelf) m_CanvasObj.SetActive(false);
            return;
        }

        if (m_Player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) m_Player = playerObj.transform;
        }

        if (m_Cam == null) m_Cam = Camera.main;

        bool inRange = false;
        if (m_Player != null)
        {
            float dist = Vector3.Distance(transform.position, m_Player.position);
            inRange = dist <= visibilityRange;
        }

        if (m_CanvasObj.activeSelf != inRange)
            m_CanvasObj.SetActive(inRange);

        if (!inRange) return;

        m_CanvasObj.transform.position = transform.position + Vector3.up * heightOffset;

        if (m_Cam != null)
        {
            Vector3 dir = m_Cam.transform.position - m_CanvasObj.transform.position;
            if (dir.sqrMagnitude > 0.001f)
                m_CanvasObj.transform.rotation = Quaternion.LookRotation(dir);
        }

        float ratio = m_Buffalo.HealthRatio;
        m_FillRT.anchorMax = new Vector2(ratio, 1f);

        m_TrailValue = Mathf.MoveTowards(m_TrailValue, ratio, trailSpeed * Time.deltaTime);
        m_TrailRT.anchorMax = new Vector2(m_TrailValue, 1f);
    }

    void Build()
    {
        m_CanvasObj = new GameObject($"BuffaloHP_{gameObject.name}");

        var canvas = m_CanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        var rt = m_CanvasObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(barWidth * 100f, barHeight * 100f);
        rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Border
        var borderRT = MakeRect("Border", rt, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        borderRT.offsetMin = new Vector2(-2, -2);
        borderRT.offsetMax = new Vector2(2, 2);
        borderRT.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);

        // Background
        var bgRT = MakeRect("BG", rt, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bgRT.gameObject.AddComponent<Image>().color = bgColor;

        // Trail (lags behind the main fill)
        m_TrailRT = MakeRect("Trail", rt, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, Vector2.zero);
        m_TrailRT.offsetMin = new Vector2(2, 2);
        m_TrailRT.offsetMax = new Vector2(-2, -2);
        m_TrailRT.gameObject.AddComponent<Image>().color = trailColor;

        // Fill (orange health bar)
        m_FillRT = MakeRect("Fill", rt, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, Vector2.zero);
        m_FillRT.offsetMin = new Vector2(2, 2);
        m_FillRT.offsetMax = new Vector2(-2, -2);
        m_FillRT.gameObject.AddComponent<Image>().color = fillColor;
    }

    RectTransform MakeRect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var r = obj.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.pivot = pivot;
        r.anchoredPosition = anchoredPos;
        r.sizeDelta = sizeDelta;
        return r;
    }

    void OnDestroy()
    {
        if (m_CanvasObj != null) Destroy(m_CanvasObj);
    }
}
