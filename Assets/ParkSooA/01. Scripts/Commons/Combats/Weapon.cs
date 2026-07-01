using UnityEngine;
using System.Collections.Generic;

public class Weapon : MonoBehaviour, IWeapon
{
    [Header("총구 위치들")]
    [SerializeField] private List<Transform> firePoints;

    public List<Transform> FirePoints => firePoints;
}