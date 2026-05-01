# Bannerlord.Cannons

Artillery mechanics module for Mount & Blade II: Bannerlord. Provides script components, ballistics, AI, and animation systems for fielding cannon-type siege weapons in both siege and open-battle missions.

**Version:** 1.0.0 | **Supported game versions:** v1.2.12

---

## Table of contents

1. [Overview](#overview)
2. [Dependencies](#dependencies)
3. [Scene setup](#scene-setup)
4. [Cannon configuration XML](#cannon-configuration-xml)
5. [Configurable fields reference](#configurable-fields-reference)
6. [Public API](#public-api)
7. [Client module initialization](#client-module-initialization)
8. [Code architecture](#code-architecture)
9. [Design notes: lazy-evaluated component parameters](#design-notes-lazy-evaluated-component-parameters)

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

## Cannon configuration XML

Client modules register cannons by shipping a `ModuleData/CustomXml/cannons.xml` file. Bannerlord.Cannons scans every loaded module for that path during startup, validates each file against the embedded schema, and registers the valid cannon definitions.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Cannons>
  <Cannon>
    <Id>greatcannon</Id>
    <DisplayName>Great Cannon</DisplayName>
    <SiegeDeploymentSelectionIconSpriteId>SPGeneral\SiegeEngine\siege_engine_cannon</SiegeDeploymentSelectionIconSpriteId>
    <MapSiegeMarkerSpriteId>SPGeneral\MapSiege\siege_engine_cannon</MapSiegeMarkerSpriteId>
    <CampaignMapSelectionIconSpriteId>SPGeneral\MapSiege\siege_engine_cannon</CampaignMapSelectionIconSpriteId>
    <CampaignMapPrefabName>campaign_siege_cannon</CampaignMapPrefabName>
    <CampaignMapProjectilePrefabName>campaign_cannonball</CampaignMapProjectilePrefabName>
    <CampaignMapReloadAnimationName>reload</CampaignMapReloadAnimationName>
    <CampaignMapFireAnimationName>fire</CampaignMapFireAnimationName>
    <MachineType>1</MachineType>
    <CampaignMapProjectileBoneIndex>0</CampaignMapProjectileBoneIndex>
    <IsDefensiveSiegeWeapon>true</IsDefensiveSiegeWeapon>
    <IsAttackerSiegeWeapon>true</IsAttackerSiegeWeapon>
  </Cannon>
</Cannons>
```

`IsDefensiveSiegeWeapon` and `IsAttackerSiegeWeapon` are required in 1.0.0. They control whether the cannon is offered to defenders, attackers, or both during siege deployment.

All properties are required. If a property is missing, empty, or invalid, the cannon definition is skipped during startup.

| Property | Type | Description |
|---|---|---|
| `Id` | string | Unique cannon id. Must match a loaded `SiegeEngineType` object id, because campaign availability resolves it with `MBObjectManager.GetObject<SiegeEngineType>(Id)`. Also used as the dynamic mission script type id. Must start with a letter and contain only letters, digits, or underscores. |
| `DisplayName` | string | User-facing cannon name used by the deployment icon registration. |
| `SiegeDeploymentSelectionIconSpriteId` | string | Sprite id used for the mission siege deployment/order UI button. Bannerlord.Cannons registers this as the icon state for the cannon's `DisplayName`. |
| `MapSiegeMarkerSpriteId` | string | Sprite id used for the campaign map siege point-of-interest marker after the cannon has been deployed or is being built. |
| `CampaignMapSelectionIconSpriteId` | string | Sprite id used for the campaign map siege deployment selection UI. |
| `CampaignMapPrefabName` | string | Campaign map prefab spawned for this siege engine. Returned from `SiegeEventModel.GetSiegeEngineMapPrefabName(...)` when the siege engine id matches this cannon. |
| `CampaignMapProjectilePrefabName` | string | Campaign map projectile prefab used by siege bombardment visualization. Returned from `GetSiegeEngineMapProjectilePrefabName(...)`. |
| `CampaignMapReloadAnimationName` | string | Animation name used by the campaign map siege engine prefab while reloading. Returned from `GetSiegeEngineMapReloadAnimationName(...)`. |
| `CampaignMapFireAnimationName` | string | Animation name used by the campaign map siege engine prefab while firing. Returned from `GetSiegeEngineMapFireAnimationName(...)`. |
| `MachineType` | int | Positive integer used as the campaign map UI machine-type discriminator for this cannon. Native Bannerlord uses fixed values for built-in siege engines; Bannerlord.Cannons patches the map-siege VM and widgets so this custom value can map back to `MapSiegeMarkerSpriteId`. Use a value that does not collide with native machine types or other custom cannons. |
| `CampaignMapProjectileBoneIndex` | int | Non-negative bone index on the campaign map siege engine prefab where the projectile should originate. Returned as an `sbyte` from `GetSiegeEngineMapProjectileBoneIndex(...)`. |
| `IsDefensiveSiegeWeapon` | bool | When `true`, the cannon is added to `GetAvailableDefenderSiegeEngines(...)` and can be selected by defenders on the campaign map. |
| `IsAttackerSiegeWeapon` | bool | When `true`, the cannon is added to `GetAvailableAttackerRangedSiegeEngines(...)` and can be selected by attackers on the campaign map. |

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

### 3. Optional trajectory preview entity

Attach `CannonTrajectoryVisualiser` to a child entity under a `GenericCannonSpawner` when sceners need an editor-only trajectory preview. Enable **ShowTrajectory** in the editor to draw Bannerlord's trajectory volume for the current spawner settings.

The visualiser reads its values from the parent spawner through `ICannonTrajectoryPreviewSource`:

| Source value | Used for |
|---|---|
| `BaseMuzzleVelocity` | Projectile speed used by the preview volume |
| Bottom/top release angle restrictions | Vertical trajectory bounds |
| `DirectionRestrictionDegrees` | Horizontal trajectory bounds |
| `projectile_leaving_position` child entity | Preview origin; falls back to the spawner origin when missing |

The generated preview holder is marked `DontSaveToScene`; it is a scene-editor aid, not a mission object.

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

### Aiming, physics, and recoil

| Field | Default | Description |
|---|---|---|
| `BaseMuzzleVelocity` | `40` | Projectile launch speed (m/s). Affects range and AI targeting |
| `PreferHighAngle` | `false` | When true, AI and player use the high-arc ballistic solution |
| `BottomReleaseAngleRestriction` | `-PI / 2` | Native lower vertical aiming limit, in radians. Used by aiming and trajectory preview |
| `TopReleaseAngleRestriction` | `PI / 2` | Native upper vertical aiming limit, in radians. Used by aiming and trajectory preview |
| `DirectionRestrictionDegrees` | `100` | Horizontal aiming arc, in degrees. Converted internally to Bannerlord's `DirectionRestriction` radians value |
| `RecoilDuration` | `0.8` | Time (s) for the recoil back-slide after firing |
| `PushDuration` | `0.8` | Time (s) for the crew push/return phase after the post-reload delay |
| `RecoilDistance` | `0.6` | Distance (m) the cannon body slides back during recoil |
| `WheelRotationAxis` | `X` | Enum axis used for wheel spin while recoiling and being pushed back: `X` or `Y` |

### Audio and visual effects

| Field | Default | Description |
|---|---|---|
| `FireSoundID` | `"mortar_shot_1"` | Primary fire sound event ID |
| `FireSoundID2` | `"mortar_shot_2"` | Secondary fire sound event ID |
| `CannonShotExplosionEffect` | — | Particle effect name played at the muzzle on fire |

### Crew and standing points

| Field | Default | Description |
|---|---|---|
| `WaitStandingPointTag` | `"Wait"` | Native standing-point tag used as the loader's waiting/push point between loading and the push-return phase |

### Display

| Field | Default | Description |
|---|---|---|
| `DisplayName` | `"Artillery"` | Name shown in targeting UI |

---

## Public API

Install the `Bannerlord.Cannons` NuGet package from client modules that need to query registered cannons or provide an external logger.

```xml
<PackageReference Include="Bannerlord.Cannons" Version="1.0.0" />
```

```csharp
using System.Linq;
using Bannerlord.Cannons.Api;
using Microsoft.Extensions.Logging;

public sealed class MyClientServices
{
    private readonly ICannonApi _cannons;

    public MyClientServices(ILoggerFactory loggerFactory)
    {
        _cannons = CannonApiFactory.Create(loggerFactory);
    }

    public bool HasAttackerCannons()
    {
        return _cannons.GetAllCannons().Any(cannon => cannon.IsAttackerSiegeWeapon);
    }
}
```

`CannonApiFactory.Create(...)` returns an `ICannonApi`. The optional `ILoggerFactory` redirects Bannerlord.Cannons internal logging to the client module's logging pipeline. Pass it from your submodule constructor, before `Bannerlord.Cannons.SubModule` finishes loading.

`ICannonApi.GetAllCannons()` returns:

| Property | Description |
|---|---|
| `Id` | Cannon id from `cannons.xml` |
| `IsDefensiveSiegeWeapon` | `true` when the cannon is available to siege defenders |
| `IsAttackerSiegeWeapon` | `true` when the cannon is available to siege attackers |

Bannerlord.Cannons makes registered cannons available to the campaign-map siege deployment UI and supporting siege-engine data, including defender and attacker availability exposed through the fields above. It does not make AI defenders select, build, or use those cannons during campaign sieges. Client modules that need AI defender cannon usage must implement or replace their own Bannerlord `SiegeStrategyActionModel`.

The API resolves the runtime registry lazily. If it is queried before Bannerlord.Cannons has finished initialising, it returns an empty sequence.

---

## Client module initialization

Client modules should initialise Bannerlord.Cannons by declaring `Bannerlord.Cannons.SubModule` in their generated `SubModule.xml`. This is the same shape used by DellarteDellaGuerra.Core: the client owns its normal submodule, and Bannerlord also creates the Cannons submodule from the same module package.

```xml
<SubModules>
  <SubModule>
    <Name value="MyModule" />
    <DLLName value="MyModule.Integration.dll" />
    <SubModuleClassType value="MyModule.Integration.SubModule" />
    <Tags />
  </SubModule>
  <SubModule>
    <Name value="Bannerlord.Cannons" />
    <DLLName value="Bannerlord.Cannons.dll" />
    <SubModuleClassType value="Bannerlord.Cannons.SubModule" />
    <Tags />
  </SubModule>
</SubModules>
```

Your code should not call `.Inject()` or `CannonSystemInitialiser`. `CannonSystemInitialiser` was removed in 1.0.0, and `Bannerlord.Cannons.SubModule` is initialized only through Bannerlord's normal submodule lifecycle.

Your client submodule can create the API in its constructor and use it after startup:

```csharp
using Bannerlord.Cannons.Api;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace MyModule;

public sealed class SubModule : MBSubModuleBase
{
    private readonly ICannonApi _cannons;

    public SubModule()
    {
        _cannons = CannonApiFactory.Create();
    }

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        foreach (var cannon in _cannons.GetAllCannons())
        {
            // Use registered cannon metadata here.
        }
    }
}
```

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

## Design notes: lazy-evaluated component parameters

### Background

DaDG places cannons in scenes via a spawner: `GenericCannonSpawner` sits on the scene entity, and the `GenericCannon` script lives on a spawned prefab. Sceners configure cannon parameters on the spawner; `AssignParameters()` (called by Bannerlord's `SpawnerEntityMissionHelper` after prefab instantiation) copies those values to the cannon.

The problem: `AssignParameters()` runs **after** `OnInit()` on the cannon. By the time field values are copied over, `RecoilEffect` and `WheelAnimator` have already been constructed with the original field values captured as plain copies:

```csharp
// Before — values captured once at construction, stale after AssignParameters()
_recoilEffect = new RecoilEffect(body, wheelAnimator,
    RecoilDuration, PushDuration, RecoilDistance);

_wheelAnimator = new WheelAnimator(wheelL, wheelR, WheelRotationAxis);
```

Setting `RecoilDuration = 1.5f` on the cannon post-init had no effect on the already-constructed `RecoilEffect`.

### Solution

`RecoilEffect` and `WheelAnimator` now store `Func<T>` delegates that read the current field value on each invocation:

```csharp
// After — values read from the cannon instance each time they are needed
_recoilEffect = new RecoilEffect(body, wheelAnimator,
    () => RecoilDuration,
    () => PushDuration,
    () => RecoilDistance);

_wheelAnimator = new WheelAnimator(wheelL, wheelR, () => WheelRotationAxis);
```

`WheelRotationAxis` is now an enum (`X` or `Y`) instead of a string, so the wheel animator can read the current value directly without string parsing.

### Effect on spawner overrides

| Field | Component | Applied from |
|---|---|---|
| `RecoilDuration` | `RecoilEffect.Update()` | Next shot |
| `PushDuration` | `RecoilEffect.UpdateReturn()` | Next push-return phase |
| `RecoilDistance` | `RecoilEffect.Begin()` | Next shot |
| `WheelRotationAxis` | `WheelAnimator.Rotate()` | Next tick |

Fields that are already read dynamically (not captured into components) — `BaseMuzzleVelocity`, `DirectionRestrictionDegrees`, `DisplayName`, `FireSoundID`, all animation action names, and `WaitStandingPointTag` — required no changes and work correctly with spawner overrides as-is.
