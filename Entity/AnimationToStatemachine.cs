using UnityEngine;

public class AnimationToStatemachine : MonoBehaviour
{
    public Entity entity;

    private void OnEnable()
    {
        if (entity == null)
            entity = GetComponentInParent<Entity>();
    }

    public void AnimationTrigger()
    {
        entity.stateMachine.currentState.AnimationTrigger();
    }

    public void AnimationFinishTrigger()
    {
        entity.stateMachine.currentState.AnimationFinishTrigger();
    }
}
