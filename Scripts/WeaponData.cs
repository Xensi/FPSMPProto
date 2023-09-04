using UnityEngine;
[CreateAssetMenu]
public class WeaponData : ScriptableObject
{
    public float timeBetweenShots = 0.1f;
    public AudioClip gunSound;
    public float randomSpread = 0;
    public int pelletsPerShot = 1;
    public int damage = 20;
    public Projectile projectile;
    public float force = 10;
    public bool inheritMomentum = false;
}
