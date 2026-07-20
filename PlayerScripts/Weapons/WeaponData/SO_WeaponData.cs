using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newWeaponData", menuName = "Data/Weapon Data/Weapon Data")]

public class SO_WeaponData : ScriptableObject
// Start is called before the first frame update
{
    public int amountOfAttacks {get; protected set; }
    
    public float[] movementSpeed {get; protected set; }
}