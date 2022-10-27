using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TurboMarching : MonoBehaviour
{

    //необходимое
    public MeshFilter filter;
    public MeshCollider collider;
    public ComputeShader shader, destroyerShader, navShader, frenderShader, EnterpriseShader;
    public int sizeXYZ = 80;
    public float size = 0.05f;
    public float step = 1.25f;
    public float isolevel = 0;
    public bool isDebug;
    private Vector4[] space;
    public Walkpoint[] walkpoints;
    private Triangle[] tris;
    private int[] trisconnections;
    public bool updateconnections;
    private bool updateconnectionslocal;

    public GameObject[] allRotationObjects, grasses,flowers;

    // Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer walkpointsBuffer;
    ComputeBuffer triCountBuffer;
    ComputeBuffer walkCountBuffer;
    ComputeBuffer connectorsBuffer;
    ComputeBuffer walkpointsdatas;

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

    //ссылки на всю траву и прочие объекты
    private Dictionary<Vector3, GameObject> decorations;

    public void Start()
    {
        center = transform.position + (new Vector3(sizeXYZ * step, sizeXYZ * step, sizeXYZ * step)) * 0.5f;
        Debug.DrawRay(center, Vector3.up, Color.magenta, 30f);
        decorations = new Dictionary<Vector3, GameObject>();
        Generate();
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
        multipler = isolevel * 0.5f;
        float scl = scale * (1 / step)*4;
        Vector3 cvec = new Vector3(cx, cy, cz);
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
                        //if (Generator.FastDist(vec, new Vector3(x, y, z) * step + transform.position, scale))
                        if (Generator.FastDist(cvec, new Vector3(x, y, z), scl))
                        {
                            space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ].w -= multipler;
                            if (decorations.ContainsKey(new Vector3(x, y, z)*step)) { Destroy(decorations[new Vector3(x, y, z) * step]); }
                            //        space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ].w -= isolevel;
                            if (!isChanged&&space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ].w < isolevel) isChanged = true;
                        }
                    }
            }
        }

        if (isChanged) { UpdateMesh(); }
    }
    public void TurboUpdate(Vector3 centerpoint, float radius, Vector4[] points) {
        noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
        noise.SetSeed(generator.seed);
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


        int i;

        List<Vector4> cavepoints = new List<Vector4>();
        for (i = 0; i < generator.cavepoints.Count; ++i)
        {
            cavepoints.Add(new Vector4(generator.cavepoints[i].x, generator.cavepoints[i].y, generator.cavepoints[i].z, 16));
        }
        List<Vector4> tunnelpoints = new List<Vector4>();
        for (i = 0; i < generator.tunnelpoints.Count; ++i)
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
        UpdateMesh();
        int l = walkpoints.Length;
        float n,fn;
        for (i = 0; i < l; ++i) if (i % 3 == 0)// && Random.Range(0, 8) == 0)
        {
                n = noise.GetNoise(walkpoints[i].x + transform.position.x, walkpoints[i].y + transform.position.y, walkpoints[i].z + transform.position.z);
                fn = secondnoise.GetNoise(walkpoints[i].x + transform.position.x, walkpoints[i].y + transform.position.y, walkpoints[i].z + transform.position.z);
                /* if (noise.GetNoise(walkpoints[i].x + transform.position.x, walkpoints[i].y + transform.position.y, walkpoints[i].z + transform.position.z) < -0.4f)
                 {
                     Instantiate(allRotationObjects[Random.Range(0, allRotationObjects.Length)], walkpoints[i].pos + transform.position, Quaternion.Euler(walkpoints[i].Yangle * 0, 0, 90 * walkpoints[i].angle), transform).name=walkpoints[i].angle+":"+walkpoints[i].Yangle;
                 }
                 else */
                if (QualitySettings.GetQualityLevel()>2)if (walkpoints[i].angle == 240 && n > 0f) 
                {
                        //  print("тра ва");
                    decorations.Add(walkpoints[i].pos,Instantiate(grasses[(int)(n*20)%grasses.Length], walkpoints[i].pos + transform.position, Quaternion.Euler(0, (fn * 100000f) % 100, 0), transform));
                }
                if (walkpoints[i].angle == 240 && (n < -0.4f||n>0.4f)&& (int)(Mathf.Sin(-n) * 200) % 9 ==0) 
                {
                    Instantiate(allRotationObjects[(int)(Mathf.Abs(n) * 20) % allRotationObjects.Length], walkpoints[i].pos + transform.position+new Vector3(0,-1,0), Quaternion.Euler(180, (fn * 100000f) % 100, 0), transform);
                }
                if (walkpoints[i].angle == 15 && (n < -0.4f || n > 0.4f) && (int)(Mathf.Cos(-n) * 200) % 9 == 0)
                {
                    Instantiate(allRotationObjects[(int)(Mathf.Abs(n) * 20) % allRotationObjects.Length], walkpoints[i].pos + transform.position + new Vector3(0, 1, 0), Quaternion.Euler(0, (fn * 100000f) % 100, 0), transform);
                }
                if (QualitySettings.GetQualityLevel() > 2) if (walkpoints[i].angle == 240 && n > -0.3f&&n<0 && (int)(Mathf.Sin(-n) * 200) % 7 ==0)
                {
                    decorations.Add(walkpoints[i].pos, Instantiate(flowers[(int)(-n * 20) % flowers.Length], walkpoints[i].pos + transform.position, Quaternion.Euler(0, (fn*100000f)%100, 0), transform));
                }
                //else print(walkpoints[i].Yangle);
            }
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

        triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 9+sizeof(int), ComputeBufferType.Append);
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        walkCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        walkpointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 6 + sizeof(int), ComputeBufferType.Append);
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
        trisconnections = new int[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
                trisconnections[i * 3 + j] = tris[i].pointparent;
            }
        }
        mesh.vertices = vertices;
        //mesh.color TODO !!!!!!!
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

        //свет
        //UpdateLight();

        //навигация
        UpdateNav();
    }
    private const int RAYSNUMBER=128;
    private void UpdateLight() 
    {
        UpdateLight(FindObjectsOfType<Light>());
    }
    public void UpdateLight(Light[] lights) 
    {
        if (trisconnections.Length == 0) { return; }
        Color[] trislight = new Color[trisconnections.Length];

        LightWave[] allwaves;
        List<LightWave> allwayslist = new List<LightWave>();
        for (int i = 0; i < lights.Length; ++i)
        {
            if (lights[i].type == LightType.Point&&Vector3.Distance(center,lights[i].transform.position)<lights[i].range+5)
            {
                LightWave[] lightWaves = new LightWave[RAYSNUMBER];
                for (int l = 0; l < RAYSNUMBER; ++l) 
                {
                    lightWaves[l].pos = lights[i].transform.position-transform.position;
                    lightWaves[l].moveVector = FibSphere(l,RAYSNUMBER,1f);
                    lightWaves[l].color = lights[i].color;
                    lightWaves[l].time = lights[i].intensity;
                    lightWaves[l].maxtime = lights[i].intensity;
                }
                allwayslist.AddRange(lightWaves);
            }
        }
        allwaves = allwayslist.ToArray();

        if (allwaves.Length > 0)
        {
            int numPoints = sizeXYZ * sizeXYZ * sizeXYZ;

            pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
            pointsBuffer.SetData(space);
            ComputeBuffer trisconnectionsbuffer = new ComputeBuffer(trisconnections.Length, sizeof(int));
            trisconnectionsbuffer.SetData(trisconnections);
            ComputeBuffer trislightbuffer = new ComputeBuffer(trislight.Length, sizeof(float) * 4);
            trislightbuffer.SetData(trislight);
            ComputeBuffer wavesbuffer = new ComputeBuffer(allwaves.Length, sizeof(float) * 12);
            wavesbuffer.SetData(allwaves);

            int numThreadsPerAxis = Mathf.CeilToInt(allwaves.Length / (float)8);
            //int numVoxelsPerAxis = sizeXYZ - 1;
            int numThreadsPerAxisY = Mathf.CeilToInt(space.Length / (float)8);
            int numThreadsPerAxisZ = Mathf.CeilToInt(Mathf.Sqrt(trisconnections.Length) / (float)8);
            int _kernelindex = EnterpriseShader.FindKernel("Light");

            EnterpriseShader.SetBuffer(_kernelindex,"points",pointsBuffer);
            EnterpriseShader.SetBuffer(_kernelindex,"connections",trisconnectionsbuffer);
            EnterpriseShader.SetBuffer(_kernelindex,"light",trislightbuffer);
            EnterpriseShader.SetBuffer(_kernelindex,"waves",wavesbuffer);
            EnterpriseShader.SetInt("iteration",0);
            EnterpriseShader.SetFloat("sqrtlen",Mathf.Sqrt(trisconnections.Length));

            for (int i = 0; i < 10; ++i) 
            {
                EnterpriseShader.SetInt("iteration",i);
                EnterpriseShader.Dispatch(_kernelindex, numThreadsPerAxisY, numThreadsPerAxisZ, numThreadsPerAxisZ);
            }

            trislightbuffer.GetData(trislight, 0, 0, trislight.Length);

            pointsBuffer.Release();
            trisconnectionsbuffer.Release();
            trislightbuffer.Release();
            wavesbuffer.Release();
            pointsBuffer.Dispose();
            trisconnectionsbuffer.Dispose();
            trislightbuffer.Dispose();
            wavesbuffer.Dispose();
            GetComponent<MeshFilter>().mesh.colors = trislight;
        }
    }
    private Vector3 FibSphere(int i, int n, float radius)
    {
        var k = i + .5f;

        var phi = Mathf.Acos(1f - 2f * k / n);
        var theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * k;

        var x = Mathf.Cos(theta) * Mathf.Sin(phi);
        var y = Mathf.Sin(theta) * Mathf.Sin(phi);
        var z = Mathf.Cos(phi);

        return new Vector3(x, y, z) * radius;
    }

    public void UpdateNav() 
    {
        if (walkpoints.Length == 0) { return; }
       // print("соединение");
        walkpointneighbors = new Generator.walkpointneighbors[walkpoints.Length];

        walkpointsBuffer = new ComputeBuffer(walkpoints.Length, sizeof(float) * 6 + sizeof(int));
        walkpointsdatas = new ComputeBuffer(walkpoints.Length, sizeof(int)*10);
        walkpointsBuffer.SetData(walkpoints);
        walkpointsdatas.SetData(walkpointneighbors);

        int numThreadsPerAxis = Mathf.CeilToInt(walkpoints.Length / (float)8);
        int _kernelindex = frenderShader.FindKernel("Burn");

        frenderShader.SetBuffer(_kernelindex,"points",walkpointsBuffer);
        frenderShader.SetBuffer(_kernelindex,"pointsdatas",walkpointsdatas);

        frenderShader.Dispatch(_kernelindex, numThreadsPerAxis, 1, 1);

        walkpointsdatas.GetData(walkpointneighbors);

        walkpointsBuffer.Release();
        walkpointsBuffer.Dispose();
        walkpointsdatas.Release();
        walkpointsdatas.Dispose();
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
    public void SetNavigation(Vector3 startpoint,Vector3 endpoint)
    {
        int minid = -1;
        int maxid = -1;
        float min = 9999f;
        float max=0;
        float d;
        for(int i = 0; i < walkpoints.Length; ++i) 
        {
            walkpoints[i].weight = 0;
            d = Vector3.Distance(walkpoints[i].pos+transform.position, startpoint);
            if (d < min) 
            {
                minid = i;
                min = d;
            }
            d = Vector3.Distance(walkpoints[i].pos+transform.position, endpoint);
            if (d > max) 
            {
                maxid = i;
                max = d;
            }
        }
        
        if (minid == -1||maxid==-1) { throw new System.Exception("ЧтоВообщеПроисходитException"); }

        walkpoints[minid].weight = 1;

        List<int> buffer = new List<int>();
        List<int> secondbuffer = new List<int>();

        //for (int i = 0; i < walkpointneighbors[minid].Length; ++i) { buffer.Add(walkpointneighbors[minid][i]); }
        buffer.Add(minid);
        int k = 0;
        while (k < 100) 
        {
            ++k;
            secondbuffer = new List<int>();
            for (int i = 0; i < buffer.Count; ++i) 
            {
                //if (maxid == buffer[i]) { k=101;break; }
                for (int ii = 0; ii < walkpointneighbors[buffer[i]].Length; ++ii) if(walkpoints[walkpointneighbors[buffer[i]][ii]].weight==0)
                {
                    secondbuffer.Add(walkpointneighbors[buffer[i]][ii]);
                    walkpoints[walkpointneighbors[buffer[i]][ii]].weight = walkpoints[buffer[i]].weight + 1;
                }
            }
            buffer = new List<int>();
            buffer.AddRange(secondbuffer);
        }

    }
    public void SetNavigationOld(Vector3 startpoint)
    {
        int _kernelindex = navShader.FindKernel("Clear");
        ComputeBuffer navbuffer = new ComputeBuffer(walkpoints.Length, sizeof(float) * 6 + sizeof(int));
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
     //   print("есть обработка!");
        for (int i = 0; i < 10; ++i) { print(walkpoints[Random.Range(0, walkpoints.Length)].iter); }
        walkpoints = new Walkpoint[walkpoints.Length];
        navbuffer.GetData(walkpoints, 0, 0, walkpoints.Length);
        navbuffer.Release();
    }
    Generator.walkpointneighbors[] walkpointneighbors;
    public List<Vector3> GetPath(Vector3 endpoint)
    {
        List<Vector3> outlist = new List<Vector3>();
        int id=0;
        for(int i=0;i<walkpoints.Length;++i)if (Vector3.Distance(walkpoints[i].pos + transform.position, endpoint) < 1)
        {
            id = i;
           //     print("присвоено " + walkpoints[i].weight);
            break;
        }
        List<int> pathchain = GetPathChain(id);
     //   print("длина цепи:"+pathchain.Count);
        for (int i = 0; i < pathchain.Count; ++i) { outlist.Add(walkpoints[pathchain[i]].pos+transform.position); }
        outlist.Reverse();
        return outlist;
    }
    private List<int> GetPathChain(int id) 
    {
        List<int> outchain = new List<int>();
        if (walkpoints[id].weight == 1) { outchain.Add(id);return outchain; }
        for (int i = 0; i < walkpointneighbors[id].Length; ++i) 
        {
            if (walkpoints[walkpointneighbors[id][i]].weight < walkpoints[id].weight) 
            {
                outchain = GetPathChain(walkpointneighbors[id][i]);
                if(id%3==0)outchain.Add(id);
                return outchain;
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
                Gizmos.color = new Color(walkpoints[i].weight*0.01f,0,1- walkpoints[i].weight*0.01f);
                Gizmos.DrawCube(new Vector3((walkpoints[i].x  + transform.position.x), (walkpoints[i].y  + transform.position.y), (walkpoints[i].z  + transform.position.z)), one);
               for (int ii = 0; ii < walkpointneighbors[i].Length; ++ii) if(walkpointneighbors[i][ii]!=-1)
                {
                    Debug.DrawLine(walkpoints[i].pos+transform.position,walkpoints[walkpointneighbors[i][ii]].pos + transform.position, Gizmos.color);
                }
            }
        }
        if (Application.isEditor) if (updateconnectionslocal!=updateconnections) {
                updateconnectionslocal = updateconnections;
                filter = GetComponent<MeshFilter>();
                collider = GetComponent<MeshCollider>();
            }
        if (Application.isEditor)
        {
            
          //  bool isSelected = Selection.Contains(gameObject);
          //  Gizmos.color = isSelected ? new Color(0.168f, 0.5814968f, 0.93741f, 0.24f) : new Color(0.465f, 0.21978f, 0.1678f, 0.24f);
            /*if(cX)Debug.DrawRay(center,new Vector3(-10,0,0),Color.blue);
            if(cY)Debug.DrawRay(center,new Vector3(0,-10,0),Color.blue);
            if(cZ)Debug.DrawRay(center,new Vector3(0,0,-10),Color.blue);*/
            //Gizmos.DrawCube(transform.position + new Vector3(4.875f, 4.875f, 4.875f), new Vector3(9.75f, 9.75f, 9.75f));
        }
    }
   /* private void OnGUI()
    {
        if(isDebug)GUI.Box(new Rect(Screen.width - 200, Screen.height - 20, 200, 20), ""+walkpoints.Length);
    }*/
    struct Triangle
    {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public int pointparent;
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
        public float angle;
        public float Yangle;
        public float x { get { return pos.x; } }
        public float y { get { return pos.y; } }
        public float z { get { return pos.z; } }
    }
    public struct LightWave 
    {
        #pragma warning disable 649 // disable unassigned variable warning
        public Vector3 pos;
        public Vector3 moveVector;
        public Color color;
        public float time;
        public float maxtime;
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
