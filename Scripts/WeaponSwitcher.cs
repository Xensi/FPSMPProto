using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class WeaponSwitcher : NetworkBehaviour
{
    public List<WeaponType> weaponTypes;
    [SerializeField] private BasicShoot shooter;
    public NetworkVariable<int> equippedWeaponID = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private int shownID = 0;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            equippedWeaponID.Value = 0;
        }
        SwitchToWeaponBase(equippedWeaponID.Value);
    }
    void Update()
	{
        if (IsOwner) //client can press a number to instantly switch to different weapon
        {
            if (Input.inputString != "")
            {
                int number;
                bool is_a_number = Int32.TryParse(Input.inputString, out number);
                if (is_a_number && number >= 0 && number < 10)
                {
                    ClientSideSwitchWeapons(number - 1);
                }
            }
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
    private void ClientSideSwitchWeapons(int id = 0)
    {
        SwitchToWeaponBase(id);
        equippedWeaponID.Value = id; 
    } 
    private void SwitchToWeaponBase(int id = 0)
    {
        shownID = id;
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