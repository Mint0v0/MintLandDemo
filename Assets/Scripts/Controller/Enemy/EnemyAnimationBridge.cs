using UnityEngine;

namespace MintLandDemo.Controller.Enemy
{
    [RequireComponent(typeof(Animator))]
    public class EnemyAnimationBridge : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyController;

        private Animator _animator;
        private readonly int _speedHash = Animator.StringToHash("Speed");
        private readonly int _attackHash = Animator.StringToHash("Attack");
        private readonly int _deadHash = Animator.StringToHash("Dead");

        private EnemyState _lastState = EnemyState.Idle;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (enemyController == null)
                enemyController = GetComponent<EnemyController>();
        }

        private void Update()
        {
            if (enemyController == null) return;

            var state = enemyController.CurrentState;

            float speed = (state == EnemyState.Chase) ? 1f : 0f;
            _animator.SetFloat(_speedHash, speed);

            if (state == EnemyState.Attack && _lastState != EnemyState.Attack)
                _animator.SetTrigger(_attackHash);

            if (state == EnemyState.Dead)
                _animator.SetBool(_deadHash, true);

            _lastState = state;
        }
    }
}