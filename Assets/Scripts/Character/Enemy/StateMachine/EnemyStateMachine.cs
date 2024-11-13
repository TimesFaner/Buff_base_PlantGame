namespace Remake
{
    public class EnemyStateMachine : StateMachine
    {
        public EnemyCtrl Enemy;

        public EnemyStateMachine(EnemyCtrl enemy)
        {
            Enemy = enemy;
            _enemyAttackState = new EnemyAttackState(this);
        }

        public EnemyAttackState _enemyAttackState { get; }
    }
}