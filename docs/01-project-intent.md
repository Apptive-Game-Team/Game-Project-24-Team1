# Project Intent & Core Scope

This document defines the core philosophy, genre, and boundaries of the project. You MUST use this context to understand the intention behind my prompts and write appropriate, scoped code.

## 1. Project Philosophy & Constraints
- **Timeline:** This is a short-term project developed over exactly **6 weeks**.
- **Code Quality (Industry Standard):** Despite the short timeline, the primary goal is to write code that adheres to **industry standards**. 
  - Do NOT write "hacky" or tightly coupled code just to get things working fast. 
  - Prioritize modularity, scalability, and clean architecture (e.g., using interfaces, event-driven programming, and decoupled components).
- **Scope Control:** Always suggest the most elegant but minimal solution. Do not over-engineer features (like complex RPG inventory systems) unless explicitly requested.

## 2. Core Gameplay & Genre
- **Genre:** 3D Stealth & Puzzle Game.
- **Vibe/Atmosphere:** Tense and tactical. The player must observe the environment and solve situations using logic and stealth rather than direct combat.
- **Naming Context:** When generating code, use context-appropriate variable names (e.g., use `VisibilityScore`, `DetectionRadius`, or `SuspicionLevel` rather than generic RPG terms like `Aggro` or `Threat`).

## 3. Core Mechanics to Anticipate
When writing scripts, anticipate that they will interact with these core systems:
- **Stealth Mechanics:** Enemy Field of View (FOV), noise/sound detection, hiding spots, and player visibility calculation.
- **Map Gimmicks & Puzzles:** Interactable objects (levers, pressure plates, doors), physical puzzles, and logic gates that require the player to manipulate the environment.
- **Player Movement:** 3D character movement focused on crouching, walking silently, and navigating tight spaces.

## 4. AI Action Directives
- **Component Reusability:** When I ask you to create a "puzzle gimmick" (like a button or a trap), ALWAYS design it as a reusable component (e.g., using Unity Events or Interfaces like `IInteractable`) so level designers can easily mix and match them in the Inspector.
- **No Combat Bias:** Unless stated otherwise, assume enemies cannot be killed directly. Focus on avoidance, distraction, and evasion mechanics.