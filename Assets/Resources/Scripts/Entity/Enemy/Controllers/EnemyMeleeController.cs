
namespace Resources.Scripts.Entity.Enemy.Controllers
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