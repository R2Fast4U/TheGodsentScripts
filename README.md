# Unity 2D Platformer - Improved Player Controller

## Overview
This is an improved 2D platformer player controller with Hollow Knight-inspired movement mechanics. The controller features responsive input handling, variable jump heights, smooth wall sliding, and precise wall jumping.

## Key Features

### 🎮 Responsive Controls
- **Variable Jump Heights**: Hold jump button for higher jumps, tap for short hops
- **Input Buffering**: Jump inputs are buffered for more forgiving timing
- **Smooth Movement**: Responsive horizontal movement with proper acceleration

### 🧱 Wall Mechanics
- **Wall Sliding**: Smooth wall sliding with reduced gravity
- **Wall Jumping**: Precise wall jumping with directional control
- **Wall Detection**: Improved wall detection with visual debugging

### ⚡ Performance Optimizations
- **Efficient FSM**: Clean state machine architecture
- **Optimized Physics**: Smart gravity scaling based on movement state
- **Reduced Debug Spam**: Cleaner logging for better performance

## Setup Instructions

### 1. Required Components
Add these components to your Player GameObject:
- `Player` (main controller)
- `PlayerInputHandler` (input processing)
- `InputBuffer` (input buffering)
- `Rigidbody2D` (physics)
- `Animator` (animations)
- `Collider2D` (collision detection)

### 2. Transform Setup
Create and assign these transforms as children of the Player:
- `GroundCheck`: Position at player's feet for ground detection
- `WallCheck`: Position at player's side for wall detection

### 3. PlayerData Configuration
Create a PlayerData ScriptableObject with these recommended values:
```
Movement Velocity: 12
Jump Velocity: 16
Wall Jump Velocity: 14
Wall Jump Time: 0.3
Ground Check Radius: 0.2
Wall Check Distance: 0.6
Coyote Time: 0.15
Wall Slide Velocity: 4
Gravity Scale: 3
```

### 4. Input System Setup
Ensure your Input Actions asset has:
- **Movement**: Vector2 input (WASD/Arrow Keys)
- **Jump**: Button input (Space/Gamepad South)

## Architecture

### State Machine Structure
```
PlayerState (Base)
├── PlayerGroundedState
│   ├── PlayerIdleState
│   └── PlayerMoveState
├── PlayerInAirState
├── PlayerAbilityState
│   ├── PlayerJumpState
│   ├── PlayerWallJumpState
│   └── PlayerLandState
└── PlayerTouchingWallState
    ├── PlayerWallSlideState
    ├── PlayerWallGrabState
    └── PlayerWallClimbState
```

### Key Components
- **Player**: Main controller managing state machine and physics
- **PlayerInputHandler**: Processes Unity Input System events
- **InputBuffer**: Provides input buffering for responsive controls
- **PlayerData**: ScriptableObject containing all movement parameters

## Improvements Made

### 1. Fixed Jump Mechanics
- **Variable Jump Heights**: Jump height now depends on how long the jump button is held
- **Better Gravity Scaling**: More responsive gravity changes for snappy feel
- **Input Buffering**: Jump inputs are buffered for more forgiving timing

### 2. Improved Wall Interactions
- **Better Wall Detection**: More reliable wall detection with proper raycasting
- **Smooth Wall Sliding**: Improved wall slide mechanics with reduced velocity
- **Responsive Wall Jumps**: Faster wall jump recovery and better control

### 3. Enhanced Input Handling
- **Clean Input Processing**: Removed conflicting input logic
- **Input Buffering**: Added input buffer for more responsive controls
- **Better State Transitions**: Smoother transitions between states

### 4. Performance Optimizations
- **Removed Debug Spam**: Cleaned up excessive logging
- **Optimized Gravity**: More efficient gravity scaling system
- **Better Code Structure**: Cleaner, more maintainable code

## Troubleshooting

### Common Issues

1. **Player not moving**
   - Check if PlayerInputHandler is attached
   - Verify Input Actions are properly configured
   - Ensure PlayerData is assigned

2. **Wall jumping not working**
   - Verify wallCheck transform is positioned correctly
   - Check wallCheckDistance in PlayerData
   - Ensure ground layer mask is set correctly

3. **Jump feels unresponsive**
   - Adjust inputHoldTime in PlayerInputHandler
   - Check gravity scale values in PlayerData
   - Verify input buffering is working

### Debug Tools
- Wall detection rays are drawn in Scene view (red/blue)
- Animation parameters are set for debugging
- Console logs for state transitions

## Customization

### Adjusting Feel
- **Movement Speed**: Modify `movementVelocity` in PlayerData
- **Jump Height**: Adjust `jumpVelocity` and gravity scaling
- **Wall Jump Distance**: Change `wallJumpVelocity` and `wallJumpAngle`
- **Input Responsiveness**: Modify buffer times in InputBuffer

### Adding New States
1. Create new state class inheriting from appropriate base state
2. Add state to Player class state variables
3. Initialize in Awake() method
4. Add transition logic in existing states

## Credits
This controller is inspired by Hollow Knight's movement system and modern platformer design principles. The FSM architecture provides a solid foundation for expanding with additional abilities and mechanics. 