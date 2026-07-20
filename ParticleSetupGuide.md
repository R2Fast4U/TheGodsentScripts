# Particle System Setup Guide

## Quick Setup Steps

### 0. Choose Your Particle Type
- **Triggered Particles**: Particles that activate based on player actions (jumping, landing, etc.)
- **Ambient Scene Particles**: Particles scattered throughout the level as environmental effects

### 1. Create a Basic Particle System
1. In Unity, right-click in Hierarchy → Effects → Particle System
2. Select the created GameObject
3. In Inspector, configure the ParticleSystem component

### 2. Basic Particle Configuration

**Main Module Settings:**
- Duration: 2 (how long system runs)
- Start Lifetime: 1-3 (particle lifetime)
- Start Speed: 2-5 (particle velocity)
- Start Size: 0.1-1 (particle size)
- Start Color: Choose your color
- Gravity Modifier: 0.5-2 (gravity effect)

**Emission Module:**
- Rate over Time: 10-50 (particles per second)
- Bursts: Add sudden particle bursts

**Shape Module:**
- Shape: Circle (for most effects)
- Radius: 0.1-1 (spawn area size)

**Renderer Module:**
- Render Mode: Billboard (2D)
- Material: Create or assign particle material

### 3. Using the ParticleController Script

1. Add the `ParticleController` script to your particle GameObject
2. Configure the settings:
   - **Play On Start**: Auto-play when scene loads
   - **Play On Trigger**: Play when player enters trigger
   - **Play On Collision**: Play on collision
   - **Trigger Tags**: Which objects can trigger particles
   - **Follow Target**: Make particles follow an object
   - **Auto Destroy**: Destroy after playing

### 4. Using the ParticleEffectManager

1. Create an empty GameObject in your scene
2. Add the `ParticleEffectManager` script
3. Configure particle effects in the inspector:
   - **Effect Name**: Unique name for the effect
   - **Particle Prefab**: The particle system prefab
   - **Duration**: How long the effect lasts
   - **Auto Destroy**: Whether to destroy after playing

### 5. Setting Up Ambient Scene Particles

1. Create an empty GameObject in your scene
2. Add the `AmbientParticleSystem` script
3. Configure ambient particles in the inspector:
   - **Particle Name**: Unique name for the ambient effect
   - **Particle Prefab**: The particle prefab with `AmbientParticle` script
   - **Max Particles In Scene**: How many particles to maintain (e.g., 4-5)
   - **Spawn Radius**: Area around player to spawn particles
   - **Spawn Interval**: Time between spawning new particles
   - **Respawn When Destroyed**: Whether to replace destroyed particles

4. Add the `AmbientParticle` script to your particle prefabs for individual behaviors:
   - **Floating**: Gentle up/down movement
   - **Rotation**: Continuous rotation
   - **Pulsing**: Size pulsing effect
   - **Fading**: Alpha fade in/out

## Common Particle Effects

### Dust Effect
- **Shape**: Circle
- **Start Speed**: 1-2
- **Start Size**: 0.1-0.3
- **Color**: Brown/Gray
- **Gravity**: 0.5-1
- **Lifetime**: 1-2 seconds

### Sparkles Effect
- **Shape**: Circle
- **Start Speed**: 3-5
- **Start Size**: 0.05-0.1
- **Color**: White/Yellow
- **Gravity**: 0.1-0.3
- **Lifetime**: 0.5-1 seconds

### Explosion Effect
- **Shape**: Sphere
- **Start Speed**: 5-10
- **Start Size**: 0.2-0.5
- **Color**: Orange/Red
- **Gravity**: 0.2-0.5
- **Lifetime**: 0.5-1.5 seconds

### Trail Effect
- **Shape**: Circle
- **Start Speed**: 0.5-1
- **Start Size**: 0.05-0.1
- **Color**: White/Blue
- **Gravity**: 0
- **Lifetime**: 0.3-0.8 seconds

## Ambient Scene Particle Effects

