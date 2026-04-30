# Project Architecture & Systems

This document defines the core architectural patterns and folder structures for this project. To ensure high code quality and maintainability within a 6-week timeframe, we strictly avoid DOTS/ECS and focus on a heavily decoupled Component-based architecture.

## 1. Core Architectural Patterns

### A. Event-Driven Architecture (Observer Pattern)
- **Decoupling is Priority #1:** Scripts must NOT tightly couple to each other. Use C# `event`, `Action`, or `UnityEvent` to broadcast state changes.
- **Example Context:** If the player makes a sound, the `PlayerController` should invoke an `OnNoiseGenerated` event. Enemy scripts will subscribe to this event. The player should NEVER explicitly search for or call methods directly on enemies.

### B. Scriptable Object (SO) Data-Driven Design
- Use Scriptable Objects to store game data, item stats, puzzle configurations, and shared variables.
- This allows Level Designers to tweak puzzle variables and enemy stats without touching the code.

### C. Interfaces for Gimmicks & Puzzles
- All interactable map elements (doors, levers, items) MUST implement shared interfaces.
- **Primary Interface:** `IInteractable`. Any script handling player interaction will simply call `IInteractable.Interact()`, completely agnostic to what the object actually is.

### D. Minimal Singleton Pattern
- Singletons (`Instance`) are permitted ONLY for global persistence systems (e.g., `GameManager`, `SoundManager`). 
- Do NOT use Singletons for scene-specific logic (like a `PuzzleManager` specific to level 1).

## 2. Directory Structure (`Assets/`)
Maintain this exact folder structure when creating new files:

- `/Scripts/`
  - `/Core/` (Managers, global systems, Game State)
  - `/Player/` (Player movement, stealth logic, input handling)
  - `/Enemies/` (AI, field of view, state machines)
  - `/Interactables/` (Puzzle logic, doors, switches, `IInteractable` implementations)
  - `/Data/` (Scriptable Object scripts defining stats and configurations)
- `/Prefabs/` (Reusable objects, properly configured with components)
- `/ScriptableObjects/` (The actual SO asset instances created in the editor)

## 3. Dependency Rules
- **UI Logic:** UI scripts must ONLY read data or listen to events. UI should never contain core business/game logic.
- **Physics vs Logic:** Physics logic (Raycasts for stealth sight, movement) resides strictly in `FixedUpdate()`.