using UnityEngine;
using System.Collections.Generic;

public class Weapon : MonoBehaviour, IHaveFirePoint
{
    [Header("총구 위치들")]
    [SerializeField] private List<Transform> firePoints;

    public List<Transform> FirePoints => firePoints;
}