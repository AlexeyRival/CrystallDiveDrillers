using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public GameObject[] chunks;
    public marchingspace[] marchingspaces;
    private GameObject[] destroyers;
    private Vector3[] centers;
    private int[] sizes;
    public void Recalculate() {
        List<marchingspace> ms = new List<marchingspace>();
        centers = new Vector3[chunks.Length];
        sizes = new int[chunks.Length];
        for (int i = 0; i < chunks.Length; ++i) {
            ms.Add(chunks[i].GetComponent<marchingspace>());
            centers[i] = chunks[i].GetComponent<marchingspace>().center;
            sizes[i] = chunks[i].GetComponent<marchingspace>().sizeX/4*3;
        }
        marchingspaces = ms.ToArray();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (GameObject.FindGameObjectWithTag("Destroyer")) {
            destroyers = GameObject.FindGameObjectsWithTag("Destroyer");
            for (int d = 0; d < destroyers.Length; ++d)
            {
                for (int i = 0; i < marchingspaces.Length; ++i)
                {
                    if (Vector3.Distance(destroyers[d].transform.position, centers[i]) < sizes[i] + destroyers[d].transform.localScale.x)
                    {
                        marchingspaces[i].CheckUpdate(destroyers[d]);
                    }
                }
            }
        }
    }
}
