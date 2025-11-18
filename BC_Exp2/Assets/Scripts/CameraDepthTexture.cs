using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.Rendering;
using UnityEngine.XR;
using static UnityEngine.Camera;

public class CameraDepthTexture : MonoBehaviour
{

    
    public float object_base_scale = 0.03f;
    private float dots_per_degree;

    public float dotFieldWidthDegs = 40f; 
    public float dotFieldHeightDegs = 40f;
    //public float dot_increment_degs = 0.25f;

    public int object_x, object_y = 96;
    public int object_increment = 32;

    private bool is_active = false;

    public Shader depthShader;
    private Material depthMaterial;
    public RenderTexture depthTexture;
    public RenderTexture computeResult;
    public GameObject Indirect_Marker;
    public GameObject direct_Marker;

    public Camera _camera;

    public ComputeShader depthComputShader;

    

    private int object_count;
    
    private int kernelHandle, thread_x, thread_y;

    private List<List<GameObject>> objects;

    private ComputeBuffer objectPosBuffer;

    public Vector4[] debugObject;


    public float DotLifeTime;
    private float[] lifeTimeUpdateValues;

    private ComputeBuffer jitterBuffer;
    private ComputeBuffer jitter_uv_update_buffer;
    private Vector2[] jitter_uv;
    private Vector2[] jitter_bin_range_uv;
    
    private bool hasUpdated;
    private bool updateIsOn;
    private bool renderpause = false;

    private float dotFieldWidthRads, dotFieldHeightRads;

    Thread update_thread;

    private ComputeBuffer randomLifeTimeBuffer;

    // Indirect Draw
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    private ComputeBuffer SizeBuffer;
    const int commandCount = 1;

    public Material render_mat;

    //public TextMeshPro Text_Res;
    //public TextMeshPro Text_Temp;
    

    // Method Switch
    private bool useNaive = false;

    private void generateUpdateBuffer()
    {
        System.Random rand = new System.Random();
        while (updateIsOn)
        {
            if(renderpause == true)
            {
                continue;
            }

            //int object_count = object_x * object_y;

            Vector2[] local_jitter_uv = new Vector2[object_count];
            
            System.Random xx = new System.Random();
            System.Random yy = new System.Random();

            for (int i = 0; i < (object_count); i++)
            {
                float jx = jitter_bin_range_uv[i][0]/2f;
                float jy = jitter_bin_range_uv[i][1]/2f;

                local_jitter_uv[i] = new Vector2(
                    (float)(xx.NextDouble()) * 2f*jx - jx,
                    (float)(yy.NextDouble()) * 2f*jy - jy
                    );
            }

            float[] locallifetimes = new float[object_count];
            for (int i = 0; i < (object_count); i++)
            {
                locallifetimes[i] = (float)rand.NextDouble() * DotLifeTime;
            }

            lock (this)
            {
                jitter_uv = local_jitter_uv;
                lifeTimeUpdateValues = locallifetimes;
                hasUpdated = true;
            }
            // Debug.Log("Updated random buffer");
            Thread.Sleep((int)(DotLifeTime * 1000));
        }
        
    }


