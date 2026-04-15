# Bannerlord.Cannons

Artillery mechanics module for Mount & Blade II: Bannerlord. Provides script components, ballistics, AI, and animation systems for fielding cannon-type siege weapons in both siege and open-battle missions.

**Version:** 0.4.1 | **Supported game versions:** v1.2.12

---

## Table of contents

1. [Overview](#overview)
2. [Dependencies](#dependencies)
3. [Scene setup](#scene-setup)
4. [Configurable fields reference](#configurable-fields-reference)
5. [Code architecture](#code-architecture)
6. [Integration guide](#integration-guide)
7. [Design notes: lazy-evaluated component parameters](#design-notes-lazy-evaluated-component-parameters)

---

## Overview

Bannerlord.Cannons adds a fully functional artillery weapon type to Bannerlord. A cannon requires a crew of two: a **pilot** (aimer/gunner) and a **reloader**. The reloader fetches cannonballs from an ammo pile, loads the cannon, and after firing assists with pushing the cannon back into firing position. The AI handles crew assignment, targeting, and push-back automatically in both siege and field battle contexts.

Key features:
- Ballistic projectile trajectory with optional high/low angle preference
- Recoil animation with ease-out cubic back-slide and smoothstep return driven by push crew
- Wheel spin animation derived from body velocity for physical plausibility
- Fire effects (sound, particle) and fire safety check (prevents shooting through crew)
- Siege AI uses native `RangedSiegeWeaponAi`; field battle AI uses custom `FieldBattleWeaponAI` with formation-based crew management
- Zero air friction for cannonballs (Harmony patch on `ItemObject.GetAirFrictionConstant`)

---

## Dependencies

| Module | Minimum version |
|---|---|
| `Bannerlord.Harmony` | v2.2.2 |
| `Native` | — |
| `SandBoxCore` | — |
| `Sandbox` | — |
| `StoryMode` | — |
| `CustomBattle` | — |

---

## Scene setup

A cannon in a scene consists of two entities:

### 1. The cannon prefab entity

The prefab must have an `ArtilleryRangedSiegeWeapon` subclass script attached (e.g. `GenericCannon` from DaDG). The entity hierarchy must contain child objects with the following tags for the script to locate them at init:

| Tag | Purpose |
|---|---|
| `Battery_Base` | Main cannon body — receives recoil animation |
| `Barrel` | Barrel sub-entity — receives aim angle |
| `Wheel_L` | Left wheel — receives spin animation |
| `Wheel_R` | Right wheel — receives spin animation |

Standing points on the prefab entity drive crew interactions:

| Tag (default) | Role |
|---|---|
| `Pilot` standing point | Gunner/aimer seat |
| `ammo_load` | Loader's position while ramming a charge |
| `ammo_pickup` | Position to pick up a cannonball |
| `push_cannon` (configurable) | One or two positions for push-back crew |
| `wait` (configurable via `WaitStandingPointTag`) | Loader's idle position between shots |

A **CannonBallPile** entity must be placed nearby with ammo pickup standing points. The reloader navigates to it automatically.

### 2. The spawner entity (when using the DaDG spawner pattern)

Place a `GenericCannonSpawner` script on a scene entity and set:
- **Team** — `Attacker` or `Defender`
- All desired cannon overrides (see [Configurable fields reference](#configurable-fields-reference))

At mission start, the spawner instantiates the cannon prefab and forwards the configured field values to the cannon instance.

---

## Configurable fields reference

All fields below are public on `ArtilleryRangedSiegeWeapon` and visible in the scene editor when the script is directly on an entity. When using the DaDG spawner, they are also exposed on `GenericCannonSpawner` via dynamic type emission.

### Animation action names

| Field | Default | Description |
|---|---|---|
| `IdleActionName` | — | Skeleton action played when idle |
| `ShootActionName` | — | Action played on fire |
| `Reload1ActionName` | — | First reload phase animation |
| `Reload2ActionName` | — | Second reload phase animation |
| `RotateLeftActionName` | — | Traversal left animation |
| `RotateRightActionName` | — | Traversal right animation |
| `LoadAmmoBeginActionName` | — | Loader begins ramming |
| `LoadAmmoEndActionName` | — | Loader finishes ramming |
| `Reload2IdleActionName` | — | Idle between reload phases |
| `PushActionName` | — | Crew push-back animation |

### Physics and recoil

| Field | Default | Description |
|---|---|---|
| `BaseMuzzleVelocity` | `40` | Projectile launch speed (m/s). Affects range and AI targeting |
| `RecoilDuration` | `0.8` | Time (s) for back-slide phase after firing |
| `Recoil2Duration` | `0.8` | Time (s) for return phase after push-back |
| `RecoilDistance` | `0.6` | Distance (m) the cannon body slides back. Takes priority over `SlideBackFrameFactor` |
| `SlideBackFrameFactor` | `0.6` | Legacy: distance multiplier for slide-back. Ignored when `RecoilDistance > 0` |
| `WheelRadius` | `0.3` | Wheel radius (m). Determines spin speed relative to body velocity |
| `WheelRotationAxis` | `"X"` | Axis wheels rotate around: `"X"` (side) or `"Y"` (forward) |
| `PreferHighAngle` | `false` | When true, AI and player use the high-arc ballistic solution |

### Audio and visual effects

| Field | Default | Description |
|---|---|---|
| `FireSoundID` | `"mortar_shot_1"` | Primary fire sound event ID |
| `FireSoundID2` | `"mortar_shot_2"` | Secondary fire sound event ID |
| `CannonShotExplosionEffect` | — | Particle effect name played at the muzzle on fire |

### Crew and standing points

| Field | Default | Description |
|---|---|---|
| `PushStandingPointTag` | `"push_cannon"` | Tag identifying standing points used by push-back crew. Supports one or two points |

### Display

| Field | Default | Description |
|---|---|---|
| `DisplayName` | `"Artillery"` | Name shown in targeting UI |

---

## Code architecture

```
ArtilleryRangedSiegeWeapon          ← main script component (scene-facing)
  └─ BaseFieldSiegeWeapon           ← ballistics, fire safety, side/team assignment
       └─ RangedSiegeWeapon         ← Bannerlord native base

Internal components (created in OnInit, owned by ArtilleryRangedSiegeWeapon):
  RecoilEffect        — eased back-slide + smoothstep return animation
  WheelAnimator       — per-frame wheel spin derived from body linear velocity
  FireEffectsPlayer   — sound and particle playback
  AmmoPickupHandler   — routes reloader to cannonball pile and back
  AmmoLoadHandler     — manages the loading animation sequence
  AmmoPointController — forces ammo point activation states
  AIFormationManager  — assigns infantry formations as crew via IArtilleryCrewProvider
  PushDrivenReturn    — coordinates push-back phase and return transition
```

### AI

| Context | Class | Mechanism |
|---|---|---|
| Siege | `FieldSiegeWeaponAI` (wraps native `RangedSiegeWeaponAi`) | Native AI calls `AimAtThreat` → Harmony patch intercepts `ShootProjectileAux` and applies `LastAiLaunchVector` |
| Field battle | `FieldBattleWeaponAI` | Custom AI selects target via `TargetingPolicy`, computes ballistic release angle, sets `Target.SelectedWorldPosition`; Harmony patch applies on shoot |

### Ballistics

`Ballistics` (static) and `BallisticsService` solve the standard 2-solution projectile problem:

```
speed⁴ - g(g·d² + 2·h·speed²) ≥ 0  →  low angle and high angle solutions
```

`GetTargetReleaseAngle` returns the appropriate solution based on `PreferHighAngle`. The Harmony patch on `ShootProjectileAux` overrides Bannerlord's native projectile launch to use the computed direction, ensuring cannonballs follow true ballistic arcs at any muzzle velocity.

A second Harmony patch on `ItemObject.GetAirFrictionConstant` zeroes air friction for `WeaponClass.Boulder`, preventing drag from reducing range.

### Harmony patches (`ArtilleryPatches`)

| Patch | Target | Effect |
|---|---|---|
| Prefix | `RangedSiegeWeapon.ShootProjectileAux` | Replaces native shot with `Mission.AddCustomMissile` using ballistic launch vector |
| Postfix | `ItemObject.GetAirFrictionConstant` | Returns `0` for `WeaponClass.Boulder` |

---

## Integration guide

### Standalone (Bannerlord.Cannons as a module dependency)

1. Add `Bannerlord.Cannons` to your `SubModule.xml` `<DependedModules>`.
2. Create a subclass of `ArtilleryRangedSiegeWeapon` and override `GetSiegeEngineType()`.
3. Place the subclass script on your cannon prefab entity and tag child objects as described in [Scene setup](#scene-setup).

### Embedded (DaDG integration via `CannonSystemInitialiser`)

DaDG does not load Bannerlord.Cannons as a separate module. Instead it calls:

```csharp
CannonSystemInitialiser.Initialise();
```

from its own `SubModule` constructor. This injects the Harmony patches without requiring Bannerlord.Cannons to register as a Bannerlord module. `GenericCannon` (DaDG) extends `SpawnableArtilleryRangedSiegeWeapon` which extends `ArtilleryRangedSiegeWeapon`.

---

## Design notes: lazy-evaluated component parameters

### Background

DaDG places cannons in scenes via a spawner: `GenericCannonSpawner` sits on the scene entity, and the `GenericCannon` script lives on a spawned prefab. Sceners configure cannon parameters on the spawner; `AssignParameters()` (called by Bannerlord's `SpawnerEntityMissionHelper` after prefab instantiation) copies those values to the cannon.

The problem: `AssignParameters()` runs **after** `OnInit()` on the cannon. By the time field values are copied over, `RecoilEffect` and `WheelAnimator` have already been constructed with the original field values captured as plain copies:

```csharp
// Before — values captured once at construction, stale after AssignParameters()
_recoilEffect = new RecoilEffect(body, wheelAnimator,
    RecoilDuration, Recoil2Duration, ResolveRecoilDistance(), WheelRadius);

_wheelAnimator = new WheelAnimator(wheelL, wheelR, _wheelRotationAxis);
```

Setting `RecoilDuration = 1.5f` on the cannon post-init had no effect on the already-constructed `RecoilEffect`.

### Solution

`RecoilEffect` and `WheelAnimator` now store `Func<T>` delegates that read the current field value on each invocation:

```csharp
// After — values read from the cannon instance each time they are needed
_recoilEffect = new RecoilEffect(body, wheelAnimator,
    () => RecoilDuration,
    () => Recoil2Duration,
    ResolveRecoilDistance,
    () => WheelRadius);

_wheelAnimator = new WheelAnimator(wheelL, wheelR, GetWheelRotationAxis);
```

`GetWheelRotationAxis()` replaces the one-time `Enum.TryParse` that was previously in `OnInit()`, re-parsing the public `WheelRotationAxis` string field on every call:

```csharp
private WheelRotationAxis GetWheelRotationAxis() =>
    Enum.TryParse(WheelRotationAxis, out WheelRotationAxis axis)
        ? axis
        : WheelRotationAxis.X;
```

> Note: `WheelRotationAxis` is both the name of the public string field and the enum type. The fully qualified type is used inside `GetWheelRotationAxis()` to avoid the name collision.

### Effect on spawner overrides

| Field | Component | Applied from |
|---|---|---|
| `RecoilDuration` | `RecoilEffect.Update()` | Next shot |
| `Recoil2Duration` | `RecoilEffect.UpdateReturn()` | Next return phase |
| `RecoilDistance` / `SlideBackFrameFactor` | `RecoilEffect.Begin()` | Next shot |
| `WheelRadius` | `RecoilEffect.Update()`, `UpdateReturn()` | Next shot |
| `WheelRotationAxis` | `WheelAnimator.Rotate()` | Next tick |

Fields that are already read dynamically (not captured into components) — `BaseMuzzleVelocity`, `DisplayName`, `FireSoundID`, all animation action names, `PushStandingPointTag` — required no changes and work correctly with spawner overrides as-is.