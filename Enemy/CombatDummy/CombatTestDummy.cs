using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CombatTestDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private GameObject hitParticles;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip[] gruntSounds;
    private AudioSource audioSource;
    private Animator anim;
    private Coroutine hitCoroutine;
    private static readonly int HitHash = Animator.StringToHash("hit");

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
        if (anim == null)
            Debug.LogError($"CombatTestDummy '{name}': no Animator found on this GameObject or its children.");

        audioSource = GetComponent<AudioSource>();
    }

    public void Damage(float amount)
    {
        Debug.Log(amount + " damage taken by " + gameObject.name);

        if (hitParticles != null)
            Instantiate(hitParticles, transform.position, Quaternion.Euler(0.0f, 0.0f, Random.Range(0.0f, 360.0f)));

        if (hitSounds != null && hitSounds.Length > 0)
            audioSource.PlayOneShot(hitSounds[Random.Range(0, hitSounds.Length)]);

        if (gruntSounds != null && gruntSounds.Length > 0)
            audioSource.PlayOneShot(gruntSounds[Random.Range(0, gruntSounds.Length)]);

        if (anim == null) return;

        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);

        hitCoroutine = StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        anim.SetBool(HitHash, true);
        yield return null; // let animator enter the hit state

        // restart from the beginning (handles re-hits mid-animation)
        anim.Play(anim.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, 0f);
        yield return null; // let the Play call take effect

        yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);

        anim.SetBool(HitHash, false);
        hitCoroutine = null;
    }
}
