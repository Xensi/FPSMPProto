using UnityEngine;
[CreateAssetMenu]
public class WeaponData : ScriptableObject
{
    public float timeBetweenShots = 0.1f;
    public AudioClip gunSound;
    public AudioClip reloadSound;
    public float baseSpread = 0.01f;
    public float spreadPerShot = 0.01f;
    public float maxSpread = 0f;
    public float recoveryScale = 0.1f; //lower number is slower to recover
    public int pelletsPerShot = 1;
    public int damage = 20;
    public Projectile projectile;
    public float force = 10;
    public bool inheritMomentum = false;
    public bool thrown = false;
    public int startingAmmo = 30;
    public int ammoPerShot = 1;
    public int reloadTime = 1;
    public int magSize = 8;

    [Header("Jolt Settings")]
    public float joltX = -25;
    public float joltY = 25;
    public float joltZ = 25;
    public float joltReturnSpeed = 50;

    [Header("AI Behavior")]
    public bool aiIndirectFire = false;
    public float aiRatio = 0.5f; 

}
