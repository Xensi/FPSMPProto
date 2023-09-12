using UnityEngine;
[CreateAssetMenu]
public class WeaponData : ScriptableObject
{
    public float timeBetweenShots = 0.1f;
    public AudioClip gunSound;
    public float spreadPerShot = 0.01f;
    public float maxSpread = .1f;
    public float recoveryScale = 0.1f; //lower number is slower to recover
    public int pelletsPerShot = 1;
    public int damage = 20;
    public Projectile projectile;
    public float force = 10;
    public bool inheritMomentum = false;
    public bool thrown = false;
    public int startingAmmo = 30;
    public int ammoPerShot = 1;
}
