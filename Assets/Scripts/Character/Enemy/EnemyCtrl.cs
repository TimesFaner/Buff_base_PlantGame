using UnityEngine;

namespace Remake
{
    public class EnemyCtrl : MonoBehaviour
    {
        public Rigidbody2D erb;
        public EnemyAttack _EnemyAttack;
        private EnemyStateMachine _enemyStateMachine;


        private void Awake()
        {
            _enemyStateMachine = new EnemyStateMachine(this);
            erb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            _enemyStateMachine.ChangeState(_enemyStateMachine._enemyAttackState);
        }

        private void Update()
        {
            _enemyStateMachine.HandleInput();
            _enemyStateMachine.Update();
        }

        private void FixedUpdate()
        {
            _enemyStateMachine.PhysicsUpdate();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            //TODO
        }
    }
}