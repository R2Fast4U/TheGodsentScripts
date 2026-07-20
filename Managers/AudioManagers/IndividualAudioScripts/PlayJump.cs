//SOUND CODE TEMPLATE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayJump : MonoBehaviour
{
	public void PlaySound()
	{
		AudioManager.PlaySound(SoundType.JUMP);
	}
}

