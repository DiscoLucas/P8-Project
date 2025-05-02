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
        internal int iceCount = 0;

        public float delteICeThreashold = 0.5f;

        [Header("Debug liquid display")]
        public DebugClassMenu debugGlassMenu;

        [Header("Haptics")]
        public HapticImpulsePlayer currentHapticPlayer;

        [Range(0,1)]
        public float intensity = 0.01f;
        public float duration = 0.04f;
        internal Coroutine hapticCoroutine = null;
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

        public virtual void setGarnish(GameObject garnish)
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
            hasGarnish = true;
        }
        void OnCollisionEnter(Collision collision)
        {
            if(collision.gameObject.tag == "Garnish" && !hasGarnish){
                setGarnish(collision.gameObject); 
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
        #region add ingredient
        internal bool pouringSession = false;
        public override void AddIngredient(IngredientBase ingredient, float inputAmount, out float actualAddedAmount)
        {
            actualAddedAmount= 0;
            if(inputAmount <= 0)
            {
                Debug.LogWarning("Input amount must be greater than 0.");
                return;
            }

            int garnishCount = hasGarnish ? 1 : 0;
            float availableSpace = (maxFill+ garnishCount) - fillAmount;
            actualAddedAmount = Mathf.Min(ConvertToInternalUnits(inputAmount), availableSpace);

            if (actualAddedAmount <= 0)
            {

                sendOneShotHaptic(fillHapticIntensity, fillHapticDuration);
                Debug.Log($"Glass is full! Cannot add more {ingredient.Name}.");
                return;
            }
            if(ingredient.Type != IngredientType.Garnish)
                fillAmount += actualAddedAmount;

            if (ingredients.ContainsKey(ingredient.Name))
                ingredients[ingredient.Name].Amount += actualAddedAmount;
            else {
                ingredients[ingredient.Name] = ingredient.copy();
                ingredients[ingredient.Name].Amount = actualAddedAmount;
                orderCounter++;
            }
            
            actualAddedAmount = ConvertToMilliliters(actualAddedAmount);
            updateLiquidDisplay();
            sendOneShotHaptic(fillHapticIntensity, fillHapticDuration);
                
        }
        #endregion
        
        #region create poured mixture
        public override IngredientBase createPouredMixture(float pourAmount, bool removeAmount)
        {
            float internalUnitPourAmount = Mathf.Min(ConvertToInternalUnits(pourAmount), (maxFill - fillAmount));
            float pourAmountinML = ConvertToMilliliters(internalUnitPourAmount);
            IngredientBase pouredMixture = null;

            if (ingredients.Count <= 0)
            {
                Debug.LogWarning("No ingredients to pour from " + transform.name);
                return null;
            }
            else if (ingredients.Count == 1)
            {
                pouredMixture = ingredients.First().Value.copy();
                pouredMixture.Amount = pourAmountinML;

                if (removeAmount)
                {
                    ingredients[pouredMixture.Name].Amount = Mathf.Max(0, ingredients[pouredMixture.Name].Amount - internalUnitPourAmount);
                    if (ingredients[pouredMixture.Name].Amount <= 0)
                    {
                        ingredients.Remove(pouredMixture.Name);
                    }
                }

                fillAmount = MathF.Max(0, fillAmount - internalUnitPourAmount);
                
                Debug.Log($"[DEBUG] Single ingredient poured: {pouredMixture.Name}, Amount: {pouredMixture.Amount}ml");
            }
            else if (ingredients.Count > 1)
            {

                pouredMixture = new IngredientBase(
                    "Mixture",
                    pourAmountinML,
                    IngredientType.MixedLiquid,
                    Color.yellow,
                    0,
                    0,
                    DrinkAction.None
                );
                Color color = new Color(0, 0, 0, 0);
                List<String> removeKeys = new List<string>();
                foreach (var kvp in ingredients)
                {
                    IngredientBase ingredient = kvp.Value;
                    if(ingredient == null)
                        continue;
                    if (fillAmount <= 0){
                        removeKeys.Add(kvp.Key);
                        continue;
                    }
                        
                    float proporation = ingredient.Amount / fillAmount;
                    float subtractAmount = internalUnitPourAmount * proporation;
                    float coloramout = ingredient.Amount/ fillAmount;
                    SerializedDictionary<string, IngredientBase> ingredientsList = pouredMixture.ingredients;
                    if (ingredientsList.ContainsKey(ingredient.Name))
                    {
                        ingredientsList[ingredient.Name].Amount += subtractAmount;
                    }
                    else
                    {
                        IngredientBase ind = ingredient.copy();
                        ingredientsList.Add(ind.Name,ind);
                    }

                    ingredient.Amount -= subtractAmount;

                    if(ingredient.Amount <= 0)
                    {
                        removeKeys.Add(kvp.Key);
                    }
                    color = new Color(color.r + (ingredient.Color.r * coloramout), color.g + (ingredient.Color.g * coloramout), color.b + (ingredient.Color.b * coloramout), color.a + (ingredient.Color.a * coloramout));
                }

                foreach (string key in removeKeys)
                {
                    ingredients.Remove(key);
                }
                color = new Color(color.r, color.g, color.b, Mathf.Max(0.4f, color.a));
                pouredMixture.Color = color;
                fillAmount = MathF.Max(0, fillAmount - internalUnitPourAmount);
            }
            if(fillAmount <= 0){
                    for(int i = 0; i < ingredients.Count; i++){
                        if(ingredients.ElementAt(i).Value.Amount <= 0 || ingredients.ElementAt(i).Value == null){
                            ingredients.Remove(ingredients.ElementAt(i).Key);
                            i= 0;
                        }
                    }
                }
            return pouredMixture;
        }
        #endregion

        public override Color getLiquidColor()
        {
            if (lastCheckColorCount != ingredients.Count) {
                materialHaveBeenChange = true;
                lastCheckColorCount = ingredients.Count;
                IngredientBase mix = createPouredMixture(maxFillInMl,false);
                if(mix == null){
                    outputColor = Color.magenta;
                }else if(mix.Color == null){
                    outputColor = Color.magenta;
                }else{
                    outputColor = mix.Color;
                }
            }
            return outputColor;
        }

        public override void cleanContainer()
        {
            if(fillAmount <= 0.01f){
                Debug.Log("Cleaning container: " + transform.name);
                ingredients.Clear();
                updateLiquidDisplay();
                if (iceFill != null)
                {
                    foreach (Transform child in iceFill)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
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


            for(int i = 0; i < orderedIngredients.Count; i++){
                if(orderedIngredients[i].Amount <= 0 || orderedIngredients[i] == null){
                    orderedIngredients.RemoveAt(i);
                    i--;
                }

                if(orderedIngredients[i].Type != IngredientType.Garnish){
                    orderedIngredients[i].Amount = ConvertToMilliliters(orderedIngredients[i].Amount);
                }
            }
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
            float fill = fillAmount/ maxFill;
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
            if(pouringSession && hapticCoroutine != null){
                pouringSession = false;
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
        public void sendOneShotHaptic(float intensity, float duration)
        {
            if (currentHapticPlayer != null)
            {
            currentHapticPlayer.SendHapticImpulse(intensity, duration);
            }
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

