# Terramon Organization Guide

This document outlines the organizational structure for the Terramon project, following TML best practices for scalability and maintainability.

## Core Principles

1. **Four Root Elements**: Core, Common, Content, Utilities
2. **Maximum 3 Namespace Levels**: e.g., `Terramon.Common.Pokemon` ✓, `Terramon.Common.Pokemon.Capture.Systems` ✗
3. **Organize by Mechanic, Not Type**: Group related functionality together
4. **No God Classes**: Split `TerramonPlayer`, `TerramonGlobalItem` into multiple focused globals/systems
5. **Component-Based Approach**: Use boolean flags to make reusable behaviors
6. **Collocate Related Types**: Keep small supporting types in the same file as their primary type

---

## Current Issues & Solutions

### 🔴 Problem: God Classes in Core/

**Current:**
- `TerramonPlayer.cs` - Massive ModPlayer doing everything
- `TerramonGlobalItem.cs` - Should be multiple focused globals
- `TerramonWorld.cs` - Likely too broad

**Solution:**
Split these into focused globals organized by mechanic in `Common/`. Examples:
- `Common.Pokemon.PokemonPlayerStorage.cs` - Party and box management
- `Common.Pokemon.PokemonPlayerSpawning.cs` - NPC spawning behavior
- `Common.Pokemon.PokemonPlayerKeybinds.cs` - Input handling
- `Common.Pokemon.PokemonItemTooltips.cs` - Item tooltip modifications
- `Common.Pokemon.PokemonWorldState.cs` - World-specific state

### 🔴 Problem: Helpers/ Directory (Utility Dump)

**Current State:**
```
Helpers/
  BallAssets.cs          - Asset loading
  ShaderAssets.cs        - Asset loading
  ColorUtils.cs          - XNA utilities
  DrawUtils.cs           - XNA utilities
  ChestGen.cs            - Terraria utilities
  TileUtils.cs           - Terraria utilities
  LocalizationHelper.cs  - TModLoader utilities
  VanillaExtensions.cs   - Terraria utilities
  DiscordInviteBeamer.cs - ??? (Review for relevance)
  PrettySharp.cs         - ??? (Review)
```

**Solution:**
Reorganize by reference dependencies into `Utilities/`:

```
Utilities/
  Xna/
    ColorUtils.cs
    DrawUtils.cs
    SpriteBatchData.cs (struct from DrawUtils)
  
  Terraria/
    ChestGen.cs
    TileUtils.cs
    VanillaExtensions.cs
  
  TModLoader/
    LocalizationHelper.cs
```

Asset loading classes move to **`Core.Assets/`**:
```
Core/
  Assets/
    BallAssets.cs
    ShaderAssets.cs
```

### 🔴 Problem: Core/ Too Broad

**Current Core/ Contents:**
- DatabaseV2.cs
- ExperienceLookupTable.cs
- PCService.cs
- PokedexService.cs
- PokemonData.cs
- Tweening.cs
- TerramonItemAPI.cs
- Abstractions/
- Battling/
- Loaders/
- Systems/
- NPCComponents/
- ProjectileComponents/
- PokeBalls/

**Issues:**
- Services mixed with engine functionality
- Components in Core but Content in Content/
- Systems folder too generic

**Solution:**

#### Core/ (Engine-Level Only)
```
Core/
  Database/
    DatabaseV2.cs
    ExperienceLookupTable.cs
    PokemonData.cs
  
  Assets/
    BallAssets.cs
    ShaderAssets.cs
  
  Components/
    NPCComponent.cs
    NPCComponentExtensions.cs
    NPCAIComponent.cs
    ProjectileComponent.cs
    ProjectileComponentExtensions.cs
  
  Loaders/
    PokemonEntityLoader.cs
    TerramonItemLoader.cs
    PokeBallLoader.cs (from Core.PokeBalls/)
    UILoading/ (keep as is)
  
  Tweening/
    Tween.cs (renamed from Tweening.cs)
    ITweener.cs
    Tweener.cs
    (other tweening support types)
```

