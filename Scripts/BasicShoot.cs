using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BasicShoot : NetworkBehaviour
{
    [SerializeField] private WeaponData equippedWeapon;
    [SerializeField] private LayerMask canHitMask;
    [SerializeField] private AudioSource source;
    public ParticleSystem muzzle;
    [SerializeField] private Jolt jolt;
    private float range = Mathf.Infinity;
    private int bulletDamage = 1;

    private float weaponTimer;
    public float timeBetweenShots = 0.1f; //can only fire if weapon timer is greater than time between shots
    [SerializeField] private Projectile projectile;
    [SerializeField] private Rigidbody body;
    private void Start()
    {
        weaponTimer = timeBetweenShots;
    }
    private void Update()
    {
        if (!IsOwner) return;

        if (weaponTimer < timeBetweenShots)
        {
            weaponTimer += Time.deltaTime;
        }
        if (Input.GetMouseButton(0))
        {
            if (weaponTimer >= timeBetweenShots)
            {
                weaponTimer = 0;
                ClientShoot();
                ShowGunCosmeticEffects();
            }
        }
    }
    public void ShowGunCosmeticEffects(byte id = 0)
    {
        //fire locally  
        BaseGunCosmeticEffects();
        //request server to send to other clients
        ShowGunCosmeticEffectsServerRpc(id);
    }
    private void BaseGunCosmeticEffects()
    {
        float pitchChange = 0.05f;
        float pitch = Random.Range(1-pitchChange, 1+pitchChange);
        if (muzzle != null) muzzle.Play();
        if (jolt != null) jolt.FireJolt();
        if (IsOwner)
        {
            source.pitch = pitch;
            source.PlayOneShot(source.clip);
        }
        else
        {
            PlayClipAtPoint(source.clip, transform.position, 1, pitch);
        }
    }
    [ServerRpc] //(RequireOwnership = false)
    private void ShowGunCosmeticEffectsServerRpc(byte id = 0)
    {
        ShowGunCosmeticEffectsClientRpc(id);
    }
    [ClientRpc]
    private void ShowGunCosmeticEffectsClientRpc(byte id = 0)
    {
        if (!IsOwner)
        {
            BaseGunCosmeticEffects();
        }
    }   
    
    public AudioSource PlayClipAtPoint(AudioClip clip, Vector3 pos, float volume = 1, float pitch = 1, bool useChorus = false)
    {
        GameObject tempGO = new("TempAudio"); // create the temp object
        tempGO.transform.position = pos; // set its position
        AudioSource tempASource = tempGO.AddComponent<AudioSource>(); // add an audio source
        if (useChorus)
        {
            tempGO.AddComponent<AudioChorusFilter>();
        }
        tempASource.clip = clip;
        tempASource.volume = volume;
        tempASource.pitch = pitch;
        tempASource.spatialBlend = 1; //3d   
        tempASource.Play(); // start the sound
        Destroy(tempGO, tempASource.clip.length * pitch); // destroy object after clip duration (this will not account for whether it is set to loop) 
        return tempASource;
    }
    private void ClientShoot() //shoot raycast at target client side
    {
        Vector3 shootDirection = transform.forward;
        
        Ray ray = new Ray(transform.position, shootDirection);
        RaycastHit hit; //otherwise, make raycast
                        //Debug.DrawRay(transform.position, shootDirection, Color.red, 1);
        /*if (Physics.Raycast(ray, out hit, range, canHitMask)) //if raycast hits something  
        { 
            if (hit.collider.TryGetComponent(out Hurtbox hurtbox))
            {
                DealDamageUmbrella(bulletDamage, hurtbox);
            }
        }*/
        ProjectileUmbrella();
    }
    private void ProjectileUmbrella()
    { 
        ShootProjectile();
        if (IsServer)
        {
            ProjectileClientRpc(); //server tells clients about projectile
        }
        else
        {
            ProjectileServerRpc();//ask server to tell clients
        }
    }
    [ClientRpc]
    private void ProjectileClientRpc()
    {
        if (!IsOwner)
        {
            ShootProjectile();
        }
    }
    [ServerRpc]
    private void ProjectileServerRpc()
    {
        ProjectileClientRpc();
    }
    [SerializeField] private WeaponSwitcher switcher;
    private void ShootProjectile()
    { 
        Projectile proj = Instantiate(projectile, muzzle.transform.position, Quaternion.identity);
        proj.transform.rotation = transform.rotation;
        //proj.body.velocity = body.velocity;
        proj.body.AddForce(transform.forward * 10, ForceMode.Impulse);
        
    }
}
