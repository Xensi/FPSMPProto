using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
public class WeaponSwitcher : NetworkBehaviour
{
    public bool playerControlled = true;
    public List<WeaponType> weaponTypes;
    [SerializeField] private BasicShoot shooter;
    public NetworkVariable<int> equippedWeaponID = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private int shownID = 0;
    [SerializeField] private TMP_Text gunText;
    [SerializeField] private TMP_Text ammoText;
    private float scrollTimer = 0;
    public WeaponType activeWeaponType;
    public bool reloading = false;
    public float reloadTime = 1;
    private float reloadTimer = 0;
    [SerializeField] private AudioSource source;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            equippedWeaponID.Value = 0;
            foreach (WeaponType item in weaponTypes)
            {
                item.availableAmmo = item.data.magSize;
                item.spareAmmo = item.data.startingAmmo;
            }


        }
        SwitchToWeaponBase(equippedWeaponID.Value);
    }
    void Update()
	{
        if (playerControlled)
        {
            if (IsOwner) //client can press a number to instantly switch to different weapon
            {
                KeyPressSwitchWeapons();
                ScrollWheelSwitchWeapons();
                CheckReload();
                ReloadProgress();
            }
        }
        else
        {
            if (IsOwner)
            {
                ReloadProgress();
            } 
        }

        if (IsOwner)
        {

        }
        else
        {
            if (shownID != equippedWeaponID.Value) //if what is shown is different than actual, fix that
            {
                shownID = equippedWeaponID.Value;
                ClientSideSwitchWeapons(shownID);
            }
        }
    }
    private void ReloadProgress()
    {
        if (reloading)
        {
            if (reloadTimer < reloadTime)
            {
                reloadTimer += Time.deltaTime;
            }
            else
            {
                reloading = false;
                PerformReload();
            }
        }
        shooter.allowedToShoot = !reloading;
    }
    private void KeyPressSwitchWeapons()
    { 
        if (Input.inputString != "")
        {
            bool is_a_number = Int32.TryParse(Input.inputString, out int number);
            if (is_a_number && number >= 0 && number < 10)
            {
                ClientSideSwitchWeapons(number - 1);
            }
        }
    }
    private void CheckReload()
    { 
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartReload();
        }
    }
    public void StartReload()
    { 
        if (!reloading)
        {
            reloading = true;
            reloadTimer = 0; 
        }
    }
    private AudioClip reloadSound;
    private void PerformReload()
    { 
        if (activeWeaponType.spareAmmo >= activeWeaponType.data.magSize)
        {
            activeWeaponType.availableAmmo = activeWeaponType.data.magSize;
            activeWeaponType.spareAmmo -= activeWeaponType.data.magSize;
        }
        else if (activeWeaponType.spareAmmo > 0)
        {
            activeWeaponType.availableAmmo = activeWeaponType.spareAmmo;
            activeWeaponType.spareAmmo = 0;
        }

        UpdateAmmoCountGUI();
        PlayReloadSound();
    }
    private void PlayReloadSound()
    {
        if (reloadSound != null)
        {
            if (IsOwner)
            {
                source.PlayOneShot(reloadSound);
            }
            else
            {
                PlayClipAtPoint(reloadSound, transform.position, 1);
            }
        }
    }
    private void UpdateAmmoCountGUI()
    { 
        if (playerControlled)
        { 
            ammoText.SetText("{0}/{1}", activeWeaponType.availableAmmo, activeWeaponType.spareAmmo);
        }
    }
    private void ScrollWheelSwitchWeapons()
    {
        float scrollDelay = .1f;
        if (scrollTimer < scrollDelay)
        {
            scrollTimer += Time.deltaTime;
        }
        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0 && scrollTimer >= scrollDelay)
        {
            scrollTimer = 0;
            int val = equippedWeaponID.Value;
            val += Mathf.RoundToInt(Mathf.Sign(Input.mouseScrollDelta.y));
            val = Mathf.Clamp(val, 0, weaponTypes.Count - 1); 
            ClientSideSwitchWeapons(val);
        } 
    }
    private void ClientSideSwitchWeapons(int id = 0)
    {
        SwitchToWeaponBase(id);
        equippedWeaponID.Value = id;
        CancelReload();
    } 
    private void CancelReload()
    {
        reloading = false;
        reloadTimer = 0;
    }
    private void SwitchToWeaponBase(int id = 0)
    {
        shooter.chargedFloat = 0;
        shownID = id;
        activeWeaponType = weaponTypes[id];
        for (int i = 0; i < weaponTypes.Count; i++)
        {
            if (i == id)
            {
                ShowWeapon(i);
            }
            else
            {
                HideWeapon(i);
            }
        }
    }
    private void ShowWeapon(int id)
    {
        WeaponData weapon = weaponTypes[id].data;
        weaponTypes[id].visualsToShow.SetActive(true);
        shooter.muzzle = weaponTypes[id].muzzleFlash;
        shooter.timeBetweenShots = weapon.timeBetweenShots;
        shooter.baseSpread = weapon.baseSpread;
        shooter.accumulatedSpread = weapon.baseSpread; //just set it now
        shooter.spreadPerShot = weapon.spreadPerShot;
        shooter.pelletsPerShot = weapon.pelletsPerShot;
        shooter.damage = weapon.damage;
        shooter.projectile = weapon.projectile;
        shooter.force = weapon.force;
        shooter.inheritMomentum = weapon.inheritMomentum;
        shooter.source.clip = weapon.gunSound;
        shooter.thrown = weapon.thrown;
        shooter.ammoPerShot = weapon.ammoPerShot;
        shooter.maxSpread = weapon.maxSpread;
        shooter.recoveryScale = weapon.recoveryScale; 
        reloadTime = weapon.reloadTime;
        reloadSound = weapon.reloadSound;
        shooter.jolt.joltX = weapon.joltX;
        shooter.jolt.joltY = weapon.joltY;
        shooter.jolt.joltZ = weapon.joltZ;
        shooter.jolt.joltReturnSpeed = weapon.joltReturnSpeed; 
        if (playerControlled)
        {
            gunText.text = weapon.name;
        }
        UpdateAmmoCountGUI();
    }
    private void HideWeapon(int id)
    {
        weaponTypes[id].visualsToShow.SetActive(false);
    } 
    public void SubtractAmmo(int amount)
    {
        activeWeaponType.availableAmmo -= amount;
        UpdateAmmoCountGUI();
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
}

[Serializable]
public class WeaponType
{
    public WeaponData data;
    public GameObject visualsToShow;
    public ParticleSystem muzzleFlash;
    public int availableAmmo = 0; //shootable
    public int spareAmmo = 0; //reloadable
}