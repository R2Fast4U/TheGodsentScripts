# Butterfly Particle Setup Guide

## Overview
This guide will help you set up animated butterfly particles using your spritesheet animation. The system includes butterfly behavior and integration with the ambient particle system.

## Step-by-Step Setup

### Step 1: Prepare Your Spritesheet

1. **Import your butterfly spritesheet** into Unity
2. **Set Texture Import Settings**:
   - Select your spritesheet in the Project window
   - In Inspector, set:
     - **Texture Type**: Sprite (2D and UI)
     - **Sprite Mode**: Multiple
     - **Pixels Per Unit**: 100
     - **Filter Mode**: Bilinear
     - **Compression**: None (for pixel art) or High Quality

### Step 2: Slice Your Spritesheet

1. Select your spritesheet in Project window
2. In Inspector, click **Sprite Editor**
3. Click **Slice** → **Grid By Cell Size** or **Grid By Cell Count**
4. Set your sprite dimensions (e.g., 32x32 pixels)
5. Click **Apply**

### Step 3: Create Butterfly Prefab

1. **Create a new GameObject** in your scene
2. **Add a SpriteRenderer** component
3. **Add the `ButterflyParticle` script**
4. **Configure the ButterflyParticle settings**:

   **Animation Settings:**
   - **Sprite Renderer**: Assign the SpriteRenderer component
   - **Butterfly Sprites**: Drag all your sliced sprites here
   - **Animation Speed**: 8 (adjust for desired wing flap speed)
   - **Randomize Start Frame**: True (for variety)

   **Flight Settings:**
   - **Flight Speed**: 2 (how fast butterflies move)
   - **Flight Amplitude**: 1 (wobble in flight)
   - **Direction Change Interval**: 3 (how often they change direction)
   - **Max Flight Distance**: 5 (how far they fly from spawn point)

   **Movement Pattern:**
   - **Enable Wandering**: True (natural butterfly movement)
   - **Wander Speed**: 1 (speed of wandering motion)
   - **Wander Amplitude**: 0.5 (amount of wandering)

   **Butterfly Behavior:**
   - **Enable Landing**: True (butterflies can land on surfaces)
   - **Landing Chance**: 0.1 (probability of landing per second)
   - **Landing Duration**: 2 (how long they stay landed)
   - **Landing Layer**: Set to your ground layer

   **Visual Effects:**
   - **Enable Fading**: True (subtle fade in/out)
   - **Fade Speed**: 0.5 (speed of fade effect)
   - **Min Alpha**: 0.6 (minimum transparency)
   - **Max Alpha**: 1 (maximum transparency)

5. **Make it a Prefab**: Drag the GameObject to your Project window

### Step 4: Set Up Ambient Butterfly System

1. **Create an empty GameObject** in your scene
2. **Add the `AmbientParticleSystem` script**
3. **Configure ambient particles**:
   - **Particle Name**: "Butterfly"
   - **Particle Prefab**: Drag your butterfly prefab
   - **Max Particles In Scene**: 4-5 (as you wanted)
   - **Spawn Radius**: 12 (area around player)
   - **Min Spawn Distance**: 3 (closest to player)
   - **Max Spawn Distance**: 10 (furthest from player)
   - **Spawn Interval**: 4 (seconds between spawns)
   - **Respawn When Destroyed**: True
   - **Spawn On Ground**: False (butterflies fly in air)
   - **Random Rotation**: True
   - **Random Scale**: True (0.8 to 1.2 for variety)

### Step 5: Configure Scene Settings

1. **Set Player Transform**: Assign your player to the AmbientParticleSystem
2. **Adjust Culling Distance**: Set to 20-25 units
3. **Test the System**: Play the scene and walk around

## Butterfly Behavior Features

### Flight Patterns
- **Wandering Movement**: Natural butterfly-like flight paths
- **Direction Changes**: Random direction changes every few seconds
- **Altitude Variation**: Gentle up/down movement
- **Speed Variation**: Slight speed changes for realism

### Landing Behavior
- **Surface Detection**: Butterflies can land on ground/objects
- **Landing Duration**: They stay landed for a few seconds
- **Takeoff**: Automatic takeoff after landing period

### Visual Effects
- **Wing Flapping**: Animated using your spritesheet
- **Sprite Flipping**: Faces direction of movement
- **Fade Effects**: Subtle transparency changes
- **Size Variation**: Random scale for variety

## Customization Options

### Animation
```csharp
// Change animation speed
butterflyParticle.SetAnimationSpeed(12f); // Faster wing flaps

// Change sprites at runtime
butterflyParticle.SetSprites(newSprites);
```

### Flight Behavior
```csharp
// Change flight speed
butterflyParticle.SetFlightSpeed(3f); // Faster movement

// Force landing
butterflyParticle.ForceLand();

// Set new home position
butterflyParticle.SetNewStartPosition(newPosition);
```

### Ambient System Control
```csharp
// Spawn butterfly at specific position
ambientSystem.SpawnParticleAtPosition("Butterfly", position);

// Clear all butterflies
ambientSystem.ClearAllParticles();
```

## Performance Tips

1. **Limit Butterfly Count**: Keep total butterflies under 10-15
2. **Optimize Sprites**: Use compressed textures for distant butterflies
3. **Cull Distant Butterflies**: The system automatically culls distant ones
4. **Use Object Pooling**: For better performance with many butterflies

## Troubleshooting

### Butterflies Not Appearing
- Check if AmbientParticleSystem is active
- Verify butterfly prefab is assigned
- Check spawn radius and distance settings
- Ensure player transform is assigned

### Animation Not Working
- Verify sprites are assigned to Butterfly Sprites array
- Check animation speed setting
- Ensure SpriteRenderer is assigned
- Verify sprites are properly sliced

### Butterflies Not Moving
- Check flight speed setting
- Verify direction change interval
- Check if wandering is enabled
- Ensure max flight distance is reasonable

### Performance Issues
- Reduce max particles in scene
- Increase cull distance
- Use simpler sprites for distant butterflies
- Disable unnecessary visual effects

## Example Butterfly Configurations

### Gentle Butterflies
```
Flight Speed: 1.5
Animation Speed: 6
Direction Change: 4 seconds
Landing Chance: 0.05
```

### Active Butterflies
```
Flight Speed: 3
Animation Speed: 12
Direction Change: 2 seconds
Landing Chance: 0.15
```

### Large Butterflies
```
Flight Speed: 2
Animation Speed: 8
Max Flight Distance: 8
Random Scale: 1.2 to 1.8
```

This system will give you beautiful, animated butterflies that naturally fly around your scene, creating a living, breathing environment! 