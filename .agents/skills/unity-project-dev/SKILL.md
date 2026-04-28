---
name: unity-project-dev
description: Project-specific Unity workflow for Mage. Use when developing, auditing, testing, or automating this Unity 2022.3 URP project, especially tasks involving Assets/Wizard, Assets/Scenes, Packages/manifest.json, ProjectSettings, Unity MCP, C# gameplay scripts, scenes, prefabs, input, camera, build settings, or editor automation.
---

# Mage Unity Project Workflow

Use this skill as the project onboarding path for `C:\Users\BRUNO\Documents\ProjetoTwin\Mage`.

## Project shape

- Treat `Assets/Wizard` and `Assets/Scenes` as the project-owned gameplay and scene area.
- Treat `Assets/PolygonDungeon`, `Assets/PolygonFantasyCharacters`, `Assets/Hovl Studio`, `Assets/MekaruStudios`, `Assets/Starter Assets`, `Assets/Samples`, `Assets/WizardPBR`, and `Assets/AY_Shader` as third-party or imported content unless the user says otherwise.
- Do not edit `Library`, `Temp`, `Logs`, or `UserSettings`.
- Prefer Unity 2022.3.22f1 compatibility unless the user asks to upgrade. The project uses URP 14, Input System, Cinemachine, TextMeshPro, Visual Scripting, and Unity Test Framework.

## Development flow

1. Inspect `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, and relevant scene or script files before changing behavior.
2. Keep gameplay scripts focused and serializable in the Unity Inspector. Use `[SerializeField] private` for new fields unless public API is needed.
3. Avoid broad asset-pack refactors. If third-party code must be touched, isolate the change and explain why.
4. For scene/build changes, verify paths exist and keep `ProjectSettings/EditorBuildSettings.asset` aligned with real `.unity` files.
5. Validate through Unity compilation, edit-mode tests, or the MCP console/compilation tools when available.

## Unity MCP

- The project config expects the AnkleBreaker Unity MCP plugin bridge on `127.0.0.1:7890`.
- Codex starts the MCP server with `npx -y anklebreaker-unity-mcp@latest`.
- The Unity Editor must open the project after `Packages/manifest.json` restores the MCP plugin. The plugin dashboard is under `Window > MCP Dashboard`.
- If MCP tools are unavailable, verify Node.js, Unity Editor, plugin restore, and the bridge ping at `http://127.0.0.1:7890/api/ping`.

## Audit priorities

- Broken build scene paths or missing scene GUIDs.
- Console compilation errors, malformed scripts, and mojibake in source comments.
- Input System or Cinemachine setup mismatches.
- URP material/shader compatibility after imported assets.
- Unused demo/sample scenes in build settings.
- MCP security: local-only bridge, no LAN binding unless explicitly requested.
