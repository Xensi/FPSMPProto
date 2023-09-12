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
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            equippedWeaponID.Value = 0;
            foreach (WeaponType item in weaponTypes)
            {
                item.availableAmmo = item.data.startingAmmo;
            }


        }
        SwitchToWeaponBase(equippedWeaponID.Value);
    }
    void Update()
	{
        if (!playerControlled) return;
        if (IsOwner) //client can press a number to instantly switch to different weapon
        {
            KeyPressSwitchWeapons();
            ScrollWheelSwitchWeapons();
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
    private float scrollTimer = 0;
    
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
        if (playerControlled)
        { 
            gunText.text = weapon.name;
            ammoText.SetText("{0}", activeWeaponType.availableAmmo);
        }
    }
    private void HideWeapon(int id)
    {
        weaponTypes[id].visualsToShow.SetActive(false);
    } 
    public void SubtractAmmo(int amount)
    {
        activeWeaponType.availableAmmo -= amount;
        if (playerControlled)
        {
            ammoText.SetText("{0}", activeWeaponType.availableAmmo); 
        }
    }
    public WeaponType activeWeaponType;
}

[Serializable]
public class WeaponType
{
    public WeaponData data;
    public GameObject visualsToShow;
    public ParticleSystem muzzleFlash;
    public int availableAmmo = 0;
}