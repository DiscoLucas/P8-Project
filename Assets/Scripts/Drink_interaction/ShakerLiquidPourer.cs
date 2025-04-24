using Assets.Scripts.Ingridence;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ShakerLiquidPourer : LiquidPourer
{
    [SerializeField]
    XRSocketInteractor lowerSocket;
    [SerializeField]
    XRSocketInteractor upperSocket;

    [Header("Shaker parts")]
    [SerializeField]
    bool lower_cap = false;
    [SerializeField]
    bool upper_cap = false;
    [SerializeField]
    bool strainer = false;
    [SerializeField] protected Transform pourPointClosed;

    private void OnEnable()
    {
        if (lowerSocket != null)
        {
            lowerSocket.selectEntered.AddListener(OnLowerSocketSelectEntered);
            lowerSocket.selectExited.AddListener(OnLowerSocketSelectExited);
        }

        if (upperSocket != null)
        {
            upperSocket.selectEntered.AddListener(OnUpperSocketSelectEntered);
            upperSocket.selectExited.AddListener(OnUpperSocketSelectExited);
        }
    }

    private void OnDisable()
    {
        if (lowerSocket != null)
        {
            lowerSocket.selectEntered.RemoveListener(OnLowerSocketSelectEntered);
            lowerSocket.selectExited.RemoveListener(OnLowerSocketSelectExited);
        }

        if (upperSocket != null)
        {
            upperSocket.selectEntered.RemoveListener(OnUpperSocketSelectEntered);
            upperSocket.selectExited.RemoveListener(OnUpperSocketSelectExited);
        }
    }

    private void OnLowerSocketSelectEntered(SelectEnterEventArgs args)
    {
        if(args.interactableObject.transform.gameObject.tag == "Strainer")
        {
            set_strainer(true);
            GameObject interactingObject = args.interactableObject.transform.gameObject;
            Debug.Log("Attached object's tag is: " + interactingObject.name + " and name is: " + args.interactorObject.transform.name);

            if (interactingObject.tag == "Strainer")
            {
                Debug.Log("Strainer attached.");
            }
        }
        else
        {
            set_lower_cap(true);
            Debug.Log("Strainer detached.");
        }
    }

    private void OnLowerSocketSelectExited(SelectExitEventArgs args)
    {
        set_lower_cap(false);
        set_strainer(false);
        set_lower_cap(false);
    }

    private void OnUpperSocketSelectEntered(SelectEnterEventArgs args)
    {
        set_upper_cap(true);
    }

    private void OnUpperSocketSelectExited(SelectExitEventArgs args)
    {
        set_upper_cap(false);
    }

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
        if (!lower_cap)
            return pourPoint;
        else
            return pourPointClosed;
    }

    internal override IngredientBase getIngredientBase()
    {
        if (strainer)
        {
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
        return isPouring && haveEnoughtLiqquid && !(upper_cap && !strainer);
    }
}
