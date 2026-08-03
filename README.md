# The Godsent — Scripts Reference

2D action-platformer built in Unity. This README documents every C# script in `Assets/Scripts/`.

---

## Architecture Overview

```
MonoBehaviour
├── Player                    — Character controller with hierarchical finite state machine
├── Enemy / Chomper           — Enemy base class + concrete enemy with patrol, damage, and death
├── Core / CoreComponents     — Shared physics/movement/collision system (used by Player + enemies)
├── Weapons                   — Weapon base + melee damage with combo system
├── Audio Managers            — SFX pool, music playlists with crossfade, animation-driven audio
├── World Behaviour           — Parallax backgrounds, camera, particles, scene loading
└── Interfaces                — IDamageable contract shared by Player, enemies, weapons
```

---

## Player System

### Finite State Machine

| File | Type | Purpose |
|---|---|---|
| `Player.cs` | MB | Root component. Owns all states, Core/Input/Inventory, check transforms, animation trigger relay. |
| `PlayerStateMachine.cs` | Pure | `Initialize(State)` → `ChangeState(State)`. Exits old, enters new. Clears jump input on transition. |
| `PlayerState.cs` | Pure | Base state. Virtuals: `Enter()`, `Exit()`, `LogicUpdate()`, `PhysicsUpdate()`, `AnimationTrigger()`, `AnimationFinishTrigger()`. Sets/clears animation bools. |

### Super States (abstract groups)

| File | Purpose |
|---|---|
| `PlayerGroundedState.cs` | Jump, attack, look up/down hold, warp, fall-to-air transitions |
| `PlayerAbilityState.cs` | Tracks `isAbilityDone`, transitions back to Idle/Move/InAir |
| `PlayerTouchingWallState.cs` | Shared wall state logic: xInput, jumpInput, grounded, touchingWall |

### Sub States

| File | Purpose |
|---|---|
| `PlayerIdleState.cs` | Zero velocity, transitions to Move on horizontal input |
| `PlayerMoveState.cs` | Horizontal movement with flip, returns to Idle on no input |
| `PlayerJumpState.cs` | Variable-height jump, air control, attack interruption |
| `PlayerInAirState.cs` | Coyote time, air movement, wall grab/slide/ledge climb detection |
| `PlayerLandState.cs` | Brief landing transition, consumes jump input |
| `PlayerWallGrabState.cs` | Hold stationary on wall, wall jump or release |
| `PlayerWallSlideState.cs` | Slide down wall, fast on down-press, loop audio |
| `PlayerWallJumpState.cs` | Angled velocity off wall, lockout timer, air control |
| `PlayerLedgeClimbState.cs` | Auto-climb with position interpolation |
| `PlayerLookUpState.cs` | Cinemachine camera offset up |
| `PlayerLookDownState.cs` | Cinemachine camera offset down |
| `PlayerWarpState.cs` | Slow-time aim, dash on release/timeout, cooldown |
| `PlayerAttackState.cs` | Weapon parenting, combo counter, flip grace period |

### Data & Input

| File | Type | Purpose |
|---|---|---|
| `PlayerData.cs` | SO | All player tuning: movement speed, jump force, gravity multipliers, wall jump, warp, attack timings |
| `PlayerInputHandler.cs` | MB | Unity Input System bridge: movement, jump, warp, primary/secondary attack, input blocking |
| `InputBuffer.cs` | MB | Soft buffer for jump/wall-jump with configurable expiry window |

### Inventory & Weapons

| File | Type | Purpose |
|---|---|---|
| `PlayerInventory.cs` | MB | Array of Weapon references |
| `Weapon.cs` | MB | Base weapon: activation, attack counter, animators, animation relay triggers |
| `AgressiveWeapon.cs` | MB | Melee weapon: tracks IDamageable targets, applies damage from data |
| `WeaponAnimationRelay.cs` | MB | Routes animation events to parent Weapon |
| `WeaponHitboxToWeapon.cs` | MB | Forwards collision enter/exit to AgressiveWeapon |
| `SO_WeaponData.cs` | SO | Base weapon data: attack count, movement speed per attack |
| `SO_AggressiveWeaponData.cs` | SO | Adds `WeaponAttackDetails[]` (damage amount per combo hit) |
| `AttackDetails.cs` | Struct | `WeaponAttackDetails` (name, speed, damage, position) |

---

## Core System (Player + Enemy shared)

| File | Type | Purpose |
|---|---|---|
| `Core.cs` | MB | Top-level: locates child `Movement` and `CollisionSenses` |
| `CoreComponent.cs` | MB | Base for sub-components: finds parent Core |
| `Movement.cs` | MB | Rigidbody2D velocity, facing direction, gravity adjustment, wall snap, flip |
| `CollisionSenses.cs` | MB | Ground/wall/ledge detection via raycasts |

---

## Enemy System

### Base

| File | Type | Purpose |
|---|---|---|
| `Enemy.cs` | MB | Walking/Knockback/Dead state machine, touch damage, ground/wall detection, `IDamageable.Damage()`, hit feedback |

