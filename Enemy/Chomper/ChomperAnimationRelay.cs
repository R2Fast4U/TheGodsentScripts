using UnityEngine;

public class ChomperAnimationRelay : MonoBehaviour
{
    private Entity entity;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
    }

    private void FlipAnimationEvent()
    {
        if (entity != null && entity.stateMachine != null)
            entity.stateMachine.currentState.AnimationTrigger();
    }
}