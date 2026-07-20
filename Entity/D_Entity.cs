using UnityEngine;

[CreateAssetMenu(fileName = "newEntityData", menuName = "Data/Entity Data/Base Data")]
public class D_Entity : ScriptableObject
{
    public float maxHealth = 30f;
    public float movementSpeed = 3f;

    public float damageHopSpeed = 3f;

    public float wallCheckDistance = 0.4f;
    public float ledgeCheckDistance = 0.6f;

    public float knockbackDuration = 0.2f;
    public Vector2 knockbackSpeed = new Vector2(5f, 3f);

    public float minAgroDistance = 3f;
    public float maxAgroDistance = 4f;
    public float closeRangeActionDistance = 1f;

    public float stunResistance = 3f;
    public float stunRecoveryTime = 2f;

    public LayerMask whatIsGround;
    public LayerMask whatIsPlayer;
}
