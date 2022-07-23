using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ШТУКИ/сложность")]
public class difficulty : ScriptableObject
{
    public string difname;
    public int bonus;
    public Sprite icon;
}   