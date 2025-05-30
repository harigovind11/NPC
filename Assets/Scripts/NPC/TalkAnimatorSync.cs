using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class TalkAnimationSync : MonoBehaviour
{
    private Animator _animator;
    private AudioSource _audioSource;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        bool isTalking = _audioSource.isPlaying;
        _animator.SetBool("Talk", isTalking);
    }
}