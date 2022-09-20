using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TurboMarching : MonoBehaviour
{
    //необходимое
    public MeshFilter filter;
    public MeshCollider collider;
    public ComputeShader shader, destroyerShader, navShader;
    public int sizeXYZ = 80;
    public float size = 0.05f;
    public float step = 1.25f;
    public float isolevel = 0;
    public bool isDebug;
    private Vector4[] space;
    public Walkpoint[] walkpoints;
    private Triangle[] tris;
    public bool updateconnections;
    private bool updateconnectionslocal;
    // Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer walkpointsBuffer;
    ComputeBuffer triCountBuffer;
    ComputeBuffer walkCountBuffer;
    ComputeBuffer connectorsBuffer;

    //соединения и поиск пути
    public bool cX, cY, cZ;
    public bool isChecked;
    public int weight;

    //кроме основы
    public Generator generator;
    public Vector3 center;
    public List<TurboMarching> neighbors;
    public List<TurboMarching> friends;
    private FastNoiseLite noise, secondnoise, thirdnoise;

    public void Start()
    {
        center = transform.position + (new Vector3(sizeXYZ * step, sizeXYZ * step, sizeXYZ * step)) * 0.5f;
        Debug.DrawRay(center, Vector3.up, Color.magenta, 30f);
        Generate();
        UpdateMesh();
    }
    public void CheckUpdate(GameObject sph) {
        //костыль
        float supersize = 10f;
        if (sph.transform.position.x - transform.position.x + sph.transform.localScale.x < 0 || sph.transform.position.y - transform.position.y + sph.transform.localScale.y < 0 || sph.transform.position.z - transform.position.z + sph.transform.localScale.z < 0 ||
         (sph.transform.position.x - sph.transform.localScale.x) > transform.position.x + supersize || (sph.transform.position.y - sph.transform.localScale.y) > transform.position.y + supersize || (sph.transform.position.z - sph.transform.localScale.z) > transform.position.z + supersize)
        {
            return;
        }
        bool isChanged = false;
        Vector3 vec = sph.transform.position;
        float scale = sph.transform.localScale.x;
        int iscale = (int)(scale * 2.5f);
        int minx = (int)((vec.x - transform.position.x) * (1 / step) - scale * 2.5f);
        int miny = (int)((vec.y - transform.position.y) * (1 / step) - scale * 2.5f);
        int minz = (int)((vec.z - transform.position.z) * (1 / step) - scale * 2.5f);
        int maxx = (int)(minx + scale * 5f);
        int maxy = (int)(miny + scale * 5f);
        int maxz = (int)(minz + scale * 5f);
        int lx = maxx - minx;
        int ly = maxy - miny;
        int lz = maxz - minz;
        float cx = (maxx + minx) / 2 + 0.00001f;
        float cy = (maxy + miny) / 2 + 0.00001f;
        float cz = (maxz + minz) / 2 + 0.00001f;
        float multipler;
        float mx, my, mz;
        if (minx < 0) minx = 0;
        if (maxx > sizeXYZ) maxx = sizeXYZ;
        if (miny < 0) miny = 0;
        if (maxy > sizeXYZ) maxy = sizeXYZ;
        if (minz < 0) minz = 0;
        if (maxz > sizeXYZ) maxz = sizeXYZ;
        int x, y, z;
        for (x = minx; x < maxx; ++x)
        {
            //  mx = cx/ Mathf.Abs(x - cx)-1f;
            for (y = miny; y < maxy; ++y)
            {
                //    my = cy / Mathf.Abs(x - cx)-1f;
                for (z = minz; z < maxz; ++z) if (space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ].w > isolevel)
                    {
                        //          mz = cz / Mathf.Abs(z - cz)-1f;

                        //        multipler = mx + my + mz;
                        //      multipler *= 0.334f;
                        multipler = isolevel * 0.5f;
                        if (Generator.FastDist(vec, new Vector3(x, y, z) * step + transform.position, scale))
                        {
                            space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ].w -= multipler;
                            //        space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ].w -= isolevel;
                            if (space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ].w < isolevel) isChanged = true;
                        }
                    }
            }
        }

        if (isChanged) { UpdateMesh(); }
    }
    public void TurboUpdate(Vector3 centerpoint, float radius, Vector4[] points) {

        // Debug.DrawLine(point, center, Color.green, 10f);
        //point = new Vector3(point.x - transform.position.x, point.y - transform.position.y, point.z - transform.position.z) / step;


        // radius *= step;

        int numPoints = sizeXYZ * sizeXYZ * sizeXYZ;
        int numVoxelsPerAxis = sizeXYZ;
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        ComputeBuffer CaveBuffer = new ComputeBuffer(points.Length, sizeof(float) * 4);
        CaveBuffer.SetData(points);

        int threadGroupSize = 8;
        int numThreadsPerAxis = Mathf.CeilToInt((numPoints) / (float)threadGroupSize);
        //int numThreadsPerAxis = Mathf.CeilToInt((numVoxelsPerAxis) / (float)threadGroupSize);
        //print(numThreadsPerAxis);

        pointsBuffer.SetData(space);

        int _kernelindex = destroyerShader.FindKernel("boom");

        destroyerShader.SetBuffer(_kernelindex, "points", pointsBuffer);
        destroyerShader.SetBuffer(_kernelindex, "caves", CaveBuffer);
        destroyerShader.SetVector("worldpos", transform.position);
        destroyerShader.SetFloat("step", step);
        destroyerShader.SetInt("numPointsPerAxis", numVoxelsPerAxis);
        destroyerShader.SetFloat("radius", radius);
        destroyerShader.SetVector("desPoint", centerpoint);

        //destroyerShader.Dispatch(_kernelindex,numThreadsPerAxis,numThreadsPerAxis,numThreadsPerAxis);
        destroyerShader.Dispatch(_kernelindex, numThreadsPerAxis, 1, 1);

        space = new Vector4[numPoints];
        pointsBuffer.GetData(space, 0, 0, numPoints);
        //pointsBuffer.GetData(_bspace);
        //for (int i = 0; i < space.Length; ++i) { space[i] = _bspace[i]; }
        int id;
        for (int i = 0; i < 40; ++i) {
            id = Random.Range(0, space.Length);
            if (space[id].w < isolevel - 0.05f)
            {
                generator.bugspawnpoints.Add(space[id] + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0));
                if (Random.Range(0, 50) == 0) { generator.startpoints.Add(space[id] + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0)); }
            }
            if (space[id].w > isolevel && space[id].w < isolevel + 0.05f && Random.Range(0, 5) == 0)
            {
                Debug.DrawLine(space[id] + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0), center, Color.cyan, 10f);
                generator.orepoints.Add(space[id] + new Vector4(transform.position.x, transform.position.y, transform.position.z, 0));
            }
        }

        pointsBuffer.Release();
        CaveBuffer.Release();
        CaveBuffer.Dispose();
    }
    public void Generate()
    {
        space = new Vector4[sizeXYZ * sizeXYZ * sizeXYZ];
        noise = new FastNoiseLite();
        secondnoise = new FastNoiseLite();
        thirdnoise = new FastNoiseLite();
        noise.SetSeed(generator.seed);
        secondnoise.SetSeed(-generator.seed);
        thirdnoise.SetSeed((int)(((long)generator.seed) * 125 / 144));
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        secondnoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        thirdnoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);


        List<Vector4> cavepoints = new List<Vector4>();
        for (int i = 0; i < generator.cavepoints.Count; ++i)
        {
            cavepoints.Add(new Vector4(generator.cavepoints[i].x, generator.cavepoints[i].y, generator.cavepoints[i].z, 16));
        }
        List<Vector4> tunnelpoints = new List<Vector4>();
        for (int i = 0; i < generator.tunnelpoints.Count; ++i)
        {
            tunnelpoints.Add(new Vector4(generator.tunnelpoints[i].x, generator.tunnelpoints[i].y, generator.tunnelpoints[i].z, 7));
        }

        int x, y, z;
        for (x = 0; x < sizeXYZ; ++x)
            for (y = 0; y < sizeXYZ; ++y)
                for (z = 0; z < sizeXYZ; ++z)
                {
                    space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ] = new Vector4(x * step, y * step, z * step, 10);
                    //   space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ] = new Vector4(x * step, y * step, z * step, (noise.GetNoise((x * step + transform.position.x) * size, (y * step + transform.position.y) * size, (z * step + transform.position.z) * size) + 1f) * 10f);
                }
        tunnelpoints.AddRange(cavepoints);
        Vector4[] arr = tunnelpoints.ToArray();
        TurboUpdate(Generator.center, 20, arr);
        /*
        for (x = 0; x < sizeXYZ; ++x)
            for (y = 0; y < sizeXYZ; ++y)
                for (z = 0; z < sizeXYZ; ++z)
                {
                    space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ] = new Vector4(x * step, y * step, z * step, (noise.GetNoise((x * step + transform.position.x) * size, (y * step + transform.position.y) * size, (z * step + transform.position.z) * size) + 1f) * 10f);
                    //space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ] = new Vector4(x*step,y * step, z * step, Mathf.Sin((x * step + transform.position.x) * size+ (y * step + transform.position.y) * size+ (z * step + transform.position.z) * size));
                }*/

    }
    private void OnDestroy()
    {
        pointsBuffer.Dispose();
        triangleBuffer.Dispose();
        walkpointsBuffer.Dispose();
        walkCountBuffer.Dispose();
        triCountBuffer.Dispose();
        triangleBuffer.Dispose();
    }
    public void UpdateMesh()
    {

        int numPoints = sizeXYZ * sizeXYZ * sizeXYZ;
        int numVoxelsPerAxis = sizeXYZ - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        int[] cons = new int[3];
        // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
        // Otherwise, only create if null or if size has changed

        triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        walkCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        walkpointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4 + sizeof(int), ComputeBufferType.Append);
        connectorsBuffer = new ComputeBuffer(3, sizeof(int));
        int threadGroupSize = 8;

        Mesh mesh = new Mesh();

        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        //int numThreadsPerAxis = 8;

        pointsBuffer.SetData(space);
        connectorsBuffer.SetData(cons);

        int _kernelindex = shader.FindKernel("March");

        triangleBuffer.SetCounterValue(0);
        walkpointsBuffer.SetCounterValue(0);
        shader.SetBuffer(_kernelindex, "points", pointsBuffer);
        shader.SetBuffer(_kernelindex, "triangles", triangleBuffer);
        shader.SetBuffer(_kernelindex, "walkpoints", walkpointsBuffer);
        shader.SetBuffer(_kernelindex, "connectors", connectorsBuffer);
        shader.SetInt("numPointsPerAxis", sizeXYZ);
        shader.SetFloat("isoLevel", isolevel);
        shader.SetInt("seed", generator.seed);
        shader.SetVector("chunkpos", transform.position);

        shader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        //навигационные кубы

        ComputeBuffer.CopyCount(walkpointsBuffer, walkCountBuffer, 0);
        int[] walkCountArray = { 0 };
        walkCountBuffer.GetData(walkCountArray);
        int numWalks = walkCountArray[0];

        walkpoints = new Walkpoint[numWalks];
        walkpointsBuffer.GetData(walkpoints, 0, 0, numWalks);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];


        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        // получение коннекторов
        connectorsBuffer.GetData(cons);
        cX = cons[0] == 1;
        cY = cons[1] == 1;
        cZ = cons[2] == 1;


        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateTangents();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        filter.mesh = mesh;
        collider.sharedMesh = mesh;

        triangleBuffer.Release();
        pointsBuffer.Release();
        triCountBuffer.Release();
        walkCountBuffer.Release();
        walkpointsBuffer.Release();
        walkpointsBuffer.Dispose();
        connectorsBuffer.Release();
        connectorsBuffer.Dispose();
    }
    public void UpdateFriends()
    {
        friends = new List<TurboMarching>();
        for (int i = 0; i < neighbors.Count; ++i)
        {
            if (neighbors[i].transform.position.x < transform.position.x && cX) { friends.Add(neighbors[i]); continue; }
            if (neighbors[i].transform.position.x > transform.position.x && neighbors[i].cX) { friends.Add(neighbors[i]); continue; }
            if (neighbors[i].transform.position.y < transform.position.y && cY) { friends.Add(neighbors[i]); continue; }
            if (neighbors[i].transform.position.y > transform.position.y && neighbors[i].cY) { friends.Add(neighbors[i]); continue; }
            if (neighbors[i].transform.position.z < transform.position.z && cZ) { friends.Add(neighbors[i]); continue; }
            if (neighbors[i].transform.position.z > transform.position.z && neighbors[i].cZ) { friends.Add(neighbors[i]); continue; }
        }
        for (int i = 0; i < friends.Count; ++i)
        {
            //Debug.DrawLine(center, friends[i].center, Color.magenta, 10f);
        }
    }
    public void SetNavigation(Vector3 startpoint)
    {
        int _kernelindex = navShader.FindKernel("Clear");
        ComputeBuffer navbuffer = new ComputeBuffer(walkpoints.Length, sizeof(float) * 4 + sizeof(int));
        navbuffer.SetData(walkpoints);
        navShader.SetBuffer(_kernelindex, "points", navbuffer);
        int numThreadsPerAxis = Mathf.CeilToInt(walkpoints.Length / (float)8);
        navShader.Dispatch(_kernelindex, numThreadsPerAxis, 1, 1);
        _kernelindex = navShader.FindKernel("Splat");
        navShader.SetVector("startpoint", startpoint - transform.position);
        navShader.SetBuffer(_kernelindex, "points", navbuffer);
        navShader.Dispatch(_kernelindex, numThreadsPerAxis, 1, 1);
        _kernelindex = navShader.FindKernel("Set");
        navShader.SetBuffer(_kernelindex, "points", navbuffer);
        for (int i = 0; i < 50; ++i)
        {
            navShader.SetInt("iter", i);
            navShader.Dispatch(_kernelindex, numThreadsPerAxis, 1, 1);
        }/**/
        print("есть обработка!");
        for (int i = 0; i < 10; ++i) { print(walkpoints[Random.Range(0, walkpoints.Length)].iter); }
        walkpoints = new Walkpoint[walkpoints.Length];
        navbuffer.GetData(walkpoints, 0, 0, walkpoints.Length);
        navbuffer.Release();
    }
    Generator.walkpointneighbors[] walkpointneighbors;
    public List<Vector3> GetPath(Vector3 endpoint)
    {
        List<Vector3> outlist = new List<Vector3>();
        List<int> buffer = new List<int>();
        HashSet<int> secondbuffer = new HashSet<int>();
        walkpointneighbors = new Generator.walkpointneighbors[walkpoints.Length];
        int id=0;
        for (int i = 0; i < walkpoints.Length; ++i)
        {
            walkpointneighbors[i] = new Generator.walkpointneighbors();
            if (Vector3.Distance(walkpoints[i].pos + transform.position, endpoint) < 1)
            {
                id = i;
                break;
            }
            for (int ii = 0; ii < walkpoints.Length; ++ii) if (i != ii)
                {
                    for (int j = 0; j < neighborsTable.Length; ++j)
                    {
                        if (walkpoints[i].pos + neighborsTable[j] == walkpoints[ii].pos)
                        {
                            walkpointneighbors[i].Add(ii);
                            break;
                        }
                    }
                }
            walkpointneighbors[i].Optimise();
        }
        List<int> pathchain = GetPathChain(id);
        for (int i = 0; i < pathchain.Count; ++i) { outlist.Add(walkpoints[pathchain[i]].pos); }

        return outlist;
    }
    private List<int> GetPathChain(int id) 
    {
        List<int> outchain = new List<int>();
        if (walkpoints[id].weight == 0) { outchain.Add(id);return outchain; }
        for (int i = 0; i < walkpointneighbors[id].Length; ++i) 
        {
            if (walkpoints[walkpointneighbors[id][i]].weight < walkpoints[id].weight) 
            {
                return GetPathChain(walkpointneighbors[id][i]);
            }
        }
        throw new System.Exception("PathNotFoundException, лол");
    }
    private void OnDrawGizmos()
    {
        if (isDebug&&Application.isPlaying)
        {
            Gizmos.color = new Color(0.576f, 0.439f, 0.203f,0.3f);
            Vector3 one = new Vector3(0.125f, 0.125f, 0.125f);
            int x, y, z;/*
            for (x = 0; x < sizeXYZ; ++x)
                for (y = 0; y < sizeXYZ; ++y)
                    for (z = 0; z < sizeXYZ; ++z)
                        //if (space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ].w > isolevel)//&& space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ].w< isolevel+0.01f)
                        {
                            Gizmos.DrawCube(new Vector3((x * step + transform.position.x), (y * step + transform.position.y), (z * step + transform.position.z)), one);
                        }*/
            for (int i = 0; i < walkpoints.Length; ++i)
            {
                Gizmos.color = new Color(walkpoints[i].weight*0.1f,0,1- walkpoints[i].weight*0.1f);
                Gizmos.DrawCube(new Vector3((walkpoints[i].x  + transform.position.x), (walkpoints[i].y  + transform.position.y), (walkpoints[i].z  + transform.position.z)), one);
            }
        }
        if (Application.isEditor) if (updateconnectionslocal!=updateconnections) {
                updateconnectionslocal = updateconnections;
                filter = GetComponent<MeshFilter>();
                collider = GetComponent<MeshCollider>();
            }
        if (Application.isEditor)
        {
            bool isSelected = Selection.Contains(gameObject);
            Gizmos.color = isSelected ? new Color(0.168f, 0.5814968f, 0.93741f, 0.24f) : new Color(0.465f, 0.21978f, 0.1678f, 0.24f);
            /*if(cX)Debug.DrawRay(center,new Vector3(-10,0,0),Color.blue);
            if(cY)Debug.DrawRay(center,new Vector3(0,-10,0),Color.blue);
            if(cZ)Debug.DrawRay(center,new Vector3(0,0,-10),Color.blue);*/
            //Gizmos.DrawCube(transform.position + new Vector3(4.875f, 4.875f, 4.875f), new Vector3(9.75f, 9.75f, 9.75f));
        }
    }
    private void OnGUI()
    {
        if(isDebug)GUI.Box(new Rect(Screen.width - 200, Screen.height - 20, 200, 20), ""+walkpoints.Length);
    }
    struct Triangle
    {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }
    public struct Walkpoint
    {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 pos;
        public float weight;
        public int iter;
        public float x { get { return pos.x; } }
        public float y { get { return pos.y; } }
        public float z { get { return pos.z; } }
    }
    public static readonly Vector3[] neighborsTable = {
        new Vector3(-0.5f,-0.5f,-0.5f),
        new Vector3(-0.5f,-0.5f,0),
        new Vector3(-0.5f,-0.5f,0.5f),
        new Vector3(-0.5f,0,-0.5f),
        new Vector3(-0.5f,0,0),
        new Vector3(-0.5f,0,0.5f),
        new Vector3(-0.5f,0.5f,-0.5f),
        new Vector3(-0.5f,0.5f,0),
        new Vector3(-0.5f,0.5f,0.5f),
        new Vector3(0,-0.5f,-0.5f),
        new Vector3(0,-0.5f,0),
        new Vector3(0,-0.5f,0.5f),
        new Vector3(0,0,-0.5f),
        new Vector3(0,0,0.5f),
        new Vector3(0,0.5f,-0.5f),
        new Vector3(0,0.5f,0),
        new Vector3(0,0.5f,0.5f),
        new Vector3(0.5f,-0.5f,-0.5f),
        new Vector3(0.5f,-0.5f,0),
        new Vector3(0.5f,-0.5f,0.5f),
        new Vector3(0.5f,0,-0.5f),
        new Vector3(0.5f,0,0),
        new Vector3(0.5f,0,0.5f),
        new Vector3(0.5f,0.5f,-0.5f),
        new Vector3(0.5f,0.5f,0),
        new Vector3(0.5f,0.5f,0.5f)
    };
}
