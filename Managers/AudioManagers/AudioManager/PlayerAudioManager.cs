using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
        if (player == null)
        {
            player = GetComponentInParent<Player>();
        }
    }

    public void PlayAttack()
    {
        AudioManager.PlaySound(SoundType.ATTACK);
    }

    public void PlayJump()
    {
        if (player == null)
        {
            player = GetComponent<Player>() ?? GetComponentInParent<Player>() ?? FindObjectOfType<Player>();
        }

        if (player != null && player.StateMachine != null)
        {
            if (player.StateMachine.CurrentState == player.JumpState && player.JumpState.JumpedFromGround)
            {
                AudioManager.PlaySound(SoundType.JUMP);
            }
        }
        else
        {
            Debug.LogWarning("PlayerAudioManager: Player script or StateMachine is not found. JUMP sound skipped.");
        }
    }

    public void PlayHurt()
    {
        AudioManager.PlaySound(SoundType.HURT);
    }

    public void PlayLedgeClimb()
    {
        AudioManager.PlaySound(SoundType.LEDGECLIMB);
    }

    public void PlayWarp()
    {
        AudioManager.PlaySound(SoundType.WARP);
    }

    public void PlayDeath()
    {
        AudioManager.PlaySound(SoundType.DEATH);
    }

    public void PlayLand()
    {
        AudioManager.PlaySound(SoundType.LAND);
    }

    public void PlayFootstep()
    {
        AudioManager.PlaySound(SoundType.WALK);
    }

    public void PlayWallSlide()
    {
        AudioManager.PlaySound(SoundType.WALLSLIDE);
    }
}