    public void initializeBuffers()
    {


        if (depthShader == null)
        {
            Debug.LogError("Depth shader not set!");
            is_active = false;
            return;
        }

        // Set Camera properties and fov
        Camera parentCamera = GetComponentInParent<Camera>();
        _camera.fieldOfView = parentCamera.fieldOfView;
        float fovy_rads = _camera.fieldOfView * Mathf.Deg2Rad; 
        float fovx_rads = 2f * Mathf.Atan(Mathf.Tan(fovy_rads/ 2f) * _camera.aspect);
        depthComputShader.SetFloat("fovx_rads", fovx_rads);
        depthComputShader.SetFloat("fovy_rads", fovy_rads);

        dotFieldWidthRads = Mathf.Deg2Rad * dotFieldWidthDegs;
        dotFieldHeightRads = Mathf.Deg2Rad * dotFieldHeightDegs;
        depthComputShader.SetFloat("dotFieldWidthRads", dotFieldWidthRads);
        depthComputShader.SetFloat("dotFieldHeightRads", dotFieldHeightRads);

        //object_x = object_y =  (int)((dotFieldWidthDegs * dots_per_degree) / 8) * 8;
        dots_per_degree = object_x / dotFieldWidthDegs;

        Debug.Log("Dots per degree: " + dots_per_degree);

        // number of objects on x and y
        object_count = object_x * object_y;

        // Depth material
        depthMaterial = new Material(depthShader);
        depthMaterial.SetFloat("_CameraFarClipPlane", _camera.farClipPlane);
        depthMaterial.SetFloat("_CameraNearClipPlane", _camera.nearClipPlane);
        _camera.depthTextureMode = DepthTextureMode.Depth;

        // Compute Shaders
        kernelHandle = depthComputShader.FindKernel("CSMain");
        depthComputShader.SetTexture(kernelHandle, "DepthTexture", depthTexture);
        depthComputShader.SetTexture(kernelHandle, "Result", computeResult);
        objectPosBuffer = new ComputeBuffer(object_count, 4 * sizeof(float));
        depthComputShader.SetBuffer(kernelHandle, "ObjectPositions", objectPosBuffer);

        // Get the current viewport dimensions
        int width = _camera.pixelWidth;
        int height = _camera.pixelHeight;

        // Pass values to the depth compute shader
        depthComputShader.SetMatrix("_InverseProjectionMatrix", GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false).inverse);
        depthComputShader.SetMatrix("_ProjectionMatrix", GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false));
        depthComputShader.SetFloat("nearclipplane", _camera.nearClipPlane);

        depthComputShader.SetFloat("step_x", dotFieldWidthRads / (object_x - 1));
        depthComputShader.SetFloat("step_y", dotFieldHeightRads / (object_y - 1));
        
        depthComputShader.SetInt("object_x", object_x);
        depthComputShader.SetInt("object_y", object_y);

        //Debug.Log("Depth texture width: " + depthTexture.width);
        thread_x = Mathf.CeilToInt(object_x / 8.0f);
        thread_y = Mathf.CeilToInt(object_y / 8.0f);

        // Calculate the range of jitter in UV space.  See calc_jitter_bin_range_uv for more info.
        jitter_bin_range_uv = calc_jitter_bin_range_uv();
        jitterBuffer = new ComputeBuffer(object_count, 2 * sizeof(float));
        Vector2[] jitter_uv = new Vector2[object_count];
        for (int i = 0; i < (object_count); i++)
        {
            float jx = jitter_bin_range_uv[i][0]/2f;
            float jy = jitter_bin_range_uv[i][1]/2f;

            jitter_uv[i] = new Vector2(
                UnityEngine.Random.Range(-jx, jx), 
                UnityEngine.Random.Range(-jy, jy)
                );
        }
        jitterBuffer.SetData(jitter_uv);
        depthComputShader.SetBuffer(kernelHandle, "jitter_uv", jitterBuffer);

        // Random lifetime buffer
        randomLifeTimeBuffer = new ComputeBuffer(object_count, sizeof(float));
        float[] lifetimes = new float[object_count];
        for(int i=0; i< (object_count); i++)
        {
            lifetimes[i] = UnityEngine.Random.Range(0.0f, DotLifeTime); // Set for 5.0 for now
        }
        randomLifeTimeBuffer.SetData(lifetimes);
        depthComputShader.SetBuffer(kernelHandle, "lifeTimeBuffer", randomLifeTimeBuffer);

        // random buffer update thread
        depthComputShader.SetFloat("DotLifeTime", DotLifeTime);
        hasUpdated = false;
        updateIsOn = true;
        jitter_uv_update_buffer = new ComputeBuffer(object_count, 2 * sizeof(float));
        update_thread = new Thread(generateUpdateBuffer);
        update_thread.Start();

        // "Naive" object control.  Inefficient but transparent mode for debugging
        if (useNaive == true)
        {
            Vector3[] objectPositions = new Vector3[object_count];
            // objectPosBuffer.GetData(objectPositions);
            objects = new List<List<GameObject>>();
            for (int i = 0; i < object_x; i++)
            {
                List<GameObject> object_row = new List<GameObject>();
                objects.Add(object_row);
                for (int j = 0; j < object_y; j++)
                {
                    GameObject new_Marker = Instantiate(direct_Marker, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
                    object_row.Add(new_Marker);
                }
            }
        }
        else
        {
            // Indirect Draw
            commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];

            SizeBuffer = new ComputeBuffer(object_count, sizeof(float));
            depthComputShader.SetBuffer(kernelHandle, "SizeBuffer", SizeBuffer);
            render_mat = Indirect_Marker.GetComponent<Renderer>().sharedMaterial;

        }

        is_active = true;

    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Copy original image to destination without modifications
        Graphics.Blit(source, destination);

        // If the depth material is available, generate the depth map
        if (depthMaterial)
        {
            Graphics.Blit(source, depthTexture, depthMaterial);
        }
    }

   

    private void Update()
    {

       
        
        // Handle Text
        // Text_Res.text = "Dots/degree: " + dots_per_degree;
        // Text_Temp.text = "LifeTime (ms): " + (1000 * DotLifeTime).ToString("F2");

        // Change dot lifetime
        // if (Input.GetKeyDown("a"))
        // {
        //     DotLifeTime -= 0.02f;
        //     if(DotLifeTime < 0.0f)
        //     {
        //         DotLifeTime = 0.01f;
        //         renderpause = true;
        //     }
        //     updateLifeTime();
        // }
        // if (Input.GetKeyDown("d"))
        // {
        //     DotLifeTime += 0.02f;
        //     if (DotLifeTime > 0.0f)
        //     {
        //         renderpause = false;
        //     }
        //     updateLifeTime();
        // }

        // bool refresh_buffers = false;
        // if (Input.GetKeyDown("w"))
        // {
        //     //dots_per_degree += dot_increment_degs;
        //     object_x += object_increment;
        //     object_y += object_increment;
        //     refresh_buffers = true;
        // }
        // if (Input.GetKeyDown("s"))
        // {
        //     //dots_per_degree -= dot_increment_degs;
        //     object_x -= object_increment;
        //     object_y -= object_increment;
        //     refresh_buffers = true;

        //     //if (dots_per_degree < dot_increment_degs)
        //     //{
        //     //    dots_per_degree = dot_increment_degs;
        //     //}

        // }
        
        // if(refresh_buffers){
        //     releaseBuffers();
        //     initializeBuffers();
        // }

        // if(Input.GetKeyDown(KeyCode.Minus))
        // {
        //     if(is_active){
        //         releaseBuffers();   
        //         Debug.Log("Released buffers");
        //         is_active = false;
        //     }else{
        //         initializeBuffers();
        //         Debug.Log("Initialized buffers");
        //         is_active=true;

        //     }
        // }


        if( is_active){
            update_buffers();
        }
               
    }

    private void update_buffers(){
    
        // Update time
        float dt = Time.deltaTime;
        //Debug.Log(dt);

        // Handle random update
        if (hasUpdated)
        {
            depthComputShader.SetFloat("DotLifeTime", DotLifeTime);
            jitter_uv_update_buffer.SetData(jitter_uv);
            randomLifeTimeBuffer.SetData(lifeTimeUpdateValues);
            depthComputShader.SetBuffer(kernelHandle, "jitter_uv_update_buffer", jitter_uv_update_buffer);
            hasUpdated = false;
        }
        
        // Update values in shader
        depthComputShader.SetMatrix("_CameraToWorldMatrix", _camera.cameraToWorldMatrix);
        depthComputShader.SetVector("camera_pos", new Vector4(_camera.transform.position.x, _camera.transform.position.y, _camera.transform.position.z, 0.0f));
        depthComputShader.SetFloat("base_scale", object_base_scale);
        depthComputShader.SetFloat("deltaTime", dt);
        
        // Naive Object control for debugging
        if (useNaive == true)
        {
            Vector4[] objectPositions = new Vector4[object_x * object_y];
            objectPosBuffer.GetData(objectPositions);

            debugObject = objectPositions;
            float fovy = Camera.main.fieldOfView; 
            float aspectRatio = Camera.main.aspect; 
            float fovx = 2f * Mathf.Atan(Mathf.Tan(fovy * Mathf.Deg2Rad / 2f) * aspectRatio) * Mathf.Rad2Deg;
            float horizontalStep = fovx / (object_x - 1);
            float verticalStep = fovy / (object_y - 1);
            float nearclipplane = Camera.main.nearClipPlane;
            Matrix4x4 _projectionMatrix = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false);

            for (int i = 0; i < object_x; i++)
            {
                for (int j = 0; j < object_y; j++)
                {

                    // https://discussions.unity.com/t/worldtoviewportpoint-and-viewporttoworldpoint-math/202708
                    
                    float angleX = -fovx / 2 + i * horizontalStep;
                    float angleY = -fovy / 2 + j * verticalStep;

                    Vector3 direction = new Vector3(Mathf.Tan( angleX * Mathf.Deg2Rad), Mathf.Tan( angleY * Mathf.Deg2Rad), 1f).normalized;
                    
                    Matrix4x4 P = Camera.main.projectionMatrix;  // camera matrix
                    
                    Vector4 projPos = Camera.main.projectionMatrix * direction;
                    Vector3 ndcPos = new Vector3(projPos.x / projPos.w, projPos.y / projPos.w, projPos.z / projPos.w);
                    float a = ndcPos.x;
                    Vector3 UV = new Vector3(ndcPos.x * 0.5f + 0.5f, ndcPos.y * 0.5f + 0.5f, -direction.z);

                    Vector3 pos = Camera.main.ViewportToWorldPoint( new Vector3(UV.x, UV.y, nearclipplane*1.05f));

                    objects[i][j].transform.position = pos;
                    // objects[i][j].transform.position = pos + _camera.transform.position;
                    float scale_factor = (pos - _camera.transform.position).magnitude * 0.5f;
                    objects[i][j].transform.localScale = new Vector3(object_base_scale, object_base_scale, object_base_scale) * scale_factor;
                    

                }
            }
        }
        else
        {

            // Shader control
            float fovy = _camera.fieldOfView; 
            float fovx = 2f * Mathf.Atan(Mathf.Tan(fovy * Mathf.Deg2Rad / 2f) * _camera.aspect) * Mathf.Rad2Deg;
            depthComputShader.SetFloat("fovx", fovx);
            depthComputShader.SetFloat("fovy", fovy);

            depthComputShader.Dispatch(kernelHandle, thread_x, thread_y, 1);

            RenderParams rp = new RenderParams(Indirect_Marker.GetComponent<Renderer>().sharedMaterial);
            rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds for better FOV culling  
            rp.matProps = new MaterialPropertyBlock();

            var meshFilter = Indirect_Marker.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.sharedMesh;

            commandData[0].indexCountPerInstance = mesh.GetIndexCount(0); // how many vertex -> for vertex shader
            commandData[0].instanceCount = (uint)(object_x * object_y); // how many instances
            commandBuf.SetData(commandData);

            render_mat.SetBuffer("objectBuffer", objectPosBuffer);
            render_mat.SetBuffer("SizeBuffer", SizeBuffer);
            Graphics.RenderMeshIndirect(rp, mesh, commandBuf, commandCount);
        }

        
    

    }

    public void updateLifeTime()
    {
        

        depthComputShader.SetFloat("DotLifeTime", DotLifeTime);
        randomLifeTimeBuffer.SetData(lifeTimeUpdateValues);
        render_mat.SetBuffer("lifeTimeBuffer", randomLifeTimeBuffer);
    }

    public Vector2[] calc_jitter_bin_range_uv(){
        
        int object_count = object_x * object_y;
        float fovy_rads = _camera.fieldOfView * Mathf.Deg2Rad; 
        float fovx_rads = 2f * Mathf.Atan(Mathf.Tan(fovy_rads/ 2f) * _camera.aspect);

        float step_x = dotFieldHeightRads / (object_x - 1);
        float step_y = dotFieldWidthRads / (object_y - 1);
        
        // For each point, precalculate the range of offset in UV coordinates.
        // We want the amount of jitter to be constant in degrees
        // because of cosine error, this range will vary in UV units as you move across the visual field
        // (the range of the jitter in UV space will diminish in the periphery)
        // This offset can be imagined as a box that centered on each grid point
        // ...and +/- one step_x wide, and +/- one step_y tall
        
        //Vector3[] randomValues = new Vector3[object_count];
        
        Vector2[] jitter_ranges = new Vector2[object_count];
        for (int i = 0; i < (object_x); i++)
        {
            for (int j = 0; j < (object_y); j++)
            {
                // float angleX = -fovx / 2 + i * horizontalStep;
                float left_angleX = -dotFieldWidthRads / 2f + i * step_x - step_x/2f; // half bin left of center
                float top_angleY = -dotFieldHeightRads / 2f + j * step_y - step_y/2f; // half bin top of center
                float right_angleX = -dotFieldWidthRads / 2f + i * step_x + step_x/2f; // half bin right of center
                float bottom_angleY = -dotFieldHeightRads / 2f + j * step_y + step_y/2f; // half bin below center
                 
                Vector3 leftTopDir = new Vector3(Mathf.Tan( left_angleX ), Mathf.Tan(top_angleY), 1f).normalized;
                Vector3 rightBottomDir = new Vector3(Mathf.Tan( right_angleX), Mathf.Tan( bottom_angleY), 1f).normalized;

                Matrix4x4 P = Camera.main.projectionMatrix;  // camera matrix

                // Left top
                Vector4 projPos = Camera.main.projectionMatrix * leftTopDir;
                Vector3 ndcPos = new Vector3(projPos.x / projPos.w, projPos.y / projPos.w, projPos.z / projPos.w);
                Vector2 left_top_UV = new Vector3(ndcPos.x * 0.5f + 0.5f, ndcPos.y * 0.5f + 0.5f, -leftTopDir.z);

                                // Left top
                projPos = Camera.main.projectionMatrix * rightBottomDir;
                ndcPos = new Vector3(projPos.x / projPos.w, projPos.y / projPos.w, projPos.z / projPos.w);
                Vector2 right_bottom_UV = new Vector3(ndcPos.x * 0.5f + 0.5f, ndcPos.y * 0.5f + 0.5f, -rightBottomDir.z);

                int object_index = i + j * object_x;
                jitter_ranges[object_index] = new float2( right_bottom_UV[0] - left_top_UV[0],right_bottom_UV[1]-left_top_UV[1]);

             
           }
        }

        return jitter_ranges;

    }

    public void releaseBuffers(){

        objectPosBuffer.Release();  
        jitterBuffer.Release();  
        jitter_uv_update_buffer.Release(); 
        randomLifeTimeBuffer.Release(); 
        SizeBuffer.Release(); 
        commandBuf.Release();
    }


    void OnDestroy()
    {
        enabled=false;
        releaseBuffers();
    }

     void OnDisable()
    {
        if (depthMaterial)
        {
            DestroyImmediate(depthMaterial);
        }
        updateIsOn = false;
        update_thread.Join();
    }

    
    void OnEnable()
    {
        initializeBuffers();
    }

    

}
