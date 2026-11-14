namespace ChickenShooter
{
    /// <summary>Implemented by every pooled object so it can reset its own state.</summary>
    public interface IPooledObject
    {
        void OnObjectSpawn();
        void OnObjectReturn();
    }

    /// <summary>Anything that can receive damage (enemies, asteroids, eggs, feathers, bosses).</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}
