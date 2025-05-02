using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Ingridence;
using Unity.VisualScripting;
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

    [Header("Shacker Lid Sounds")]
    [SerializeField] AudioSource shaker_audioSource;
    [SerializeField] AudioClip lidCloseSound;
    [SerializeField] AudioClip lidOpenSound;
    [SerializeField] AudioClip capCloseSound;
    [SerializeField] AudioClip capOpenSound;
    [SerializeField] float shackerLidsVolume = 1f;
    [SerializeField] float shackerLidsMaxPitch = 1.2f;
    [SerializeField] float shackerLidsMinPitch = 0.8f;


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
            shaker_audioSource.pitch = Random.Range(shackerLidsMinPitch, shackerLidsMaxPitch);
            shaker_audioSource.PlayOneShot(capCloseSound, shackerLidsVolume);
            Debug.Log("Strainer detached.");
        }
    }

    private void OnLowerSocketSelectExited(SelectExitEventArgs args)
    {
        set_lower_cap(false);
        set_strainer(false);
        set_lower_cap(false);
        shaker_audioSource.pitch = Random.Range(shackerLidsMinPitch, shackerLidsMaxPitch);
        shaker_audioSource.PlayOneShot(capOpenSound, shackerLidsVolume);
    }

    private void OnUpperSocketSelectEntered(SelectEnterEventArgs args)
    {
        set_upper_cap(true);
        shaker_audioSource.pitch = Random.Range(shackerLidsMinPitch, shackerLidsMaxPitch);
        shaker_audioSource.PlayOneShot(lidCloseSound, shackerLidsVolume);
    }

    private void OnUpperSocketSelectExited(SelectExitEventArgs args)
    {
        set_upper_cap(false);
        shaker_audioSource.pitch = Random.Range(shackerLidsMinPitch, shackerLidsMaxPitch);
        shaker_audioSource.PlayOneShot(lidOpenSound, shackerLidsVolume);
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
        // Call base.getIngredientBase() only once and store the result
        IngredientBase ingredientBase = base.getIngredientBase();

        // If no ingredient base is returned, exit early
        if (ingredientBase == null)
            return null;


        // If the strainer is active, modify the action to "Strained"
        if (strainer)
        {
            ingredientBase.step.action = DrinkAction.Strained;
        }

        return ingredientBase;
    }

    override internal bool isPouring()
    {
        bool isPouring = Vector3.Dot(transform.up, Vector3.down) > Mathf.Cos(pourThreshold * Mathf.Deg2Rad);
        bool haveEnoughtLiqquid = false;
        if (liquidContainer != null)
            haveEnoughtLiqquid = liquidContainer.canPoourer();
        return isPouring && haveEnoughtLiqquid && (strainer || !(lower_cap && upper_cap));
    }

    public override void clearContainer()
    {
      /*  liquidContainer.ingredients.Clear();
        LiquidContainerLimited lc = liquidContainer as LiquidContainerLimited;
        lc.clearIce();*/
    }
}
