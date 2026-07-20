using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RippleEffectUpdater : MonoBehaviour
{
    public Material rippleEffectMaterial; // Assign your ripple effect material in the Inspector

    void Update()
    {
        // Ensure continuous update of the ripple effect
        float time = Time.time;
        rippleEffectMaterial.SetFloat("_Time", time);
    }
}
