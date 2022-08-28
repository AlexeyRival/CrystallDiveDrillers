﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public GameObject[] chunks;
    public marchingspace[] marchingspaces;
    public TurboMarching[] turboMarchings;
    public GameObject spheredestroyer;
    private GameObject[] destroyers;
    private Vector3[] centers;
    private int[] sizes;
    public bool TURBOMODE;


    public void Recalculate()
    {
        if (!TURBOMODE)
        {
            List<marchingspace> ms = new List<marchingspace>();
            centers = new Vector3[chunks.Length];
            sizes = new int[chunks.Length];
            for (int i = 0; i < chunks.Length; ++i)
            {
                ms.Add(chunks[i].GetComponent<marchingspace>());
                centers[i] = chunks[i].GetComponent<marchingspace>().center;
                sizes[i] = chunks[i].GetComponent<marchingspace>().sizeX / 4 * 3;
            }
            for (int i = 0; i < ms.Count; ++i)
            {
                for (int ii = i; ii < ms.Count; ++ii) if (i != ii)
                    {
                        if (Generator.FastDist(ms[i].transform.position, ms[ii].transform.position, ms[i].sizeX * ms[i].sizeX + 1))
                        {
                            ms[i].neighbors.Add(ms[ii]);
                            ms[ii].neighbors.Add(ms[i]);
                        }
                    }
            }
            for (int i = 0; i < ms.Count; ++i)
            { ms[i].BakeMesh(); }
            marchingspaces = ms.ToArray();
        }
        else
        {
            GameObject[] spaces = GameObject.FindGameObjectsWithTag("Chunk");
            turboMarchings = new TurboMarching[spaces.Length];
            centers = new Vector3[spaces.Length];
            sizes = new int[spaces.Length];
            for (int i = 0; i < spaces.Length; ++i)
            {
                turboMarchings[i] = spaces[i].GetComponent<TurboMarching>();
                centers[i] = spaces[i].GetComponent<TurboMarching>().center;
                sizes[i] = spaces[i].GetComponent<TurboMarching>().sizeXYZ;
            }
            Generator generator = GameObject.Find("ChungGenerator").GetComponent<Generator>();
            for (int i = 0; i < turboMarchings.Length; ++i)
            {
                for (int ii = i; ii < turboMarchings.Length; ++ii) if (i != ii)
                    {
                        if (Generator.FastDist(turboMarchings[i].transform.position, turboMarchings[ii].transform.position, turboMarchings[i].sizeXYZ * turboMarchings[i].sizeXYZ + 1))
                        {
                            turboMarchings[i].neighbors.Add(turboMarchings[ii]);
                            turboMarchings[ii].neighbors.Add(turboMarchings[i]);
                        }
                    }
            }

        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (GameObject.FindGameObjectWithTag("Destroyer")) {
            destroyers = GameObject.FindGameObjectsWithTag("Destroyer");
            for (int d = 0; d < destroyers.Length; ++d)
            {
                if (!TURBOMODE)
                {
                    for (int i = 0; i < marchingspaces.Length; ++i)
                    {
                        if (Vector3.Distance(destroyers[d].transform.position, centers[i]) < sizes[i] + destroyers[d].transform.localScale.x)
                        {
                            marchingspaces[i].CheckUpdate(destroyers[d]);
                        }
                    }
                }
                else 
                {
                    for (int i = 0; i < turboMarchings.Length; ++i) 
                    {
                        if (Vector3.Distance(destroyers[d].transform.position, turboMarchings[i].center) < turboMarchings[i].sizeXYZ + destroyers[d].transform.localScale.x) 
                        {
                            turboMarchings[i].CheckUpdate(destroyers[d]);
                        }
                    }
                }
            }
        }
    }
}
