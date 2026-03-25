using System.Collections;
using UnityEngine;

/// <summary>
/// Enables TrailRenderer emission briefly on attack so the weapon leaves a visible arc.
/// Auto-creates a child at the weapon tip (via tipOffset) so the trail follows the blade,
/// not the handle grip.
/// </summary>
public class WeaponTrailEffect : MonoBehaviour
{
    [SerializeField] private TrailRenderer[] trails;

    [Tooltip("If no trails assigned, auto-create one on a tip child.")]
    [SerializeField] private bool createIfMissing = true;

    [Tooltip("Default burst length when Play() is called without duration.")]
    [SerializeField] private float defaultPlayDuration = 0.35f;

    [Header("Tip Offset (where the blade/tip is relative to this object)")]
    [Tooltip("Local offset from this object to the weapon tip. For a polearm held upright, try (0, 1.2, 0).")]
    [SerializeField] private Vector3 tipOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Auto trail style")]
    [SerializeField] private float trailTime = 0.25f;
    [SerializeField] private float startWidth = 0.12f;
    [SerializeField] private float endWidth = 0.02f;
    [SerializeField] private Color trailColor = new Color(1f, 0.92f, 0.65f, 0.85f);
    [SerializeField] private Color trailColorEnd = new Color(1f, 0.9f, 0.5f, 0f);

    private void Awake()
    {
        if (trails == null || trails.Length == 0)
            trails = GetComponentsInChildren<TrailRenderer>(true);

        if (createIfMissing && (trails == null || trails.Length == 0))
        {
            var tipChild = new GameObject("WeaponTip_Trail");
            tipChild.transform.SetParent(transform, false);
            tipChild.transform.localPosition = tipOffset;

            var tr = tipChild.AddComponent<TrailRenderer>();
            ConfigureTrail(tr);
            trails = new[] { tr };
        }

        foreach (var t in trails)
        {
            if (t != null)
            {
                t.emitting = false;
                t.Clear();
            }
        }
    }

    private void ConfigureTrail(TrailRenderer tr)
    {
        tr.time = trailTime;
        tr.minVertexDistance = 0.02f;
        tr.widthMultiplier = 1f;
        var w = new AnimationCurve();
        w.AddKey(0f, startWidth);
        w.AddKey(1f, endWidth);
        tr.widthCurve = w;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(trailColor, 0f),
                new GradientColorKey(new Color(trailColorEnd.r, trailColorEnd.g, trailColorEnd.b), 1f)
            },
            new[] { new GradientAlphaKey(trailColor.a, 0f), new GradientAlphaKey(0f, 1f) });
        tr.colorGradient = g;
        tr.numCapVertices = 2;
        tr.numCornerVertices = 2;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        if (tr.sharedMaterial == null || tr.sharedMaterial.shader.name.Contains("Hidden"))
            TryAssignDefaultTrailMaterial(tr);
    }

    private static void TryAssignDefaultTrailMaterial(TrailRenderer tr)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Particles/Standard Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s != null)
        {
            var mat = new Material(s);
            mat.color = Color.white;
            tr.sharedMaterial = mat;
        }
    }

    public void Play(float duration = -1f)
    {
        if (trails == null || trails.Length == 0) return;
        if (duration < 0f) duration = defaultPlayDuration;

        StopAllCoroutines();
        foreach (var t in trails)
        {
            if (t == null) continue;
            t.Clear();
            t.emitting = true;
        }

        StartCoroutine(StopEmittingAfter(duration));
    }

    private IEnumerator StopEmittingAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        foreach (var t in trails)
        {
            if (t != null)
                t.emitting = false;
        }
    }
}
