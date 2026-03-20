using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small world-space yellow health bar floating above a minion enemy's head.
/// Attach to any enemy GameObject that has GeneralHitReceiver.
/// Uses World Space Canvas (works with any render pipeline).
/// </summary>
public class MinionHealthBar : MonoBehaviour
{
    [Header("Bar Settings")]
    [SerializeField] private float heightOffset = 2.2f;
    [SerializeField] private float barWidth = 1.0f;
    [SerializeField] private float barHeight = 0.08f;

    [Header("Colors")]
    [SerializeField] private Color fillColor = new Color(1f, 0.9f, 0.1f);
    [SerializeField] private Color bgColor = new Color(0.2f, 0.2f, 0.2f, 0.85f);

    private GeneralHitReceiver m_HR;
    private float m_MaxHP;
    private Camera m_Cam;
    private GameObject m_CanvasObj;
    private RectTransform m_FillRT;

    void Start()
    {
        m_HR = GetComponent<GeneralHitReceiver>();
        if (m_HR == null)
        {
            Debug.LogError($"[MinionHealthBar] No GeneralHitReceiver on {gameObject.name}");
            enabled = false;
            return;
        }

        m_MaxHP = m_HR.health;
        if (m_MaxHP <= 0) m_MaxHP = 1;

        m_Cam = Camera.main;
        Build();
    }

    void LateUpdate()
    {
        if (m_HR == null || m_CanvasObj == null) return;

        if (m_HR.isDead)
        {
            if (m_CanvasObj.activeSelf) m_CanvasObj.SetActive(false);
            return;
        }

        if (m_Cam == null) m_Cam = Camera.main;
        if (m_Cam == null) return;

        m_CanvasObj.transform.position = transform.position + Vector3.up * heightOffset;

        // Billboard: face camera
        Vector3 dir = m_Cam.transform.position - m_CanvasObj.transform.position;
        if (dir.sqrMagnitude > 0.001f)
            m_CanvasObj.transform.rotation = Quaternion.LookRotation(dir);

        float ratio = Mathf.Clamp01(Mathf.Max(0, m_HR.health) / m_MaxHP);
        m_FillRT.anchorMax = new Vector2(ratio, 1f);
    }

    void Build()
    {
        m_CanvasObj = new GameObject($"MinionHP_{gameObject.name}");

        var canvas = m_CanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        var rt = m_CanvasObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(barWidth * 100f, barHeight * 100f);
        rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Background
        var bgRT = MakeRect("BG", rt, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bgRT.gameObject.AddComponent<Image>().color = bgColor;

        // Fill (scales via anchorMax.x)
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
