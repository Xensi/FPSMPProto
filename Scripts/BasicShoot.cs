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
            ShowGunCosmeticEffects(); 
        }
    }
    private void BaseGunCosmeticEffects()
    { 
        if (muzzle != null) muzzle.SetActive(true);
        if (jolt != null) jolt.FireJolt();
        PlayClipAtPoint(source.clip, transform.position, 1);
    }
    public void ShowGunCosmeticEffects(byte id = 0)
    {
        //fire locally  
        BaseGunCosmeticEffects();
        //request server to send to other clients
        ShowGunCosmeticEffectsServerRpc(id);
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
