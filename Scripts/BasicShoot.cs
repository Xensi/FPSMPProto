using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BasicShoot : NetworkBehaviour
{
    [SerializeField] private LayerMask canHitMask;
    [SerializeField] private AudioSource source;
    [SerializeField] private GameObject muzzle;
    [SerializeField] private Jolt jolt;
    private float range = Mathf.Infinity;
    private int bulletDamage = 1;
    private void Update()
    {
         if (Input.GetMouseButtonDown(0))
        {
            ClientShoot();
            PlayShootSound();
            JoltWeapon();
            ShowMuzzleFlash();
        }
    }
    private void ShowMuzzleFlash()
    {
        if (muzzle != null) muzzle.SetActive(true);
        if (IsServer)
        {
            ShowMuzzleFlashClientRpc();
        }
        else
        {
            ShowMuzzleFlashServerRpc();
        }
    }
    private void BaseMuzzleFlash()
    {
        if (!IsOwner)
        { 
            if (muzzle != null) muzzle.SetActive(true);
        }
    }
    [ClientRpc]
    private void ShowMuzzleFlashClientRpc() //tell all clients to show muzzle flash
    {
        BaseMuzzleFlash();
    }
    [ServerRpc]
    private void ShowMuzzleFlashServerRpc() //ask server to tell clients
    {
        ShowMuzzleFlashClientRpc();
    }
    private void JoltWeapon()
    {
        if (jolt != null) jolt.FireJolt();
    }
    private void PlayShootSound()
    {
        //client plays sound
        source.PlayOneShot(source.clip); 
        /*if (IsServer) //tell all other clients to play sound at this position
        { 
        }
        else
        {

        }*/
    }
    /*[ClientRpc]
    private void ShootSoundClientRpc()
    {
        AudioSource.PlayClipAtPoint(source.clip, );

    }
    [ServerRpc]
    private void ShootSoundServerRpc()
    {

    }*/
    private void ClientShoot() //shoot raycast at target client side
    {
        Vector3 shootDirection = transform.forward;
        
        Ray ray = new Ray(transform.position, shootDirection);
        RaycastHit hit; //otherwise, make raycast
        Debug.DrawRay(transform.position, shootDirection, Color.red, 1);

        if (Physics.Raycast(ray, out hit, range, canHitMask)) //if raycast hits something  
        { 
            if (hit.collider.TryGetComponent(out Hurtbox hurtbox))
            {
                DealDamageUmbrella(bulletDamage, hurtbox);
            }
        }
    }
    private void DealDamageUmbrella(int damage, Hurtbox hurtbox)
    { 
        if (IsServer) //server can write network variables
        {
            DealDamage(bulletDamage, hurtbox);
        }
        else //ask server to write network variable
        {
            DealDamageServerRpc(bulletDamage, hurtbox);
        }
    }
    private void DealDamage(int damage, Hurtbox hurtbox)
    { 
        hurtbox.playerHP.Value -= damage;
    }
    [ServerRpc]
    private void DealDamageServerRpc(int damage, NetworkBehaviourReference hurtbox)
    {
        if (hurtbox.TryGet(out Hurtbox hurt))
        { 
            DealDamage(damage, hurt);
        }
    }
}
