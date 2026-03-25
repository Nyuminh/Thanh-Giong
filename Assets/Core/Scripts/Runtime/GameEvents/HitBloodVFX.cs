using UnityEngine;

/// <summary>
/// Spawns a quick blood-splash particle burst at a world position.
/// Call HitBloodVFX.Spawn(position) from combat scripts.
/// No prefab needed — builds a ParticleSystem at runtime.
/// </summary>
public static class HitBloodVFX
{
    private static Material s_ParticleMat;

    public static void Spawn(Vector3 position)
    {
        Spawn(position, Vector3.up);
    }

    public static void Spawn(Vector3 position, Vector3 hitNormal)
    {
        var go = new GameObject("BloodSplash");
        go.transform.position = position;
        go.transform.rotation = Quaternion.LookRotation(hitNormal);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.7f, 0.05f, 0.05f, 1f),
            new Color(0.45f, 0f, 0f, 1f));
        main.gravityModifier = 1.5f;
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 12, 20)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.1f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = GetParticleMaterial();

        ps.Play();
    }

    private static Material GetParticleMaterial()
    {
        if (s_ParticleMat != null) return s_ParticleMat;

        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Particles/Standard Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s != null)
        {
            s_ParticleMat = new Material(s);
            s_ParticleMat.color = new Color(0.6f, 0.02f, 0.02f, 1f);
        }
        return s_ParticleMat;
    }
}
