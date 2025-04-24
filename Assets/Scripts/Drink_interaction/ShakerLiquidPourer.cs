using Assets.Scripts.Ingridence;
using UnityEngine;

public class ShakerLiquidPourer : LiquidPourer
{
    [Header("Shaker parts")]
    [SerializeField]
    bool lower_cap = false;
    [SerializeField]
    bool upper_cap = false;
    [SerializeField]
    bool strainer = false;
    [SerializeField] protected Transform pourPointClosed;
    public void set_lower_cap(bool value)
    {
        lower_cap = value;
    }

    public void set_upper_cap(bool value)
    {
        upper_cap = value;
    }

        public void set_strainer(bool value)
    {
        strainer = value;
    }

    public bool canShake()
    {
        return lower_cap && upper_cap;
    }

    internal override Transform getPourPoint()
    {
        if(!lower_cap)
            return pourPoint;
        else
            return pourPointClosed;
    }

    internal override IngredientBase getIngredientBase()
    {
        if (strainer){

            IngredientBase ingredientBase = base.getIngredientBase();
            ingredientBase.step.action = DrinkAction.Strained;
            return ingredientBase;
        }
        return base.getIngredientBase();
    }

    override internal bool isPouring()
    {
        bool isPouring = Vector3.Dot(transform.up, Vector3.down) > Mathf.Cos(pourThreshold * Mathf.Deg2Rad);
        bool haveEnoughtLiqquid = false;
        if (liquidContainer != null)
            haveEnoughtLiqquid = liquidContainer.canPoourer();
        return isPouring && haveEnoughtLiqquid && !upper_cap;
    }
}
