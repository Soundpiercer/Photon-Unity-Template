using System.Collections;
using UnityEngine;

public class LobbyPlayer : MonoBehaviour
{
    [Header("Unity-chan! Model")]
    public Animator animator;

    [Header("Audio Source")]
    public AudioSource audioSource;

    public const float ANIMATOR_SPEED = 1.5f;

    public IEnumerator InitEnumerator()
    {
        animator.speed = ANIMATOR_SPEED;

        yield return new WaitForSeconds(0.2f);
        animator.SetTrigger("Jump");
        audioSource.Play();
    }

    public void PlayAnimationOnRaycastHit()
    {
        animator.SetTrigger("Jump");
        audioSource.Play();
    }
}
