using UnityEngine;

public class WeaponAnimationRelay : MonoBehaviour
{
    private Weapon weapon;

    private void Awake()
    {
        weapon = GetComponentInParent<Weapon>();
    }

    private void AnimationFinishTrigger() => weapon?.AnimationFinishTrigger();
    private void AnimationActionTrigger() => weapon?.AnimationActionTrigger();
}
