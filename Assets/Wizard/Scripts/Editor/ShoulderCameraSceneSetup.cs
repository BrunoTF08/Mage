using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShoulderCameraSceneSetup
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Wizard Testes.unity",
        "Assets/Scenes/Wizard Testes 1.unity"
    };

    public static void Apply()
    {
        foreach (string scenePath in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            PlayerController playerController = Object.FindObjectOfType<PlayerController>(true);
            Camera mainCamera = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>(true);

            if (playerController == null || mainCamera == null)
            {
                Debug.LogError($"Shoulder camera setup skipped for {scenePath}: player or main camera missing.");
                continue;
            }

            ConfigureShoulderCamera(mainCamera, playerController.transform);
            ConfigurePlayerController(playerController, mainCamera.transform);
            ConfigureAimTargets(mainCamera.transform);
            DisableCinemachineForDirectCamera(mainCamera);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Shoulder camera setup applied to {scenePath}.");
        }

        AssetDatabase.SaveAssets();
    }

    private static void ConfigureShoulderCamera(Camera mainCamera, Transform player)
    {
        ShoulderCameraController controller = mainCamera.GetComponent<ShoulderCameraController>();
        if (controller == null)
        {
            controller = mainCamera.gameObject.AddComponent<ShoulderCameraController>();
        }

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("player").objectReferenceValue = player;
        serializedController.FindProperty("shoulderOffset").vector3Value = new Vector3(0.62f, 1.55f, 0f);
        serializedController.FindProperty("distance").floatValue = 3.2f;
        serializedController.FindProperty("mouseSensitivity").floatValue = 2.2f;
        serializedController.FindProperty("startingPitch").floatValue = 12f;
        serializedController.FindProperty("minPitch").floatValue = -30f;
        serializedController.FindProperty("maxPitch").floatValue = 65f;
        serializedController.FindProperty("positionSharpness").floatValue = 24f;
        serializedController.FindProperty("rotationSharpness").floatValue = 28f;
        serializedController.FindProperty("rotatePlayerWithCamera").boolValue = true;
        serializedController.FindProperty("lockCursor").boolValue = true;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        controller.SnapToTarget();
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(mainCamera.transform);
    }

    private static void ConfigurePlayerController(PlayerController playerController, Transform cameraTransform)
    {
        SerializedObject serializedPlayer = new SerializedObject(playerController);
        SerializedProperty cameraProperty = serializedPlayer.FindProperty("cameraTransform");
        SerializedProperty relativeMoveProperty = serializedPlayer.FindProperty("moveRelativeToCamera");

        if (cameraProperty != null)
        {
            cameraProperty.objectReferenceValue = cameraTransform;
        }

        if (relativeMoveProperty != null)
        {
            relativeMoveProperty.boolValue = true;
        }

        serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(playerController);
    }

    private static void ConfigureAimTargets(Transform cameraTransform)
    {
        foreach (AimTarget aimTarget in Object.FindObjectsOfType<AimTarget>(true))
        {
            SerializedObject serializedAim = new SerializedObject(aimTarget);
            serializedAim.FindProperty("cameraTransform").objectReferenceValue = cameraTransform;
            serializedAim.FindProperty("distance").floatValue = 30f;
            serializedAim.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(aimTarget);
        }
    }

    private static void DisableCinemachineForDirectCamera(Camera mainCamera)
    {
        foreach (CinemachineBrain brain in mainCamera.GetComponents<CinemachineBrain>())
        {
            brain.enabled = false;
            EditorUtility.SetDirty(brain);
        }

        foreach (CinemachineVirtualCameraBase virtualCamera in Object.FindObjectsOfType<CinemachineVirtualCameraBase>(true))
        {
            virtualCamera.enabled = false;
            EditorUtility.SetDirty(virtualCamera);
        }
    }
}