### Chomper (concrete enemy)

| File | Type | Purpose |
|---|---|---|
| `Chomper.cs` | MB | Robot enemy. Flip animation on obstacle, footstep/death sounds, sprite flip, particle death FX. Only Inspector fields — all config in one place. |

### Other Enemies

| File | Type | Purpose |
|---|---|---|
| `CombatTestDummy.cs` | MB | Training dummy: plays hit/grunt sounds and animation on damage |
| `EnemyPatrol.cs` | MB | Simple waypoint patrol enemy (test) |
| `Launcher.cs` | MB | Fires bullet prefabs on a timed schedule |
| `Bullet.cs` | MB | Linear projectile with lifetime self-destruct |
| `HitParticleController.cs` | MB | Auto-destroys hit particle after animation ends |

---

## Audio System

### Music

| File | Type | Purpose |
|---|---|---|
| `MusicManager.cs` | MB | Singleton. Peaceful/combat/ambience playlists, crossfade transitions, AudioMixer snapshots, overlay support |

### SFX

| File | Type | Purpose |
|---|---|---|
| `AudioManager.cs` | MB | Static SFX pool: plays sounds by enum, volume config, wall-slide looping, auto-pool return |
| `PlayerAudioManager.cs` | MB | Bridge on Player: `PlayAttack()`, `PlayJump()`, `PlayHurt()`, etc. |
| `PlaySoundEnter.cs` | SMB | Plays a sound on animator state enter |
| `PlaySoundExit.cs` | SMB | Plays a sound on animator state exit |
| `PlayJump.cs` | MB | Animation event → plays jump SFX |
| `PlayFootstep.cs` | MB | Animation event → plays walk SFX |

---

## World Behaviour

### Camera

| File | Purpose |
|---|---|
| `CinemachineOffsetController.cs` | Singleton. Vertical camera offset + "hit zoom" punch when player lands attacks |
| `CameraFollow.cs` | Legacy (commented out) |
| `CameraAspectRatio.cs` | Legacy (commented out) |

### Parallax

| File | Purpose |
|---|---|
| `GalaxyBGController.cs` | Infinite tiled background with organic drift + UV rotation |
| `BGController.cs` | 5-layer parallax with motion drift, velocity reaction, UV zoom |
| `ParrallaxBGFinal.cs` | Simple 2D parallax with factor slider and smoothing |
| `ParallaxBackground.cs` | Delegates camera delta to child ParallaxLayer components |
| `ParallaxLayer.cs` | Single parallax layer |
| `ParallaxCamera.cs` | Fires camera delta event each frame |
| `ZBasedParallax.cs` | Parallax driven by object Z-depth |
| `ZBasedBlur.cs` | Distance-based blur via MaterialPropertyBlock |

### Particles

| File | Purpose |
|---|---|
| `ParticleEffectManager.cs` | Dictionary-based spawner: dust, jump sparkles, explosions, trails |
| `ParticleController.cs` | Configurable trigger: play on start/collision/trigger, follow targets, auto-destroy |
| `AmbientParticleSystem.cs` | Pools ambient particles around player with distance culling |
| `AmbientParticle.cs` | Floating/rotating/pulsing/fading ambient particle |
| `ButterflyParticle.cs` | Animated butterfly with wandering flight, landing, fading |

### Other

| File | Purpose |
|---|---|
| `WaterRippleUpdater.cs` | Feeds time into ripple shader |
| `LoadScene.cs` | Loads scene by name on click |
| `Tiles.cs` | Centers tilemap and scales it |

---

## Interfaces

| File | Purpose |
|---|---|
| `IDamageable.cs` | `void Damage(float amount)` — implemented by Player, Enemy, CombatTestDummy |

---

## Utilities

| File | Type | Purpose |
|---|---|---|
| `CoroutineRunner.cs` | MB | Singleton for running coroutines from inactive objects (DontDestroyOnLoad) |

---

## Prefab Setup: Chomper

```
Chomper (root)
├── Chomper.cs          ← ALL Inspector fields
├── Animator, SpriteRenderer, Rigidbody2D, AudioSource, BoxCollider2D
├── GroundCheck         (empty child, center bottom)
├── WallCheck           (empty child, front edge)
└── TouchDamageCheck    (empty child, optional)
```

## Prefab Setup: Player

```
Player (root)
├── Player.cs, PlayerInputHandler, InputBuffer, PlayerInventory, PlayerAudioManager
├── Animator, Rigidbody2D, AudioSource
├── Core (child)
│   ├── Core.cs
│   ├── Movement.cs
│   └── CollisionSenses.cs
│       ├── GroundCheck
│       ├── WallCheck
│       └── LedgeCheck
├── GroundCheck, WallCheck, LedgeCheck (check transforms)
├── WarpDirectionIndicator
└── FeetParticlesPosition, HeadParticlesPosition (empty children for particle placement)
```
