using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class grenade : NetworkBehaviour
{
    public GameObject explosion,destroyer;    
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
    private void OnDestroy()
    {
        if (isServer)
        {
            GameObject ob = Instantiate(destroyer, transform.position, transform.rotation);
            Destroy(ob,0.1f);
            NetworkServer.Spawn(ob);
        }
        Destroy(Instantiate(explosion, transform.position, transform.rotation),1f);
    }
}
