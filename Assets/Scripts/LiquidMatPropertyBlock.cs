using UnityEngine;

public class LiquidMatPropertyBlock : MonoBehaviour
{
    private MaterialPropertyBlock Block;
    private Renderer _renderer;

    [Range(0, 1)] public float FillAmount = 0f;

    [Header("Do not tweak these values, this is clean code I promise")]
    public float FillMax = 0f;
    public float FillMin = 0f;


    void Start()
    {
        _renderer = GetComponent<Renderer>();
        Block = new MaterialPropertyBlock();

        _renderer.GetPropertyBlock(Block);
        Block.SetFloat("_CupMax", FillMax);
        Block.SetFloat("_CupMin", FillMin);
        _renderer.SetPropertyBlock(Block);
    }

    void Update()
    {
        _renderer.GetPropertyBlock(Block);
        Block.SetFloat("_FillAmount", FillAmount); // Assign unique value
        _renderer.SetPropertyBlock(Block);
    }
}