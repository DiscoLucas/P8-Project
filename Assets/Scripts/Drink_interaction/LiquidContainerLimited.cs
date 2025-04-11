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
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Assets.Scripts.Drink_interaction
{
    /// <summary>
    /// A liquid container that contain a finiate amount. used for glasses and alike
    /// </summary>
    public class LiquidContainerLimited : LiquidContainer
    {
        protected int lastCheckColorCount = 0;

        [SerializeField]
        protected Color outputColor = Color.white;
        protected int orderCounter = 0;
        public GlassType glassType;

        [Header("Garnishing")]
        public GameObject garnish = null;
        public Transform garnishPoint;
        IngredientBase garnishIngredient = null;
        public bool hasGarnish = false;

        [Header("Solid glass")]
        [SerializeField]
        bool iceIn = false;
        [SerializeField]
        string iceInTextString = "Ice", noIceTextString = "No Ice";

        [Header("Liquid Display")]
        [SerializeField]
        Slider drinkSlider;
        [SerializeField]
        TMP_Text glassTypeText,IceInText,alcoholTypeText, softDrinkText, garnishText;
        [Header("object state")]
        [SerializeField]
        Transform stirredStateObject,ShakenStateObject,StrainedStateObject;


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

        string softDrinkContain = "", alcoholDrinkContain = "";
        void updateTheIngredientDisplay(){
            softDrinkContain = "";
            alcoholDrinkContain = "";
            Debug.Log(" Looping through ingredients: " + ingredients.Count);
            foreach (IngredientBase ingredientBase in ingredients.Values)
            {
                Debug.Log("Drink step: " + ingredientBase.step.action);
                if (ingredientBase.Type == IngredientType.Mixer || ingredientBase.Type == IngredientType.Sirup )
                {
                    softDrinkContain += $"\n[{ingredientBase.Amount}]{ingredientBase.Name},";
                }
                else if (ingredientBase.Type == IngredientType.Spirit)
                {
                    alcoholDrinkContain += $"\n[{ingredientBase.Amount}]{ingredientBase.Name},";
                }
                if(ingredientBase.step.action == DrinkAction.Stirred){
                    stirredStateObject.gameObject.SetActive(true);
                    //Debug.Log("Drink has been Stirred");
                }
                if(ingredientBase.step.action == DrinkAction.Shaked){
                    ShakenStateObject.gameObject.SetActive(true);
                    //Debug.Log("Drink has been Shacken");
                }
                if(ingredientBase.step.action == DrinkAction.Strained){
                    StrainedStateObject.gameObject.SetActive(true);
                    //Debug.Log("Drink has been Strained");
                }
                
            }
            //Debug.Log("Soft drink: " + softDrinkContain);
            //Debug.Log("Alcohol drink: " + alcoholDrinkContain);
            //Debug.Log("Garnish: " + garnishIngredient?.Name);
        }
        void OnCollisionEnter(Collision collision)
        {
            if(collision.gameObject.tag == "Garnish" && !hasGarnish)
            setGarnish(collision.gameObject); hasGarnish = true;
        }

        public override void AddIngredient(IngredientBase ingredient, float inputAmount)
        {
            if (ingredient.solid == false)
            {
                float availableSpace = maxFill - fillAmount;
                float actualAddedAmount = Mathf.Min(inputAmount, availableSpace);

                if (actualAddedAmount <= 0)
                {
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
                updateLiquidDisplay();
            }
            else
            {
                if (ingredients.ContainsKey(ingredient.Name))
                    ingredients[ingredient.Name].Amount += inputAmount;
                else {
                    ingredients[ingredient.Name] = ingredient.copy(orderCounter);
                    orderCounter++;
                }
                updateLiquidDisplay();
            }     
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
                float amountToPour = Mathf.Min(singleIngredient.Amount, actualPouredAmount);
                singleIngredient.Amount -= amountToPour;
                fillAmount -= amountToPour;

                return new IngredientBase(singleIngredient.Name, amountToPour, singleIngredient.Type, singleIngredient.Color, singleIngredient.AlcoholContent);
            }

            List<string> ingredientNames = new List<string>();
            IngredientBase pouredMixture = new IngredientBase("", 0, IngredientType.MixedLiquid, Color.clear);
            Color objectColor = new Color(0, 0, 0, 0);
            Vector4 sum= new Vector4(0, 0, 0, 0);
            foreach (var kvp in ingredients)
            {
                IngredientBase ingredient = kvp.Value;
                float proportion = ingredient.Amount / totalCurrentLiquid;
                float amountToPour = actualPouredAmount * proportion;

                
                Debug.Log(kvp.Value.Amount);
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
                }else{
                    ingredientNames.Add(ingredient.Name);
                    ingredient.ingredients = ingredientsList;
                    Debug.Log( " color : "+ kvp.Value.Color + " of " + kvp.Value.Name);
                    sum = new Vector4(sum.x + ingredient.Color.r*(kvp.Value.Amount/fillAmount)
                    , sum.y + ingredient.Color.g*(kvp.Value.Amount/fillAmount)
                    , sum.z + ingredient.Color.b*(kvp.Value.Amount/fillAmount)
                    , sum.w + ingredient.Color.a*(kvp.Value.Amount/fillAmount)
                    );
                }
               
            }
            Debug.Log(sum);
            pouredMixture.Color = new Color(sum.x,sum.y,sum.z,sum.w);
            fillAmount -= actualPouredAmount;

            pouredMixture.Name = string.Join(", ", ingredientNames.Take(3));
            if (ingredientNames.Count > 3)
                pouredMixture.Name += " & more";
            
            updateTheIngredientDisplay();
            updateLiquidDisplay();

            return pouredMixture;
        }

        public override Color getLiquidColor()
        {
            if (lastCheckColorCount != ingredients.Count) {
                materialHaveBeenChange = true;
                lastCheckColorCount = ingredients.Count;
                IngredientBase mix = createPouredMixture(0);
                outputColor = mix.Color;
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
            return(fillAmount / maxFill); 
        }

        internal override void updateLiquidDisplay()
        {
            updateTheIngredientDisplay();
            drinkSlider.value = FillPercentage();
            glassTypeText.text = glassType.ToString();
            IceInText.text = iceIn ? iceInTextString : noIceTextString;
            alcoholTypeText.text = alcoholDrinkContain;
            softDrinkText.text = softDrinkContain;
            garnishText.text = garnishIngredient != null ? garnishIngredient.Name : "No Garnish";

        }

        internal override void drinkOnStart()
        {
            if (xrGrabInteractable == null)
                xrGrabInteractable = gameObject.GetComponent<XRGrabInteractable>();

            if (ingridentTextDisplay != null)
            {
                ingridentTextDisplay.gameObject.SetActive(false);
                StrainedStateObject.gameObject.SetActive(false);
                ShakenStateObject.gameObject.SetActive(false);
                stirredStateObject.gameObject.SetActive(false);
                xrGrabInteractable.hoverEntered.AddListener(activateDrinkDisplay);
                xrGrabInteractable.hoverExited.AddListener(deactivateDrinkDisplay);

                xrGrabInteractable.selectEntered.AddListener(activateDrinkDisplayOnSelect);
                xrGrabInteractable.selectExited.AddListener(deactivateDrinkDisplayOnSelect);
                updateTheIngredientDisplay();
                updateLiquidDisplay();
                
            }
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
    }
}

