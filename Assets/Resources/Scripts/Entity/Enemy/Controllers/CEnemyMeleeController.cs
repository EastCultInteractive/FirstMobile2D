
namespace Resources.Scripts.Enemy.Controllers
{
    public class CEnemyMeleeController : CEnemyController
    {
        protected override void PerformAttack()
        {
            base.PerformAttack();
            Player.TakeDamage(this);
        }
    }
}