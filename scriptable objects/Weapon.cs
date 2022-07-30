using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ШТУКИ/БАБАХА")]
public class Weapon : ScriptableObject
{
    public string weaponname;
    public float firerate;
    public int dmg;
    public int ammo;
    public float reloadtime;
    public AudioClip sound;
    public float recoil;
    public GameObject firesplash;
    public GameObject bulletmark;
}
