using UnityEngine;

public class NPCBehaviorController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float targetDistanceThreshold = 0.1f;
    [SerializeField] private float rotationSpeed = 5f;

    private Animator _animator;
    private State _currentState;
    private Transform _currentTarget;
    private Quaternion _originalRotation;

    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int WalkingHash = Animator.StringToHash("Walking");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("Animator component missing on NPC.");
        }
    }

    private void Start()
    {
        _currentState = State.Idle;
        _originalRotation = transform.rotation;
    }

    private void Update()
    {
        switch (_currentState)
        {
            case State.Move:
                Move();
                break;
            case State.Idle:
                RotateBackToOriginal();
                break;
        }
    }

    public void UpdateCurrentTarget(Transform newTarget)
    {
        _currentTarget = newTarget;
    }

    public void UpdateState(int newState)
    {
        UpdateState((State)newState);
    }

    private void UpdateState(State newState)
    {
        _currentState = newState;
        PerformPostUpdateStateActions();
    }

    private void PerformPostUpdateStateActions()
    {
        switch (_currentState)
        {
            case State.Move:
                _animator.CrossFade(WalkingHash, 0.01f);
                break;
            case State.Idle:
                _animator.CrossFade(IdleHash, 0.01f);
                break;
        }
    }

    private void Move()
    {
        if (_currentTarget == null)
        {
            Debug.LogWarning("No target set for NPC.");
            UpdateState(State.Idle);
            return;
        }

        if (Vector3.Distance(transform.position, _currentTarget.position) < targetDistanceThreshold)
        {
            UpdateState(State.Idle);
            return;
        }

        Vector3 direction = (_currentTarget.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
    }

    private void RotateBackToOriginal()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, _originalRotation, Time.deltaTime * rotationSpeed);
    }

    private enum State
    {
        Move = 0,
        Idle = 1
    }
}
