using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public class ObjectCollisionSensor : MonoBehaviour
{
    [SerializeField] private Transform ownerRoot;
    [SerializeField] private LayerMask detectionMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] private bool ignorePlayer = true;
    [SerializeField, Min(1)] private int maxHits = 24;

    private SphereCollider sensorCollider;
    private Collider[] overlapBuffer;

    private void Awake()
    {
        Initialize();
    }

    private void OnValidate()
    {
        sensorCollider = GetComponent<SphereCollider>();
        if (sensorCollider != null)
        {
            sensorCollider.isTrigger = true;
        }

        maxHits = Mathf.Max(1, maxHits);
    }

    public Vector3 GetAvoidanceDirection(Transform owner, Collider ownerCollider)
    {
        Initialize();

        if (sensorCollider == null)
        {
            return Vector3.zero;
        }

        Transform rootToIgnore = ownerRoot != null ? ownerRoot : owner;
        if (rootToIgnore == null)
        {
            rootToIgnore = transform.root;
        }

        EnsureBuffer();

        Vector3 center = transform.TransformPoint(sensorCollider.center);
        float radius = GetScaledRadius();
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer, detectionMask, triggerInteraction);
        Vector3 avoidance = Vector3.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider other = overlapBuffer[i];
            overlapBuffer[i] = null;

            if (!IsValidHit(other, rootToIgnore, ownerCollider))
            {
                continue;
            }

            Vector3 closestPoint = GetClosestPoint(other, center);
            Vector3 away = center - closestPoint;
            away.y = 0f;

            if (away.sqrMagnitude < 0.0001f)
            {
                away = center - other.bounds.center;
                away.y = 0f;
            }

            if (away.sqrMagnitude < 0.0001f)
            {
                away = transform.position - other.transform.position;
                away.y = 0f;
            }

            if (away.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            float distance = Mathf.Max(0.05f, away.magnitude);
            avoidance += away.normalized / distance;
        }

        return avoidance.sqrMagnitude > 0.001f ? avoidance.normalized : Vector3.zero;
    }

    private void Initialize()
    {
        if (sensorCollider == null)
        {
            sensorCollider = GetComponent<SphereCollider>();
        }

        if (sensorCollider != null)
        {
            sensorCollider.isTrigger = true;
        }

        if (ownerRoot == null)
        {
            ownerRoot = transform.root;
        }

        EnsureBuffer();
    }

    private void EnsureBuffer()
    {
        if (overlapBuffer == null || overlapBuffer.Length != maxHits)
        {
            overlapBuffer = new Collider[maxHits];
        }
    }

    private bool IsValidHit(Collider other, Transform rootToIgnore, Collider ownerCollider)
    {
        if (other == null || !other.enabled || other == ownerCollider)
        {
            return false;
        }

        if (rootToIgnore != null && other.transform.root == rootToIgnore.root)
        {
            return false;
        }

        if (other is TerrainCollider)
        {
            return false;
        }

        if (ignorePlayer && IsPlayerCollider(other))
        {
            return false;
        }

        if ((detectionMask.value & (1 << other.gameObject.layer)) == 0)
        {
            return false;
        }

        return true;
    }

    private bool IsPlayerCollider(Collider other)
    {
        return other.CompareTag("Player")
            || other.GetComponentInParent<PlayerHealth>() != null
            || other.GetComponentInParent<PlayerController>() != null;
    }

    private Vector3 GetClosestPoint(Collider other, Vector3 center)
    {
        if (other is BoxCollider || other is SphereCollider || other is CapsuleCollider)
        {
            return other.ClosestPoint(center);
        }

        MeshCollider meshCollider = other as MeshCollider;
        if (meshCollider != null && meshCollider.convex)
        {
            return other.ClosestPoint(center);
        }

        return other.bounds.ClosestPoint(center);
    }

    private float GetScaledRadius()
    {
        Vector3 scale = transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return sensorCollider.radius * maxScale;
    }

    private void OnDrawGizmosSelected()
    {
        sensorCollider = GetComponent<SphereCollider>();
        if (sensorCollider == null)
        {
            return;
        }

        Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.65f);
        Gizmos.DrawWireSphere(transform.TransformPoint(sensorCollider.center), GetScaledRadius());
    }
}