### Floating Sparkles
- **Shape**: Circle
- **Start Speed**: 0.1-0.3
- **Start Size**: 0.02-0.05
- **Color**: White/Yellow
- **Gravity**: 0
- **Lifetime**: 8-15 seconds
- **Behavior**: Floating + Rotation

### Dust Particles
- **Shape**: Circle
- **Start Speed**: 0.2-0.5
- **Start Size**: 0.1-0.2
- **Color**: Brown/Gray
- **Gravity**: 0.1-0.3
- **Lifetime**: 10-20 seconds
- **Behavior**: Floating + Fading

### Fireflies
- **Shape**: Circle
- **Start Speed**: 0.3-0.8
- **Start Size**: 0.03-0.08
- **Color**: Green/Yellow
- **Gravity**: 0
- **Lifetime**: 12-18 seconds
- **Behavior**: Floating + Pulsing + Fading

### Floating Leaves
- **Shape**: Circle
- **Start Speed**: 0.5-1.5
- **Start Size**: 0.1-0.3
- **Color**: Green/Brown
- **Gravity**: 0.2-0.5
- **Lifetime**: 15-25 seconds
- **Behavior**: Floating + Rotation + Fading

## Integration with Player Controller

### Adding Particle Effects to Player States

1. **In PlayerLandState.cs:**
```csharp
private void OnLand()
{
    // Find the particle manager
    ParticleEffectManager particleManager = FindObjectOfType<ParticleEffectManager>();
    if (particleManager != null)
    {
        particleManager.OnPlayerLanded(transform);
    }
}
```

2. **In PlayerJumpState.cs:**
```csharp
private void OnJump()
{
    ParticleEffectManager particleManager = FindObjectOfType<ParticleEffectManager>();
    if (particleManager != null)
    {
        particleManager.OnPlayerJumped(transform);
    }
}
```

### Adding Particle Effects to Enemies

1. **In Enemy.cs:**
```csharp
private void OnDeath()
{
    ParticleEffectManager particleManager = FindObjectOfType<ParticleEffectManager>();
    if (particleManager != null)
    {
        particleManager.OnEnemyDied(transform.position);
    }
}
```

## Advanced Particle Techniques

### 1. Particle Materials
Create materials specifically for particles:
- Shader: Particles/Standard Unlit
- Color: Set your desired color
- Texture: Add particle texture if needed

### 2. Particle Animation
Use the Animation module to:
- Animate particle size over lifetime
- Change color over lifetime
- Rotate particles

### 3. Sub-Emitters
Create complex effects by:
- Adding sub-emitters to main particles
- Creating particle chains
- Building explosion effects

### 4. Particle Collision
Enable collision to:
- Make particles bounce off surfaces
- Create realistic physics effects
- Add interaction with world objects

## Performance Tips

1. **Limit Particle Count**: Keep particles under 1000 per system
2. **Use Object Pooling**: Reuse particle systems instead of creating new ones
3. **Optimize Materials**: Use simple shaders for particles
4. **Cull Distant Particles**: Disable particles far from camera
5. **Use LOD**: Different particle counts based on distance

## Ambient Particle Performance

1. **Limit Ambient Particles**: Keep total ambient particles under 20-30 per scene
2. **Use Distance Culling**: The AmbientParticleSystem automatically culls distant particles
3. **Optimize Individual Particles**: Use simple sprites or basic particle systems
4. **Batch Similar Effects**: Group similar ambient particles together
5. **Disable When Not Visible**: Consider disabling ambient particles when player is in menus

## Troubleshooting

### Particles Not Showing
- Check if material is assigned
- Verify particle system is playing
- Check if particles are spawning in visible area
- Ensure camera can see particle layer

### Performance Issues
- Reduce particle count
- Simplify particle materials
- Use object pooling
- Disable unnecessary modules

### Particles Not Triggering
- Check trigger/collision settings
- Verify object tags match
- Ensure colliders are set up correctly
- Check if ParticleController is attached 