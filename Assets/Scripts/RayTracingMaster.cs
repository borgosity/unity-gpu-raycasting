﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    [SerializeField]
    private ComputeShader rayTracingShader;
    [SerializeField]
    private Vector3 numThreads = new Vector3(8, 8, 1);
    [SerializeField]
    private Texture skyboxTexture;
    [SerializeField]
    private SpheresList spheresList;

    private RenderTexture target;
    private Camera renderCamera;
    private uint currentSample = 0;
    private Material addMaterial;

    private ComputeBuffer sphereBuffer;

    private void Awake()
    {
        renderCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        SetupScene();
    }

    private void SetupScene()
    {
        if (sphereBuffer != null)
            sphereBuffer.Release();

        sphereBuffer = new ComputeBuffer(spheresList.SphereCount, 40);
        sphereBuffer.SetData(spheresList.Spheres);
    }

    private void OnDisable()
    {
        if (sphereBuffer != null)
            sphereBuffer.Release();
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            currentSample = 0;
            transform.hasChanged = false;
        }
    }

    private void SetShaderParameters()
    {
        rayTracingShader.SetMatrix("_CameraToWorld", renderCamera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", renderCamera.projectionMatrix.inverse);
        rayTracingShader.SetTexture(0, "_SkyboxTexture", skyboxTexture);
        rayTracingShader.SetVector("_PixelOffset", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination)
    {
        InitRenderTexture();

        // set target and dispatch compute shader
        rayTracingShader.SetTexture(0, "Result", target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / numThreads.x);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / numThreads.y);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, (int)numThreads.z);

        if (addMaterial == null)
        {
            addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        }
        addMaterial.SetFloat("_Sample", currentSample);
        Graphics.Blit(target, destination, addMaterial);
        currentSample++;
    }

    private void InitRenderTexture()
    {
        if(target == null || target.width != Screen.width || target.height != Screen.height)
        {
            // release existing target
            if(target != null)
            {
                target.Release();
            }

            // create target
            target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }
    }

}