#### Move to Common/
```
Common/
  Pokemon/
    PokemonCapture.cs (behavior logic)
    PokemonSpawning.cs
    PokemonStorage.cs (combines PCService functionality)
    PokemonPokedex.cs (combines PokedexService functionality)
    PokemonPlayerParty.cs (extracted from TerramonPlayer)
    PokemonItemSets.cs (from TerramonItemAPI.Sets)
    PokemonNPCBehavior.cs
    PokemonNPCVisuals.cs
    PokemonProjectileVisuals.cs
    PCBox.cs (supporting type)
    PokedexEntry.cs (supporting type)
  
  Battle/
    BattleInstance.cs (from Core.Battling/)
    BattleClient.cs
    BattleManager.cs
    BattleField.cs
    BattleSide.cs
    BattlePokemon.cs
    BattleAction.cs
    BattleError.cs
    IBattleProvider.cs
    TurnBasedBattleSystem.cs (from Core.Systems/)
    Packets/
      (move BattlePackets here)
  
  Realtime/
    RealtimeCombatSystem.cs (from Core.Systems.RealtimeCombatSystem/)
    (related components)
  
  PokeBalls/
    ModPokeBall.cs (from Core.PokeBalls/)
    PokeBallCapture.cs (capture logic)
```

### 🔴 Problem: Misplaced Systems

**Current:**
```
Core/
  Systems/
    AnimatedIconSystem.cs
    CrossModSystem.cs
    KeybindSystem.cs
    MOTDSystem.cs
    RecipeSystem.cs
    TreeDropsSystem.cs
    TurnBasedBattleSystem.cs
    PokemonDirectUseSystem/
    RealtimeCombatSystem/
```

**Solution:**
Move to Common/ and organize by feature area:

```
Common/
  Pokemon/
    PokemonDirectUse.cs (from PokemonDirectUseSystem)
    PokemonTreeDrops.cs (from TreeDropsSystem)
    PokemonRecipes.cs (from RecipeSystem)
  
  Battle/
    TurnBasedBattleSystem.cs
  
  Realtime/
    RealtimeCombatSystem.cs
  
  UI/
    AnimatedIconSystem.cs
    KeybindSystem.cs
  
  Integration/
    CrossModSystem.cs
    MOTDSystem.cs
```

### 🔴 Problem: Content.NPCs Organization

**Current:**
```
Content/
  NPCs/
    NPCBounceBehaviour.cs
    NPCSpawnController.cs
    NPCTransform.cs
    NPCVariants.cs
    NPCVisuals.cs
    NPCWalkingBehaviour.cs
    NPCWanderingHoverBehaviour.cs
    PokemonNPC.cs
    Modifications/
    Town/
    TownModifications/
```

**Solution:**
Move behaviors to Common:
```
Common/
  Pokemon/
    PokemonNPCMovement.cs
      - WalkingBehavior (component)
      - WanderingHoverBehavior (component)
      - BounceBehavior (component)
    PokemonNPCVisuals.cs
      - VisualComponent
      - TransformComponent
      - VariantComponent
    PokemonNPCSpawning.cs
```

Keep only entity declarations in Content:
```
Content/
  NPCs/
    Pokemon/
      BulbasaurNPC.cs
      CharmanderNPC.cs
      SquirtleNPC.cs
      ...
    Town/
      ProfessorOakNPC.cs
      NurseJoyNPC.cs
      ...
```

---

## Recommended Structure

### Core/ - Engine Functionality
Foundation systems that power the mod. No game-specific behavior.

```
Core/
  Database/          - Pokemon/move data storage
  Assets/            - Asset loading/caching
  Components/        - Component system infrastructure
  Loaders/           - Content loaders
  Tweening/          - Animation/interpolation engine
```

### Common/ - Game Behavior Implementation
Behavior systems organized by game mechanic, not by TML type.

```
Common/
  Pokemon/           - Pokemon mechanics (capture, evolution, storage, etc.)
  Battle/            - Turn-based battle system
  Realtime/          - Real-time combat
  PokeBalls/         - Pokeball behavior
  UI/                - UI systems
  WorldGen/          - World generation behaviors
  Integration/       - Cross-mod integration
```

