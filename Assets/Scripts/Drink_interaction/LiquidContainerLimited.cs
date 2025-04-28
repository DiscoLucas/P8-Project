using Assets.Scripts.Ingridence;
using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Assets.Scripts.Drink_interaction
{
    /// <summary>
    /// A liquid container that contain a finiate amount. used for glasses and alike
    /// </summary>
    public class LiquidContainerLimited : LiquidContainer
    {

        [Header("Game settings")]
        public GameSettings gameSettings;

        [Header("Liquid")]
        protected int lastCheckColorCount = 0;

        [SerializeField]
        protected Color outputColor = Color.white;
        protected int orderCounter = 0;
        public GlassType glassType;

        [Header("Garnishing")]
        public GameObject garnish = null;
        public Transform garnishPoint;
        public IngredientBase garnishIngredient = null;
        public bool hasGarnish = false;

        [Header("Ice")]
        public bool hasIce = false;
        public Transform iceFill;
        int iceCount = 0;

        [Header("Debug liquid display")]
        public DebugClassMenu debugGlassMenu;

        [Header("Haptics")]
        public HapticImpulsePlayer currentHapticPlayer;

        [Range(0,1)]
        public float intensity = 0.01f;
        public float duration = 0.04f;
        internal Coroutine hapticCoroutine = null, sessionCoroutine;
        [SerializeField] internal float minIntensity = 0.01f;
        [SerializeField] internal float maxIntensity = 0.7f;
        [SerializeField] internal float routineWait = 0.05f;

        [SerializeField] internal float fillHapticDuration = 0.1f;
        [SerializeField] internal float fillHapticIntensity = 0.5f;

        [Header("Audio")]
        //List of sound effects
        [SerializeField] List<AudioClip> glassPlaceSounds;
        [SerializeField] AudioClip glasePlace;
        [SerializeField] AudioClip glaseFill;
        [SerializeField] List<AudioClip> iceSounds;
        [SerializeField] AudioClip iceSound; 
        [SerializeField] float glaseVolume = 1f;
        [SerializeField] float maxPitch = 1.2f;
        [SerializeField] float minPitch = 0.8f;
        [SerializeField] AudioSource audioSource;

        public void setGarnish(GameObject garnish)
        {
            GarnishContainer gc = garnish.GetComponent<GarnishContainer>();
            IngredientBase ib;
            if (gc != null)
            {
                ib = gc.ingredientScribtiableObject.ingredientBase.copy();
            }else{
                ib = new IngredientBase(garnish.name, 1, IngredientType.Garnish, Color.white);
                Debug.Log("Garnish not found, using default: " + ib.Name);
            }
            ib.solid = true;
            garnishIngredient = ib;
            AddIngredient(ib, 1);

            Debug.Log("Garnish set: " + garnish.name);
            this.garnish = garnish;
            garnish.transform.SetParent(this.transform);

            //Orientation & Freezing
            garnish.transform.SetParent(garnishPoint);
            garnish.transform.localPosition = Vector3.zero;

            if(garnish.gameObject.TryGetComponent<XRGrabInteractable>(out XRGrabInteractable grab))
            {
                Destroy(grab);
            }
            if(garnish.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                Destroy(rb);
            }
            if(garnish.gameObject.TryGetComponent<Collider>(out Collider col))
            {
                Destroy(col);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if(collision.gameObject.tag == "Garnish" && !hasGarnish){
                setGarnish(collision.gameObject); 
                hasGarnish = true;
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            if (glassPlaceSounds.Count <= 0)
                return;

            int randomIndex = UnityEngine.Random.Range(0, glassPlaceSounds.Count);
            glasePlace = glassPlaceSounds[randomIndex];
            audioSource.clip = glasePlace;
            audioSource.volume = glaseVolume;
            audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            audioSource.Play();

        }
        bool pouringSession = false;
        public override void AddIngredient(IngredientBase ingredient, float inputAmount, out float actualAddedAmount)
        {
            actualAddedAmount= 0;
            if(inputAmount <= 0)
            {
                Debug.LogWarning("Input amount must be greater than 0.");
                return;
            }

            if (ingredient.solid == false)
            {
                float availableSpace = maxFill - fillAmount;
                actualAddedAmount = Mathf.Min(inputAmount, availableSpace);

                if (actualAddedAmount <= 0)
                {
                    if(currentHapticPlayer != null)
                        currentHapticPlayer.SendHapticImpulse(fillHapticIntensity, fillHapticDuration);
                    Debug.Log($"Glass is full! Cannot add more {ingredient.Name}.");
                    return;
                }

                fillAmount += actualAddedAmount;

                if (ingredients.ContainsKey(ingredient.Name))
                    ingredients[ingredient.Name].Amount += actualAddedAmount;
                else {
                    ingredients[ingredient.Name] = ingredient.copy();
                    orderCounter++;
                }
            }
            else
            {
                if (ingredients.ContainsKey(ingredient.Name))
                    ingredients[ingredient.Name].Amount += inputAmount;
                else {
                    ingredients[ingredient.Name] = ingredient.copy(orderCounter);
                    orderCounter++;
                }
            }    
            updateLiquidDisplay();
            pouringSession = true;
            checkingForSessionEnd = true;
            if(hapticCoroutine == null)
                hapticCoroutine = StartCoroutine(HapticFeedbackRoutine()); 
        }
        
        

        public override IngredientBase createPouredMixture(float pourAmount)
        {
            if (fillAmount <= 0)
                return null;

            float totalCurrentLiquid = fillAmount;
            float actualPouredAmount = Mathf.Min(pourAmount, totalCurrentLiquid);

            if (ingredients.Count == 1)
            {
                IngredientBase singleIngredient = ingredients.Values.First();
                float amountToPour = actualPouredAmount;
                singleIngredient.Amount -= amountToPour;
                fillAmount -= amountToPour;
                if (singleIngredient.Amount <= 0)
                {
                    ingredients.Remove(singleIngredient.Name);
                }
                return new IngredientBase(singleIngredient.Name, amountToPour, singleIngredient.Type, singleIngredient.Color, singleIngredient.AlcoholContent);
            }

            List<string> ingredientNames = new List<string>();
            IngredientBase pouredMixture = new IngredientBase("", 0, IngredientType.MixedLiquid, Color.clear);
            Color objectColor = new Color(0, 0, 0, 0);
            Vector4 sum= new Vector4(0, 0, 0, 0);
            foreach (var kvp in ingredients)
            {
                IngredientBase ingredient = kvp.Value;
                if(ingredient == null)
                    continue;


                float proportion = ingredient.Amount / totalCurrentLiquid;
                float amountToPour = actualPouredAmount * proportion;
                SerializedDictionary<string, IngredientBase> ingredientsList = pouredMixture.ingredients;
                if (ingredientsList.ContainsKey(ingredient.Name))
                {
                    ingredientsList[ingredient.Name].Amount += amountToPour;
                }
                else
                {
                    IngredientBase ind = ingredient.copy();
                    ingredientsList.Add(ind.Name,ind);
                }

                ingredient.Amount -= amountToPour;

                if(ingredient.Amount <= 0)
                {
                    ingredients.Remove(kvp.Key);
                    continue;
                }

                if(!ingredient.solid){

                    ingredientNames.Add(ingredient.Name);
                    sum = new Vector4(sum.x + ingredient.Color.r*(kvp.Value.Amount/fillAmount)
                    , sum.y + ingredient.Color.g*(kvp.Value.Amount/fillAmount)
                    , sum.z + ingredient.Color.b*(kvp.Value.Amount/fillAmount)
                    , sum.w + ingredient.Color.a*(kvp.Value.Amount/fillAmount)
                    );
                }
               
            }
            Debug.Log(sum);
            pouredMixture.Color = new Color(sum.x,sum.y,sum.z,sum.w);

            if(pouredMixture.Color == null)
                pouredMixture.Color = Color.white;
            if (pouredMixture.Color == Color.clear)
                pouredMixture.Color = Color.white;


            fillAmount -= actualPouredAmount;

            pouredMixture.Name = string.Join(", ", ingredientNames.Take(3));
            if (ingredientNames.Count > 3)
                pouredMixture.Name += " & more";
            updateLiquidDisplay();

            return pouredMixture;
        }

        public override Color getLiquidColor()
        {
            if (lastCheckColorCount != ingredients.Count) {
                materialHaveBeenChange = true;
                lastCheckColorCount = ingredients.Count;
                IngredientBase mix = createPouredMixture(0);
                if(mix == null){
                    outputColor = Color.magenta;
                }else if(mix.Color == null){
                    outputColor = Color.magenta;
                }else{
                    outputColor = mix.Color;
                }
                Debug.Log(outputColor.ToString());
            }
            return outputColor;
        }

        /// <summary>
        /// Sort this dictionary of container and its ingredients into a sorted list
        /// and filter out mixtures, keeping only base components.
        /// </summary>
        /// <returns>List of base ingredients in order</returns>
        public List<IngredientBase> getIngreidentsAsOrderedeList()
        {
            List<IngredientBase> orderedIngredients = new List<IngredientBase>();
            Debug.Log("orderingrend null: " + (orderedIngredients == null));

            var sortedIngredients = ingredients.Values.OrderBy(ing => ing.step.order);
            Debug.Log("sortedIngredients null: " + (sortedIngredients == null));

            foreach (IngredientBase ingredient in sortedIngredients)
            {
                addIngredientRecursively(ingredient, orderedIngredients);
            }
            orderedIngredients = orderedIngredients
                .Where(ing => ing.ingredients == null || ing.ingredients.Count == 0)
                .ToList();

            return orderedIngredients;
        }


        /// <summary>
        /// Recursively adds an ingredient and its nested ingredients to the list.
        /// </summary>
        private void addIngredientRecursively(IngredientBase ingredient, List<IngredientBase> orderedList)
        {
            if (!orderedList.Contains(ingredient))
                orderedList.Add(ingredient);

            var nestedIngredients = ingredient.ingredients.Values.OrderBy(ing => ing.step.order);
            foreach (var nestedIngredient in nestedIngredients)
            {
                addIngredientRecursively(nestedIngredient, orderedList);
            }
        }

        public float FillPercentage(){
            float fill = fillAmount / maxFill;
            return fill; 
        }

        internal override void updateLiquidDisplay()
        {
            if(debugGlassMenu != null)
            {
                debugGlassMenu.updateLiquidDisplay();
            }
        }

        internal override void drinkOnStart()
        {
            if (iceFill != null)
            {
                foreach (Transform child in iceFill)
                {
                    child.gameObject.SetActive(false);
                }
            }
            if (xrGrabInteractable == null)
                xrGrabInteractable = gameObject.GetComponent<XRGrabInteractable>();

            if (ingridentTextDisplay != null)
            {
                updateLiquidDisplay();
            }
            updateFromSettings();

            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

        }

        internal void updateFromSettings(){
            gameSettings = GameManager.Instance.gameSettings;
            intensity = gameSettings.HapticMinIntensity;
            duration = gameSettings.HapticDuration;
            minIntensity = gameSettings.HapticMinIntensity;
            maxIntensity = gameSettings.HapticMaxIntensity;
            routineWait = gameSettings.HapticRoutineWait;
            fillHapticDuration = gameSettings.Haptic_Fill_Contatiner_HapticDuration;
            fillHapticIntensity = gameSettings.Haptic_Fill_Contatiner_HapticIntensity;

        }

        public void activateDrinkDisplayOnSelect(SelectEnterEventArgs arg0)
        {
            Debug.Log("activateDrinkDisplayOnSelect " + ingridentTextDisplay.gameObject.activeSelf);
            ingridentTextDisplay.gameObject.SetActive(true);
            updateLiquidDisplay();
            displayNeedToUpdate = true;
            
        }

        public void deactivateDrinkDisplayOnSelect(SelectExitEventArgs arg0)
        {
            updateLiquidDisplay();
            ingridentTextDisplay.gameObject.SetActive(false);
            displayNeedToUpdate = false;
        }
        bool displayNeedToUpdate = false;
        public override void deactivateDrinkDisplay(HoverExitEventArgs arg0)
        {
            if(!displayNeedToUpdate){
                base.deactivateDrinkDisplay(arg0);
            }
                

        }

        public bool addIceToContainer(IngredientBase ice){

            hasIce = true;
            //Code to add ice to glass 
            if(iceFill.childCount > 0 || iceFill.childCount < (iceCount+1)){
                iceCount++;
                iceFill.GetChild(iceCount).gameObject.SetActive(true);
                AddIngredient(ice, ice.Amount, out float actualAddedAmount);
                int randomIndex = UnityEngine.Random.Range(0, iceSounds.Count);
                iceSound = iceSounds[randomIndex];
                audioSource.clip = iceSound;
                audioSource.volume = glaseVolume;
                audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
                audioSource.Play();
                return true;

            }
            return false;
            
        }

        void OnEnable()
        {
            if(xrGrabInteractable == null)
                xrGrabInteractable = gameObject.GetComponent<XRGrabInteractable>();
            xrGrabInteractable.selectEntered.AddListener(findHapticController);
            xrGrabInteractable.selectExited.AddListener(removeHapticController);
        }

        private void removeHapticController(SelectExitEventArgs arg0)
        {
            currentHapticPlayer = null;
            if (hapticCoroutine != null)
                StopCoroutine(hapticCoroutine);
            intensity = gameSettings.HapticMinIntensity;
        }

        void OnDisable()
        {
            xrGrabInteractable.selectEntered.RemoveListener(findHapticController);
            xrGrabInteractable.selectExited.RemoveListener(removeHapticController);
        }
        void FixedUpdate()
        {
            if(checkingForSessionEnd){
                checkingForSessionEnd = false;
                sessionCoroutine= StartCoroutine(endSession());
            }
        }
        //Haptics

        internal void findHapticController(SelectEnterEventArgs arg0){
            currentHapticPlayer = arg0.interactorObject.transform.parent.GetComponent<HapticImpulsePlayer>();
            if (currentHapticPlayer == null)
            {
                Debug.LogWarning("Interactor does not have a HapticImpulsePlayer component.");
                return;
            }

        }
        bool checkingForSessionEnd = false;
        internal IEnumerator endSession(){
            checkingForSessionEnd = true;
                while(checkingForSessionEnd)
            yield return new WaitForSeconds(0.001f);
            pouringSession = false;
        }
        internal IEnumerator HapticFeedbackRoutine()
        {
            while (pouringSession)
            {
                if (currentHapticPlayer != null){
                    Debug.Log("HapticFeedbackRoutine started " + transform.name);
                    currentHapticPlayer.SendHapticImpulse(intensity, duration);
                }
                    

                // Increase intensity, but clamp to 1
                intensity = Mathf.Min(intensity + minIntensity, maxIntensity);

                yield return new WaitForSeconds(routineWait);
            }
            

        }

    }
}

