using UnityEngine;

[DisallowMultipleComponent]
public class GhostTargetAura : MonoBehaviour
{
    private static readonly int AuraColorId = Shader.PropertyToID("_AuraColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

    [SerializeField] private SkinnedMeshRenderer sourceRenderer;
    [SerializeField] private SkinnedMeshRenderer auraRenderer;
    [SerializeField] private Material auraMaterial;
    [SerializeField, Min(0f)] private float outlineWidth = 0.045f;
    [SerializeField, Min(0f)] private float pulseWidth = 0.012f;
    [SerializeField, Min(0f)] private float pulseSpeed = 4f;
    [SerializeField] private Color auraColor = new Color(1f, 1f, 1f, 0.82f);

    private MaterialPropertyBlock propertyBlock;
    private bool isVisible;

    private void Awake()
    {
        EnsurePropertyBlock();
        ResolveRendererReferences();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (!isVisible || auraRenderer == null)
        {
            return;
        }

        float pulse = pulseWidth > 0f
            ? (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * pulseWidth
            : 0f;

        EnsurePropertyBlock();
        auraRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(AuraColorId, auraColor);
        propertyBlock.SetFloat(OutlineWidthId, outlineWidth + pulse);
        auraRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        ResolveRendererReferences();

        if (auraRenderer != null)
        {
            auraRenderer.enabled = visible;
        }
    }

    private void ResolveRendererReferences()
    {
        if (sourceRenderer == null)
        {
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i] != auraRenderer)
                {
                    sourceRenderer = renderers[i];
                    break;
                }
            }
        }

        if (auraRenderer == null)
        {
            Transform auraTransform = transform.Find("TargetLockAura");
            if (auraTransform != null)
            {
                auraRenderer = auraTransform.GetComponent<SkinnedMeshRenderer>();
            }
        }

        if (sourceRenderer == null || auraRenderer == null)
        {
            return;
        }

        auraRenderer.sharedMesh = sourceRenderer.sharedMesh;
        auraRenderer.bones = sourceRenderer.bones;
        auraRenderer.rootBone = sourceRenderer.rootBone;
        auraRenderer.updateWhenOffscreen = sourceRenderer.updateWhenOffscreen;
        auraRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        auraRenderer.receiveShadows = false;

        if (auraMaterial != null)
        {
            auraRenderer.sharedMaterial = auraMaterial;
        }
    }

    private void EnsurePropertyBlock()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }
}
