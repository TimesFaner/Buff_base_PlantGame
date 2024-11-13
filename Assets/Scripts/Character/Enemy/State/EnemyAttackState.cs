using UnityEngine;

namespace Remake
{
    public class EnemyAttackState : IState
    {
        private readonly EnemyStateMachine _enemyStateMachine;

        //速度·
        protected float baseSpeed = 1f;
        private float distance;

        public Transform nearestPlayer;
        private Transform[] playerTransform;

        public float speedModifier = All_Data.EnemySpeedModify;

        //TODO
        public float stopDistance = All_Data.EnemyStopDistance; //改为统一数值

        public EnemyAttackState(EnemyStateMachine enemyStateMachine)
        {
            _enemyStateMachine = enemyStateMachine;
        }

        public void Enter()
        {
            //   nearestPlayer.position = new Vector3(0, 0, 0);
            _enemyStateMachine.Enemy._EnemyAttack = _enemyStateMachine.Enemy.GetComponent<EnemyAttack>();
        }

        public void Handleinput()
        {
        }

        public void Update()
        {
            GetTransformofPlayer();
        }

        public void PhysicsUpdate()
        {
        }

        public void Exit()
        {
        }

        #region mainfunc

        private void GetTransformofPlayer()
        {
            var theplayer = GameObject.FindGameObjectsWithTag("Player");
            if (theplayer.Length < 1)
            {
                Debug.Log("NNNO Find the Player");
            }
            else
            {
                playerTransform = new Transform[theplayer.Length];
                for (var i = 0; i < theplayer.Length; i++) playerTransform[i] = theplayer[i].transform;
                //  Debug.Log("Find the Player"+theplayer[i].transform.position);
            }

            GoWhichPlayer();
        }

        private void GoWhichPlayer()
        {
            distance = Mathf
                .Infinity; //(_enemyStateMachine.Enemy.transform.position-playerTransform[0].position).sqrMagnitude;
            foreach (var player in playerTransform)
            {
                var temp = (_enemyStateMachine.Enemy.transform.position - player.position).sqrMagnitude;

                if (temp <= distance)
                {
                    distance = temp;
                    nearestPlayer = player;
                }
            }

            GoThePlayer();
        }

        private void GoThePlayer()
        {
            _enemyStateMachine.Enemy.transform.Translate
            ((nearestPlayer.position - _enemyStateMachine.Enemy.transform.position).normalized
             * (baseSpeed * speedModifier * Time.deltaTime));
            _enemyStateMachine.Enemy.erb.velocity = new Vector2(0, 0);

            //TODO
            if (Vector3.Distance(_enemyStateMachine.Enemy.transform.position, nearestPlayer.position) < stopDistance)
            {
                speedModifier = 0;
                Attack();
            }
            else
            {
                speedModifier = All_Data.EnemySpeedModify;
            }
        }

        private void Attack()
        {
            //TODO
        }

        #endregion
    }
}