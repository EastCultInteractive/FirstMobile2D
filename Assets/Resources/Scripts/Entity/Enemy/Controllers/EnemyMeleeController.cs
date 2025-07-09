
namespace Resources.Scripts.Enemy.Controllers
{
    public class EnemyMeleeController : EnemyController
    {
        protected override void PerformAttack()
        {
            base.PerformAttack();
            Player.TakeDamage(this);
        }
    }
}