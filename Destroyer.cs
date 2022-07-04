using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyer : MonoBehaviour
{
    public List<Resource> resources;
    public GameObject effector;
    private void Start()
    {
        Destroy(Instantiate(effector, transform.position, transform.rotation), 2);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Resource")) {
            int id = int.Parse(collision.gameObject.name);
            resources[id].SpawnItem(transform.position);
            Destroy(collision.gameObject);
        }
    }
}
