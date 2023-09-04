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
        SwitchToWeapon(0);
    }
    void Update()
	{
        if (Input.inputString != "")
        {
            int number;
            bool is_a_number = Int32.TryParse(Input.inputString, out number);
            if (is_a_number && number >= 0 && number < 10)
            {
                SwitchToWeapon(number-1);
            }
        }
    }
    private void SwitchToWeapon(int id = 0)
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