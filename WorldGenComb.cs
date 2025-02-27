using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenComb : MonoBehaviour
{
    Mesh mesh;
    public GameObject World;
    public static WorldGenComb Instance;

    private struct VertexInfo{
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;
    }

    [SerializeField] ComputeShader computeShader;
    private void Awake() {
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void UpdateWorldObj(Vector3 at){
        World.transform.position = at;
        World.GetComponent<MeshFilter>().mesh = mesh;
        World.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    private IEnumerator GetAndAssignData(GraphicsBuffer genVerts, GraphicsBuffer genTris){

        VertexInfo[] verts = new VertexInfo[genVerts.count];
        int[] tris = new int[genTris.count];
        genVerts.GetData(verts);
        genTris.GetData(tris);

        Vector3[] Verts = new Vector3[verts.Length];
        Vector3[] Normals = new Vector3[verts.Length];
        Vector2[] UV = new Vector2[verts.Length];
        GuiGizmoDisplayer.Instance.spheres = new List<Vector3>();
        for(int i = 0; i <verts.Length; i++){
            Verts[i] = verts[i].Position;
            // Debug.Log(verts[i].Position.y);
            Normals[i] = verts[i].Normal;
            UV[i] = verts[i].Uv;

            if(i%1000==0)
                yield return null;
        }
        
        mesh.vertices = Verts;
        mesh.normals = Normals;
        mesh.uv = UV;
        mesh.triangles = tris;

        genVerts.Release();
        genTris.Release();

        UpdateWorldObj(new Vector3(0,0,0));
        
        WorldGenRunner.Instance.Finish();
    }

    void GetAndAssignDataAbsolute(GraphicsBuffer verts, GraphicsBuffer norms, GraphicsBuffer uvs, GraphicsBuffer tris){
        Vector3[] Verts = new Vector3[verts.count];
        Vector3[] Normals = new Vector3[norms.count];
        Vector2[] UVs = new Vector2[uvs.count];
        int[] Triangles = new int[tris.count];

        verts.GetData(Verts);
        norms.GetData(Normals);
        uvs.GetData(UVs);
        tris.GetData(Triangles);


        // foreach(Vector2 uv in UVs){
        //     Debug.Log(uv);
        // }
        mesh.vertices = Verts;
        mesh.normals = Normals;
        mesh.SetUVs(0,UVs);
        mesh.triangles = Triangles;

        // verts.Release();
        FieldManagment.Instance.worldVerts = verts;
        norms.Release();
        uvs.Release();
        tris.Release();

        UpdateWorldObj(new Vector3(0,0,0));

        WorldGenRunner.Instance.Finish();
    }

    public Texture2D GenerateWorld(ref WorldGenSettings settings){
        Texture2D toReturn = new Texture2D(settings.Width,settings.Width, TextureFormat.RGBA32, 1, false);
        RenderTexture noise = new RenderTexture(settings.Width,settings.Width,0);

        // Debug.Log(toReturn.isReadable);
        // Debug.Log(noise.isReadable);
        // toReturn.isReadable=true;
        // noise.isReadable=true;

        noise.enableRandomWrite = true;

        int Seed = (int)settings.Seed;

        if(Seed==0){
            Seed=Random.Range(0,2000000000);
            // settings.Seed = Seed;
        }
        settings.PopulatedSeed = (uint)Seed;

        System.Random SeededRng = new System.Random (Seed);

        Vector2[] Offsets = new Vector2[settings.Octaves];

        for (int i = 0; i < settings.Octaves; i++){
            Offsets[i] = new Vector2(SeededRng.Next(-100000,100000),SeededRng.Next(-100000,100000));
        }
        ComputeBuffer offsetBuffer = new ComputeBuffer(settings.Octaves,8);

        offsetBuffer.SetData(Offsets);

        computeShader.SetBuffer(0, "_Offsets", offsetBuffer);
        computeShader.SetTexture(0,"Result", noise);
        computeShader.SetInt("_Width", settings.Width);
        computeShader.SetInt("_Octaves",settings.Octaves);
        computeShader.SetFloat("_Lacunarity", settings.Lacunarity);
        computeShader.SetFloat("_Persistance", settings.Persistance);
        computeShader.SetFloat("_IslandSize", settings.IslandSize);
        computeShader.SetFloat("_Zoom", settings.PerlinZoom);

         mesh = new Mesh();


        GraphicsBuffer genPos = new GraphicsBuffer(GraphicsBuffer.Target.Structured, settings.Width*settings.Width, 12); 
        GraphicsBuffer genNorm = new GraphicsBuffer(GraphicsBuffer.Target.Structured, settings.Width*settings.Width, 12); 
        GraphicsBuffer genUv = new GraphicsBuffer(GraphicsBuffer.Target.Structured, settings.Width*settings.Width, 8); 
        GraphicsBuffer genTris = new GraphicsBuffer(GraphicsBuffer.Target.Structured, ((settings.Width-1)*(settings.Width-1)*2)*3, sizeof(int));

        // computeShader.SetBuffer(1, "_VertInfo", genVerts);
        computeShader.SetBuffer(1,"_vPos", genPos);
        computeShader.SetBuffer(1,"_vNorm", genNorm);
        computeShader.SetBuffer(1,"_vUv", genUv);
        computeShader.SetBuffer(1, "_Triangles", genTris);

        computeShader.SetTexture(1,"Result", noise);
        computeShader.SetInt( "_PointWidth", settings.Width);
        computeShader.SetFloat( "_WorldSize", settings.WorldSize);

        computeShader.GetKernelThreadGroupSizes(1, out uint x, out uint y, out _);
        int dispatchSizeX = Mathf.CeilToInt((float)settings.Width/x);
        int dispatchSizeY = Mathf.CeilToInt((float)settings.Width/y);

        


        computeShader.GetKernelThreadGroupSizes(0, out x, out y, out _);
        computeShader.Dispatch(0,Mathf.CeilToInt((float)settings.Width/x),Mathf.CeilToInt((float)settings.Width/y),1);

        Debug.Log("Dispatching x: "+dispatchSizeX+" y:"+dispatchSizeY);
        computeShader.Dispatch(1, dispatchSizeX, dispatchSizeY, 1);

        // StartCoroutine(GetAndAssignDataAsync(genVerts, genTris)); //FUCK THIS

        // StartCoroutine(GetAndAssignData(genVerts, genTris));

        GetAndAssignDataAbsolute(genPos, genNorm, genUv, genTris);

        Graphics.CopyTexture(noise,toReturn);

        RockPlacer.Instance.heightmap = noise;

        // RenderTexture.active = noise;

        // toReturn.ReadPixels(new Rect(0,0,settings.Width,settings.Width), 0, 0);

        // RenderTexture.active = null;

        return toReturn;
    }
}
