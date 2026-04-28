using UnityEngine;

public class AimTarget : MonoBehaviour
{
    public Transform cameraTransform;
    public float distance = 10f;

    private bool warnedMissingCamera;
    private Transform lockTarget;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (lockTarget != null)
        {
            transform.position = lockTarget.position;
            return;
        }

        if (cameraTransform == null)
        {
            if (!warnedMissingCamera)
            {
                Debug.LogWarning("CameraTransform nao esta atribuido.");
                warnedMissingCamera = true;
            }

            return;
        }

        transform.position = cameraTransform.position + cameraTransform.forward * distance;
    }

    public void SetLockTarget(Transform target)
    {
        lockTarget = target;
    }

    public void ClearLockTarget()
    {
        lockTarget = null;
    }
}