**Key Point:** Each folder contains ALL classes needed for that mechanic:
- `Common.Pokemon.PokemonCapture.cs` might contain:
  - `PokemonCaptureGlobal : GlobalProjectile`
  - `PokemonCapturePlayer : ModPlayer`
  - `CaptureCalculation` (struct)
  - `CaptureResult` (enum)

### Content/ - Entity Declarations
Only data and entity definitions. Minimal logic.

```
Content/
  Items/
    PokeBalls/       - PokeBall item declarations
    HeldItems/       - Held item declarations
    KeyItems/        - Key item declarations
    ...
  
  NPCs/
    Pokemon/         - Individual Pokemon NPC classes
    Town/            - Town NPC classes
  
  Projectiles/       - Projectile entity declarations
  Tiles/             - Tile declarations
  Buffs/             - Buff declarations
  
  GUI/               - UI element implementations
  Particles/         - Particle type declarations
  Configs/           - Mod configuration
```

### Utilities/ - Pure Utilities
Helper methods organized by what they reference.

```
Utilities/
  Xna/               - Microsoft.Xna.Framework utilities
  Terraria/          - Terraria utilities
  TModLoader/        - tModLoader utilities
```

---

## Migration Plan

### Phase 1: Split God Classes
1. **TerramonPlayer** → Multiple ModPlayers in Common/
   - Review every method/field
   - Group by mechanic (Pokemon, Battle, UI, etc.)
   - Create focused ModPlayers in appropriate Common/ folders
   - Use IBattleProvider interface pattern where needed

2. **TerramonGlobalItem** → Multiple GlobalItems in Common/
   - Pokemon item behaviors → `Common.Pokemon.PokemonItemBehavior`
   - Tooltip modifications → `Common.Pokemon.PokemonItemTooltips`
   - Held item logic → `Common.Pokemon.HeldItemBehavior`

### Phase 2: Reorganize Helpers/
1. Create `Utilities/Xna/`, `Utilities/Terraria/`, `Utilities/TModLoader/`
2. Move files based on primary references
3. Move asset loaders to `Core.Assets/`
4. Delete or archive deprecated utilities

### Phase 3: Reorganize Core/
1. Create `Core.Database/`, `Core.Assets/`, `Core.Components/`, `Core.Tweening/`
2. Move appropriate files
3. Move services to `Common/`
4. Move systems to `Common/`

### Phase 4: Reorganize Common/
1. Create mechanic-based folders
2. Move Core.Battling/ → Common.Battle/
3. Move Core.Systems/ classes to appropriate Common/ folders
4. Move behavior implementations from Content/ to Common/

### Phase 5: Clean Content/
1. Keep only entity declarations
2. Move all behavior to Common/
3. Organize by content type, then by subcategory

---

## Naming Conventions

