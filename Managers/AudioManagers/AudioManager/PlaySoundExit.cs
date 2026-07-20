using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlaySoundExit : StateMachineBehaviour
{
    [SerializeField] private SoundType sound;
    [SerializeField, Range(0, 1)] private float volume = 1f;
    override public void OnStateExit(Animator animator, AnimatorStateInfo StateInfo, int layerIndex) {
        if (sound == SoundType.JUMP)
        {
            Player player = animator.GetComponent<Player>();
            if (player != null)
            {
                if (player.StateMachine != null && player.StateMachine.CurrentState == player.JumpState && player.JumpState.JumpedFromGround)
                {
                    AudioManager.PlaySound(sound, volume);
                }
            }
            else
            {
                AudioManager.PlaySound(sound, volume);
            }
        }
        else
        {
            AudioManager.PlaySound(sound, volume);
        }
    }
}
