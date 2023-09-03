using UnityEngine;
[CreateAssetMenu]
public class WeaponData : ScriptableObject
{
    [Range(0.1f, 10)]
    public float timeBetweenShots = 0.1f;
    public AudioClip gunSound;
}
