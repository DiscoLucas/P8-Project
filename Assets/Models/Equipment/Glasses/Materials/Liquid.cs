using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Drink_interaction;
using UnityEngine;

[ExecuteInEditMode]
public class Liquid : MonoBehaviour
{
    public enum UpdateMode { Normal, UnscaledTime }
    public UpdateMode updateMode;

    [SerializeField] float MaxWobble = 0.03f;
    [SerializeField] float WobbleSpeedMove = 1f;
    [SerializeField] public Color DrinkColor;
    [SerializeField] public LiquidContainerLimited liquidProperty;
    public float fillAmount = 0.5f;
    public float fillAmountLerpMax = 1;
    public float fillAmountLerpMin = 0;
    float fillAmountScaled;
    [SerializeField] float Recovery = 1f;
    [SerializeField] float Thickness = 1f;
    [Range(0, 1)] public float CompensateShapeAmount;
    [SerializeField] Mesh mesh;
    [SerializeField] Renderer rend;
    Vector3 pos;
    Vector3 lastPos;
    Vector3 velocity;
    Quaternion lastRot;
    Vector3 angularVelocity;
    float wobbleAmountX;
    float wobbleAmountZ;
    float wobbleAmountToAddX;
    float wobbleAmountToAddZ;
    float pulse;
    float sinewave;
    float time = 0.5f;
    Vector3 comp;
    MaterialPropertyBlock propBlock;
    
    float cachedLowestY = float.MaxValue;
    bool needsRecalculateLowestY = true;

    void Start()
    {
        GetMeshAndRend();
        liquidProperty = GetComponentInParent<LiquidContainerLimited>();
        propBlock = new MaterialPropertyBlock();
    }

    private void OnValidate()
    {
        GetMeshAndRend();
    }

    void GetMeshAndRend()
    {
        if (mesh == null)
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
                mesh = meshFilter.sharedMesh;
        }
        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }
    }

    void Update()
    {
        fillAmountScaled = Mathf.Lerp(fillAmountLerpMin, fillAmountLerpMax, fillAmount);
        if (liquidProperty != null)
        {
            DrinkColor = liquidProperty.getLiquidColor();
            fillAmount = liquidProperty.FillPercentage();
        }

        float deltaTime = updateMode == UpdateMode.Normal ? Time.deltaTime : Time.unscaledDeltaTime;
        time += deltaTime;

        if (deltaTime > 0)
        {
            velocity = (lastPos - transform.position) / deltaTime;
            angularVelocity = GetAngularVelocity(lastRot, transform.rotation);
        }
        else
        {
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
        }

        rend.GetPropertyBlock(propBlock);
        propBlock.SetFloat("_WobbleX", wobbleAmountX);
        propBlock.SetFloat("_WobbleZ", wobbleAmountZ);
        UpdatePos(deltaTime);
        UpdateMat(propBlock);
        rend.SetPropertyBlock(propBlock);
        
        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    void UpdatePos(float deltaTime)
    {
        Vector3 worldPos = transform.TransformPoint(mesh.bounds.center);
        if (CompensateShapeAmount > 0)
        {
            if (deltaTime > 0)
            {
                comp = Vector3.Lerp(comp, (worldPos - new Vector3(0, GetLowestPoint(), 0)), deltaTime * 10);
            }
            else
            {
                comp = (worldPos - new Vector3(0, GetLowestPoint(), 0));
            }
            pos = worldPos - transform.position - new Vector3(0, 1f - fillAmountScaled - (comp.y * CompensateShapeAmount), 0);
        }
        else
        {
            pos = worldPos - transform.position - new Vector3(0, 1f - fillAmountScaled, 0);
        }
    }

    void UpdateMat(MaterialPropertyBlock propBlock)
    {
        propBlock.SetVector("_FillAmount", pos);
        propBlock.SetColor("_BottomColor", DrinkColor);
        propBlock.SetColor("_TopColor", DrinkColor);
    }

    float GetLowestPoint()
    {
        if (!needsRecalculateLowestY)
            return cachedLowestY;

        float lowestY = float.MaxValue;
        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            float worldY = transform.TransformPoint(vertices[i]).y;
            if (worldY < lowestY)
                lowestY = worldY;
        }

        cachedLowestY = lowestY;
        needsRecalculateLowestY = false;
        return cachedLowestY;
    }

    Vector3 GetAngularVelocity(Quaternion foreLastFrameRotation, Quaternion lastFrameRotation)
    {
        var q = lastFrameRotation * Quaternion.Inverse(foreLastFrameRotation);
        if (Mathf.Abs(q.w) > 1023.5f / 1024.0f)
            return Vector3.zero;

        float angle = Mathf.Acos(Mathf.Clamp(q.w, -1f, 1f));
        float sinAngle = Mathf.Sin(angle);
        if (Mathf.Approximately(sinAngle, 0))
            return Vector3.zero;

        float gain = 2.0f * angle / (sinAngle * Time.deltaTime);
        return new Vector3(q.x * gain, q.y * gain, q.z * gain);
    }
}