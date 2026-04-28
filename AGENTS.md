# Mage Project Guide

This is a Unity 2022.3.22f1 project using URP 14, Input System, Cinemachine, TextMeshPro, Visual Scripting, and Unity Test Framework.

- Keep project-owned gameplay work mainly in `Assets/Wizard` and project scenes in `Assets/Scenes`.
- Treat large imported asset packs as third-party content unless the task explicitly targets them.
- Do not edit Unity generated folders: `Library`, `Temp`, `Logs`, `UserSettings`, `Obj`, `Build`, or `Builds`.
- Use the project Unity MCP server when available. Codex config is in `.codex/config.toml`; the Unity bridge is expected on `127.0.0.1:7890`.
- Validate Unity work with compilation, console checks, tests, or MCP tools before handing it back.
- Keep build settings pointed at real project scenes, not missing third-party demo scenes.