### Files
- **Common/**: Describe the behavior/mechanic: `PokemonCapture.cs`, `BattleManager.cs`
- **Content/**: Entity name: `BulbasaurNPC.cs`, `PokeBallItem.cs`
- **Core/**: Infrastructure name: `PokemonEntityLoader.cs`, `ComponentSystem.cs`
- **Utilities/**: Utility purpose: `ColorUtils.cs`, `ChestGen.cs`

### Classes
- **No "Terramon" prefix** on classes inside the Terramon namespace
- **Specific names**: `PokemonCapturePlayer` not `TerramonPlayer`
- **Behavior suffix where appropriate**: `PokemonItemBehavior`, `NPCSpawningController`

### Namespaces
Format: `Terramon.<Root>.<Mechanic>`

Examples:
- ✓ `Terramon.Common.Pokemon`
- ✓ `Terramon.Common.Battle`
- ✓ `Terramon.Core.Database`
- ✓ `Terramon.Content.Items.PokeBalls`
- ✓ `Terramon.Utilities.Xna`
- ✗ `Terramon.Common.Pokemon.Capture.Systems` (4 levels)
- ✗ `Terramon.Core.Systems` (too generic)
- ✗ `Terramon.Common.GlobalProjectiles` (organized by type)

---

## Component Pattern Example

Instead of inheritance, use components with boolean flags:

**Before (Bad):**
```csharp
// Content/NPCs/BulbasaurNPC.cs
public class BulbasaurNPC : PokemonNPC
{
    // Override 50 methods
}

// Content/NPCs/CharmanderNPC.cs  
public class CharmanderNPC : PokemonNPC
{
    // Override 50 methods
}
```

**After (Good):**
```csharp
// Common/Pokemon/PokemonNPCBehavior.cs
public class PokemonNPCBehavior : GlobalNPC
{
    public bool Enabled = false;
    public MovementType Movement;
    // Instance data for customization
}

// Content/NPCs/Pokemon/BulbasaurNPC.cs
public class BulbasaurNPC : ModNPC
{
    public override void SetDefaults()
    {
        var behavior = NPC.GetGlobalNPC<PokemonNPCBehavior>();
        behavior.Enabled = true;
        behavior.Movement = MovementType.Walking;
        // Configure with data
    }
}
```

---

## Anti-Patterns to Avoid

### ❌ Organization by Type
```
Common/
  GlobalItems/
    PokemonItemGlobal.cs
    HeldItemGlobal.cs
  GlobalProjectiles/
    CaptureProjectileGlobal.cs
  ModPlayers/
    PokemonPlayer.cs
```

### ✓ Organization by Mechanic
```
Common/
  Pokemon/
    PokemonItemBehavior.cs (GlobalItem)
    PokemonCapture.cs (GlobalProjectile + helper types)
    PokemonPlayerParty.cs (ModPlayer)
    HeldItemBehavior.cs (GlobalItem)
```

### ❌ Deep Nesting
```
Terramon.Common.Pokemon.Capture.Systems.Projectiles
```

### ✓ Flat Structure
```
Terramon.Common.Pokemon (capture logic lives here)
```

### ❌ Too Many Small Files
```
DecalSystem.cs
Decal.cs
DecalData.cs
DecalLayer.cs
ChunkDecals.cs
DecalRenderer.cs
```

### ✓ Cohesive Files
```
DecalSystem.cs (contains all above types, organized small→large)
```

---

## Quick Reference

| What | Where | Why |
|------|-------|-----|
| Pokemon capture logic | `Common.Pokemon.PokemonCapture` | Behavior implementation |
| Battle instance management | `Common.Battle.BattleInstance` | Behavior implementation |
| Pokeball item declaration | `Content.Items.PokeBalls.PokeBallItem` | Entity declaration |
| Pokemon NPC declaration | `Content.NPCs.Pokemon.BulbasaurNPC` | Entity declaration |
| Component system | `Core.Components.NPCComponent` | Engine infrastructure |
| Database loading | `Core.Database.DatabaseV2` | Engine infrastructure |
| Color utilities | `Utilities.Xna.ColorUtils` | XNA Framework utilities |
| Chest generation | `Utilities.Terraria.ChestGen` | Terraria API utilities |
| Asset caching | `Core.Assets.BallAssets` | Engine infrastructure |
| Experience calculation | `Core.Database.ExperienceLookupTable` | Engine data |

---

## Benefits of This Structure

1. **Scalability**: Adding new Pokemon mechanics doesn't clutter unrelated areas
2. **Discoverability**: Related code lives together, not scattered by type
3. **Maintainability**: Changes to a mechanic touch one folder, not 5+ folders
4. **Team-Friendly**: Multiple developers can work on different mechanics without conflicts
5. **Performance**: No impact - organization is compile-time only
6. **Namespace Clarity**: Path matches namespace exactly

---

## Questions During Refactoring

When moving a file, ask:

1. **Is this engine infrastructure or behavior?** → Core or Common
2. **Is this entity declaration or behavior?** → Content or Common
3. **What mechanic does this support?** → Determines Common subfolder
4. **What does this reference?** → Determines Utilities subfolder
5. **Is this reusable outside the mechanic?** → Maybe Core

When creating a file, ask:

1. **Does this need multiple TML types?** → Keep them together
2. **Will this have supporting types?** → Keep them in same file
3. **Is the namespace 3 levels or less?** → If not, flatten

---

*Last Updated: December 22, 2025*
