using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class WeaponSwitcher : NetworkBehaviour
{
    public List<WeaponType> weaponTypes;
    [SerializeField] private BasicShoot shooter;
    private void Start()
    {
        SwitchToWeaponBase(0);
    } 
    void Update()
	{
        if (IsOwner)
        {
            if (Input.inputString != "")
            {
                int number;
                bool is_a_number = Int32.TryParse(Input.inputString, out number);
                if (is_a_number && number >= 0 && number < 10)
                {
                    SwitchWeaponUmbrella(number - 1);
                }
            }
        } 
    }
    private void SwitchWeaponUmbrella(int id = 0)
    {
        if (IsServer)
        {
            SwitchWeaponClientRpc(id);
        }
        else
        {
            SwitchWeaponServerRpc(id);
        }
    }
    [ClientRpc]
    private void SwitchWeaponClientRpc(int id = 0)
    {
        SwitchToWeaponBase(id);
    }
    [ServerRpc]
    private void SwitchWeaponServerRpc(int id = 0)
    {
        SwitchWeaponClientRpc(id);
    }

    private void SwitchToWeaponBase(int id = 0)
    {
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
        shooter.randomSpread = weapon.randomSpread;
        shooter.pelletsPerShot = weapon.pelletsPerShot;
        shooter.damage = weapon.damage;
        shooter.projectile = weapon.projectile;
        shooter.force = weapon.force;
        shooter.inheritMomentum = weapon.inheritMomentum;
    }
    private void HideWeapon(int id)
    { 
        weaponTypes[id].visualsToShow.SetActive(false);
    }
}

[Serializable]
public class WeaponType
{
    public WeaponData data;
    public GameObject visualsToShow;
    public ParticleSystem muzzleFlash;
}