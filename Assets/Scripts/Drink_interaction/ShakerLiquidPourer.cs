using UnityEngine;

public class ShakerLiquidPourer : LiquidPourer
{
    [Header("Shaker parts")]
    [SerializeField]
    bool lower_cap = false;
    [SerializeField]
    bool upper_cap = false;

    public void set_lower_cap(bool value)
    {
        lower_cap = value;
    }

    public void set_upper_cap(bool value)
    {
        upper_cap = value;
    }

    public bool canShake()
    {
        return lower_cap && upper_cap;
    }

    override internal bool isPouring()
    {
        bool isPouring = Vector3.Dot(transform.up, Vector3.down) > Mathf.Cos(pourThreshold * Mathf.Deg2Rad);
        bool haveEnoughtLiqquid = false;
        if (liquidContainer != null)
            haveEnoughtLiqquid = liquidContainer.canPoourer();
        return isPouring && haveEnoughtLiqquid && !lower_cap && !upper_cap;
    }
}
