using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ШТУКИ/БАБАХА")]
public class Weapon : ScriptableObject
{
    public string weaponname;
    public Sprite icon;
    public float firerate;
    public int dmg;
    public int ammo;
    public int maxammo;
    public float reloadtime;
    public int sound;
    public int reloadsound;
    public float recoil;
    public bool haveRotor;
    public bool haveScope;
    public bool haveProjectile;
    public GameObject projectile;
    public float projectilespeed;
    public int firespershoot;
    public GameObject firesplash;
    public GameObject bulletmark;
}
