using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BasicShoot : NetworkBehaviour
{
    public bool playerControlled = true;
    [SerializeField] private LayerMask canHitMask;
    public AudioSource source;
    public ParticleSystem muzzle;
    public Jolt jolt;
    private readonly float range = Mathf.Infinity;
    private readonly int bulletDamage = 1;

    private float weaponTimer;
    public float timeBetweenShots = 0.1f; //can only fire if weapon timer is greater than time between shots
    public float baseSpread = 0.01f;
    public float spreadPerShot = 0.01f; 
    public int pelletsPerShot = 1;
    public Projectile projectile;
    [SerializeField] private Rigidbody body; 
    public int damage = 1;
    public int ammoPerShot = 1;
    public float maxSpread = .1f;
    public float recoveryScale = 0.1f;

    public bool thrown = false;
    public float accumulatedSpread = 0;
    [SerializeField] private WeaponSwitcher switcher;
    public float force = 10;
    public bool inheritMomentum = false;
    public Transform fixedFiringPosition;
    public float chargedFloatCap = 1;
    [SerializeField] private Hurtbox hurtbox;
    public bool allowedToShoot = true;
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
        if (playerControlled)
        { 
            PlayerControl();
        }
        if (accumulatedSpread > baseSpread)
        {
            accumulatedSpread = Mathf.Clamp(accumulatedSpread -= Time.deltaTime * recoveryScale, baseSpread, maxSpread);
        }
        //Debug.DrawRay(muzzle.transform.position, transform.forward * 2, Color.blue);
    }
    private void PlayerControl()
    {
        if (!allowedToShoot) return;
        if (Input.GetMouseButtonDown(0))
        {
            if (thrown)
            {
                chargedFloat = 0;
            }
        }
        if (Input.GetMouseButton(0))
        {
            if (thrown && weaponTimer >= timeBetweenShots && switcher.activeWeaponType.availableAmmo > 0)
            {
                chargedFloat += Time.deltaTime;
                chargedFloat = Mathf.Clamp(chargedFloat, 0, chargedFloatCap);
            }
            else
            {
                if (weaponTimer >= timeBetweenShots && switcher.activeWeaponType.availableAmmo > 0)
                {
                    weaponTimer = 0;
                    ClientShoot();
                }
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (thrown)
            {
                if (weaponTimer >= timeBetweenShots && switcher.activeWeaponType.availableAmmo > 0)
                {
                    weaponTimer = 0;
                    ClientShoot();
                }
                chargedFloat = 0;
            }
        }
    } 
    public void AIShoot()
    {
        if (!allowedToShoot) return;
        if (weaponTimer >= timeBetweenShots && switcher.activeWeaponType.availableAmmo > 0)
        {
            weaponTimer = 0;
            ClientShoot();
        } 
    }
    public float chargedFloat = 0;
    public void ShowGunCosmeticEffects(byte id = 0)
    {
        //fire locally  
        BaseGunCosmeticEffects();
        //request server to send to other clients
        ShowGunCosmeticEffectsServerRpc(id);
    }
    [SerializeField] private Jolt camJolt;
    private void BaseGunCosmeticEffects()
    {
        float pitchChange = 0.05f;
        float pitch = Random.Range(1-pitchChange, 1+pitchChange);
        if (muzzle != null) muzzle.Play();
        if (jolt != null) jolt.FireJolt();
        if (camJolt != null) camJolt.FireJolt();
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
        switcher.SubtractAmmo(ammoPerShot);
        ProjectileOverlord(inheritMomentum);
        ShowGunCosmeticEffects();
        SuppressAI();
    }
    private void SuppressAI()
    { 
        LayerMask mask = LayerMask.GetMask("Sensor");
        Vector3 shootDirection = transform.forward;
        Ray ray = new Ray(transform.position, shootDirection); 
        RaycastHit[] m_Results = new RaycastHit[5]; 
        if (Physics.RaycastNonAlloc(transform.position, transform.forward, m_Results, 200, mask, QueryTriggerInteraction.Collide) > 0)
        {
            foreach (RaycastHit result in m_Results)
            { 
                if (result.collider != null)
                { 
                    if (result.collider.TryGetComponent(out SenseDanger sensor))
                    {
                        sensor.IncomingProjectileSensed();
                    }
                }
            }
        } 
    }
    private void ProjectileOverlord(bool inheritVelocity = false)
    { 
        for (int i = 0; i < pelletsPerShot; i++)
        {
            //float charge = chargedFloat;
            //Vector3 bodyVel = body.velocity;
            Vector3 randomOffset = new(Random.Range(-accumulatedSpread, accumulatedSpread), Random.Range(-accumulatedSpread, accumulatedSpread), Random.Range(-accumulatedSpread, accumulatedSpread));
            accumulatedSpread += spreadPerShot;

            Vector3 dir;
            Vector3 position;
            if (playerControlled)
            {
                dir = transform.forward + randomOffset;
                position = muzzle.transform.position;
            }
            else
            {
                dir = fixedFiringPosition.forward + randomOffset;
                position = fixedFiringPosition.transform.position;
                /*dir = Vector3.forward;
                position = new Vector3(0, 40, 240);*/
            }
            //Vector3 position = muzzle.transform.position;

            //Vector3 dir = transform.forward + randomOffset;
            ProjectileUmbrella(dir.normalized, position);
            /*if (inheritVelocity)
            {
                InheritProjectileUmbrella(randomOffset, bodyVel, charge);
            }
            else
            {
            }*/
        }
    } 
    /*private void InheritProjectileUmbrella(Vector3 randomOffset, Vector3 bodyVel, float charge)
    {
        ShootProjectile(IsServer, randomOffset, bodyVel, charge); //server has the "real" (damaging) projectile
        if (IsServer)
        {
            InheritProjectileClientRpc(randomOffset, bodyVel, charge); //server tells clients about projectile
        }
        else
        {
            InheritProjectileServerRpc(randomOffset, bodyVel, charge);//ask server to tell clients
        }
    }
    [ClientRpc]
    private void InheritProjectileClientRpc(Vector3 randomOffset, Vector3 bodyVel, float charge)
    {
        if (!IsOwner)
        {
            ShootProjectile(IsServer, randomOffset,  bodyVel, charge);
        }
    }
    [ServerRpc]
    private void InheritProjectileServerRpc(Vector3 randomOffset, Vector3 bodyVel, float charge)
    {
        InheritProjectileClientRpc(randomOffset, bodyVel, charge);
    }*/ 
    private void ProjectileUmbrella(Vector3 dir, Vector3 position)
    {
        ShootProjectile(IsServer, dir, position); //server has the "real" (damaging) projectile
        if (IsServer)
        { 
            ProjectileClientRpc(dir, position); //server tells clients about projectile
        }
        else
        { 
            ProjectileServerRpc(dir, position);//ask server to tell clients
        }
    }
    [ClientRpc]
    private void ProjectileClientRpc(Vector3 dir, Vector3 position)
    {
        if (!IsOwner)
        {
            ShootProjectile(IsServer, dir, position);
        }
    }
    [ServerRpc]
    private void ProjectileServerRpc(Vector3 dir, Vector3 position)
    {
        ProjectileClientRpc(dir, position);
    }
    private void ShootProjectile(bool real, Vector3 dir, Vector3 position)
    {
        Projectile proj = Instantiate(projectile, position, Quaternion.identity);
        proj.damage = damage;
        proj.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        float force = switcher.activeWeaponType.data.force; // WILL NEED TO BE CHANGED LATER IF MULTIPLE WEAPONS AVAILABLE
        proj.body.AddForce(dir.normalized * force, ForceMode.Impulse); // * force
        proj.real = real;
        //proj.id = OwnerClientId;
        proj.team = hurtbox.team.Value;
        proj.firedByPlayer = playerControlled; 
    }
    /*private void ShootProjectile(bool real, Vector3 dir, Vector3 position, Vector3 bodyVelocity = default, float charge = 0)  
    {
        Projectile proj = Instantiate(projectile, position, Quaternion.identity); 
        proj.damage = damage;

        proj.body.velocity = bodyVelocity;
        proj.transform.rotation = Quaternion.LookRotation(dir, transform.up);
        proj.body.AddForce(dir * (force + charge * force), ForceMode.Impulse);
        proj.real = real;
        proj.id = OwnerClientId;
        proj.firedByPlayer = playerControlled; 
    }*/
}
