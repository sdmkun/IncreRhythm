# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**IncreRhythm** is a Unity 6 game project (version 6000.0.58f2) configured with:
- Universal Render Pipeline (URP) 2D
- Unity Input System
- 2D Pixel Perfect support

## Project Structure

```
Assets/
├── Animations/       # Animation clips and controllers
├── Audio/           # Audio files and mixers
├── Materials/       # Material assets
├── Prefabs/         # Reusable game objects
├── Scenes/          # Unity scene files
├── Scripts/         # C# game scripts (currently empty)
├── Settings/        # URP and rendering settings
└── Textures/        # Sprite sheets and textures
```

## Unity-Specific Development

### Opening and Running the Project

1. Open the project in Unity Hub or directly in Unity 6000.0.58f2
2. Main scene: `Assets/Scenes/SampleScene.unity`
3. Press Play in the Unity Editor to test

### Input System

The project uses Unity's new Input System (not the legacy Input Manager). Input actions are defined in:
- `Assets/InputSystem_Actions.inputactions`

Key input action maps:
- **Player**: Move, Look, Attack, Interact, Crouch, Jump, Sprint, Previous, Next
- **UI**: Navigate, Submit, Cancel, Point, Click, RightClick, MiddleClick, ScrollWheel

Supports multiple control schemes: Keyboard & Mouse, Gamepad, Touch, Joystick, XR

### Rendering Pipeline

Uses Universal Render Pipeline (URP 17.0.4) configured for 2D rendering:
- Renderer asset: `Assets/Settings/Renderer2D.asset`
- URP settings: `Assets/Settings/UniversalRP.asset`
- Global settings: `Assets/UniversalRenderPipelineGlobalSettings.asset`

## Key Unity Packages

From `Packages/manifest.json`:
- `com.unity.feature.2d` - 2D feature set
- `com.unity.inputsystem` - New Input System
- `com.unity.render-pipelines.universal` - URP
- `com.unity.timeline` - Timeline for cutscenes/sequences
- `com.unity.visualscripting` - Visual scripting tools

## Version Control

Standard Unity .gitignore is configured to exclude:
- `/Library/` - Unity's internal asset database
- `/Temp/` - Temporary build files
- `/Obj/` - Compiled object files
- `/Build/` and `/Builds/` - Build output
- `/Logs/` - Unity logs
- `/UserSettings/` - User-specific settings

## Notes for Development

- All gameplay scripts should go in `Assets/Scripts/`
- This is a 2D project - use 2D physics and components
- When creating new Input Actions, modify `InputSystem_Actions.inputactions` in the Unity Editor
- URP shader compatibility: use URP-compatible shaders (not Built-in RP shaders)
