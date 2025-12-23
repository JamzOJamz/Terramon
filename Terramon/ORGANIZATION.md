# Terramon Architecture Guide

This document explains how the Terramon codebase is structured and **where new code should go**.

We use **four namespaces with strict, non-overlapping responsibilities**:

- `Content` - Everything related to mod content definitions, such as Pokémon, items, moves, and NPCs.
- `Core` - The foundational systems that power the mod (like game mechanics)
- `Common` - Shared code that is used by multiple systems but does not fit into the other namespaces.
- `Utilities` - Stateless tools and helper functions that can be used across the mod without side effects.

Understanding these boundaries is essential for keeping the project maintainable as it grows.

---

## Content

**What it is:** Everything that directly appears in the game world or provides gameplay-facing interfaces.

**Examples include:**
- Items
- NPCs
- Projectiles
- Tiles
- Buffs and debuffs
- Gameplay UI

**Rules:**
- Must represent an in-game entity or gameplay-interactable interface
- Registered through tModLoader base classes (`ModItem`, `ModNPC`, `ModProjectile`, etc.)
- Should typically **not own systems or global state**, though exceptions exist where it makes sense (use best judgment)

**Rule of thumb:** If the player can see or interact with it → `Content`

---

## Core

**What it is:** The machinery that makes the mod function.

**Examples include:**
- Networking and packet handlers
- Progression systems
- Asset loaders and registrars
- Managers
- Anything that hooks into mod loading/unloading

**Rules:**
- May hook purposefully into mod loading/unloading
- May register things, cache assets, or maintain state
- Should **not** be instantiated arbitrarily; these are singletons or framework-managed

**Rule of thumb:** If it controls when, how, or whether something exists → `Core`

---

## Common

**What it is:** Shared definitions and contracts that give the mod a common vocabulary. Purely definitional with no behavior or side effects.

**Examples include:**
- IDs
- Interfaces
- Base or abstract classes not best included elsewhere
- Data models and records
- Enums
- Semantic constants

**Rules:**
- No direct load/unload side effects
- No registration logic
- No dependency on mod lifecycle
- Safe to reference at any time, even during static initialization
- Does not "do" things—it "is" things

**Rule of thumb:** If it defines meaning or structure but does not perform work on its own → `Common`

---

## Utilities

**What it is:** Stateless computation tools. Pure functions that could theoretically live in any codebase.

**Examples include:**
- Math helpers
- Geometry helpers
- Drawing utilities
- Extension methods
- Formatting helpers

**Rules:**
- Must be **stateless**—no global variables, no caching, no side effects
- Should be usable anywhere without setup
- Should be reusable helpers (may reference Terramon types when needed)
- Focuses on "how to compute" rather than "what to compute"

**Rule of thumb:** If it's a reusable calculation or transformation with no context → `Utilities`

---

## Decision Flowchart

When adding new code, ask yourself:

1. **Does the player see or interact with it directly?**  
   → `Content`

2. **Does it manage lifecycle, state, or orchestration?**  
   → `Core`

3. **Does it define structure, IDs, or contracts without behavior?**  
   → `Common`

4. **Is it a stateless helper function?**  
   → `Utilities`

---

## Final Notes

This architecture exists mainly to:
1. Allow new contributors to find where code belongs
2. Keep the mod maintainable as it scales

When in doubt, ask: **"What is this code's job?"** and place it in the namespace that matches that responsibility.

---

I'm open to ideas and changes to this! Let me know if something doesn't make sense. — Jamz

*Last Updated: December 23, 2025*