using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Ingridence;
using Assets.Scripts.Orders;
using AYellowpaper.SerializedCollections;
using JetBrains.Annotations;
using Meta.XR.ImmersiveDebugger.Gizmo;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

/// <summary>
/// Contains and controll the recipes
/// </summary>
public class RecipeManager : MonoBehaviour
{
    [SerializeField]
    PhaseManager phaseManager;
    PerformanceRecorder performanceRecorder;
    [SerializeField]
    float score = 100f;
    [SerializeField]
    float MAX_INGREDIENT_PENALTY = 50f;
    [SerializeField]
    float INGREDIENT_PENALTY_PER_MISS = 10f;
    [SerializeField]
    float MAX_POUR_PENALTY = 50f;
    [SerializeField]
    float POUR_PENALTY_FACTOR = 50f;
    [SerializeField]
    float MISHANDLED_INGREDIENT_PENALTY = 5f;

    [Header("Lab streaming")]
    [SerializeField]
    string streamName = "DrinkPerformanceMetrics";

    [SerializeField]
    string[] labels = new string[] { "Ideal Ingredients count" , "Actual Ingredients count", "Wrong Ingredients count", "Overpour", "Underpour", "Total Pouring Deviation", "Total score", "Correct Glass", "Mishandled Ingredients", "Ideal Amount", "Actual Amount","Time taken"}; 


    void Start()
    {
        if (PerformanceRecorder.Instance != null)
        {
           
            PerformanceRecorder.Instance.InitializeLSLStream(streamName, "Behavorial metrics", labels.Length, LSL.LSL.IRREGULAR_RATE, 
            LSL.channel_format_t.cf_float32, labels );
        }
        phaseManager = FindAnyObjectByType<PhaseManager>();
        performanceRecorder = FindAnyObjectByType<PerformanceRecorder>();
        if (phaseManager == null)
        {
            Debug.LogError("PhaseManager not found in the scene.");
            return;
        }
        if (performanceRecorder == null)
            Debug.LogWarning("PerformanceRecorder not found in the scene.");
        GameManager.Instance.recipeManager = this;
    }

    public CocktailRecipe getCocktailRecipe(out string recipeKey){
        SerializedDictionary<string, CocktailRecipe> phaseRecipes = phaseManager.getPhasRecipes();
        int index = Random.Range(0, phaseRecipes.Count);
        CocktailRecipe recipe = phaseRecipes.Values.ElementAt(index);
        recipeKey = phaseRecipes.Keys.ElementAt(index);
        return recipe;
    }

    /// <summary>
    /// Take two list of ingredient then compare them and calcualte a score
    /// </summary>
    /// <param name="idealList"> or called Recipe. this is the list that is wantede </param>
    /// <param name="actualList">What have been made</param>
    /// <param name="wrongIngreidentCount">How many ingreidents that was wrong</param>
    /// <param name="totalDeviation">The asbooult differences between amount on drink</param>
    /// <param name="totalOverpour">The total overpour on all ingridents</param>
    /// <param name="totalUnderpour">The total underpour on all ingridents</param>
    /// <returns>The game score of this drink</returns>
    public float compareTwoIngridienseList(List<IngredientBase> idealList, List<IngredientBase> actualList, string recipeID,GlassType currentGlass, float timeTaken, out int wrongIngreidentCount, out float totalDeviation, out float totalOverpour, out float totalUnderpour)
    {
        wrongIngreidentCount = 0;
        totalDeviation = 0f;
        totalOverpour = 0f;
        totalUnderpour = 0f;
        CocktailRecipe cocktailRecipe = phaseManager.getRecipe(recipeID);
        var idealNames = new HashSet<string>(idealList.Select(i => i.Name));
        var actualNames = new HashSet<string>(actualList.Select(i => i.Name));

        List<string> wrongIngredients = new List<string>();

        Dictionary<string, float> idealAmounts = idealList.ToDictionary(i => i.Name, i => i.Amount);
        Debug.Log("############ actualList #########");
        foreach (IngredientBase ing in actualList){
            Debug.Log(ing.Name + " " + ing.Amount);
        }

        Dictionary<string, float> actualAmounts = actualList.GroupBy(i => i.Name).ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

        List<string> overpourList = new List<string>();
        List<string> underpourList = new List<string>();
        float sumActualAmount = 0;
        float sumIdealAmount = 0;
        int mishandledIngredientCount = 0;
        foreach (var actualIngredient in actualAmounts)
        {
            string name = actualIngredient.Key;
            float actualAmount = actualIngredient.Value;
            float idealAmount = idealAmounts.ContainsKey(name) ? idealAmounts[name] : 0f;
            sumActualAmount += actualAmount;
            sumIdealAmount += idealAmount;

            if (!idealAmounts.ContainsKey(name))
            {
                wrongIngredients.Add(name);
                continue;
            }

            float difference = actualAmount - idealAmount;
            totalDeviation += Mathf.Abs(difference);

            if (difference > 0)
            {
                totalOverpour += difference;
                overpourList.Add($"{name} (+{difference})");
            }
            else if (difference < 0)
            {
                totalUnderpour += Mathf.Abs(difference);
                underpourList.Add($"{name} ({difference})");
            }


            IngredientBase idealIngredient = idealList.FirstOrDefault(i => i.Name == name);
            IngredientBase actualIngredientObj = actualList.FirstOrDefault(i => i.Name == name);

            if (idealIngredient != null && actualIngredientObj != null)
            {
                if (idealIngredient.step.action != actualIngredientObj.step.action)
                {
                    mishandledIngredientCount++;
                }
            }
        }


        bool correctGlass = cocktailRecipe.glassType == currentGlass;
        float totalScore =  calculateDrinkScore(wrongIngredients.Count,idealNames.Count,timeTaken, cocktailRecipe.expectedTime, sumActualAmount,sumIdealAmount,mishandledIngredientCount,correctGlass, cocktailRecipe.maxScore);

        Debug.Log("========== DRINK MIX REPORT ==========");
        Debug.Log($"Ideal Ingredients: [{string.Join(", ", idealList.Select(i => $"{i.Name} ({i.Amount})"))}]");
        Debug.Log($"Actual Ingredients: [{string.Join(", ", actualList.Select(i => $"{i.Name} ({i.Amount})"))}]");
        Debug.Log($"Wrong Ingredients: [{(wrongIngredients.Count > 0 ? string.Join(", ", wrongIngredients) : "None")}]");
        Debug.Log($"Overpour: {totalOverpour} ({(overpourList.Count > 0 ? string.Join(", ", overpourList) : "None")})");
        Debug.Log($"Underpour: {totalUnderpour} ({(underpourList.Count > 0 ? string.Join(", ", underpourList) : "None")})");
        Debug.Log($"Total Pouring Deviation: {totalDeviation}");
        Debug.Log($"Time taken: {timeTaken}");
        Debug.Log($"Total score: {totalScore}");
        Debug.Log("=======================================");

        wrongIngreidentCount = wrongIngredients.Count;
        try{
            if (performanceRecorder != null)
            {
                CultureInfo culture = new CultureInfo("en-US");
                string[] data = new string[labels.Length];
                data[0] = "c"+idealNames.Count.ToString(culture);
                data[1] = "c"+actualNames.Count.ToString(culture);
                data[2] = "c"+wrongIngredients.Count.ToString(culture);
                data[3] = "p"+totalOverpour.ToString(culture);
                data[4] = "p"+totalUnderpour.ToString(culture);;
                data[5] = "p"+totalDeviation.ToString(culture);;
                data[6] = "s"+totalScore.ToString(culture);;
                data[7] = (correctGlass ? 1 : 0).ToString(culture);;
                data[8] = "c"+mishandledIngredientCount.ToString(culture);;
                data[9] = "a"+sumIdealAmount.ToString(culture);;
                data[10] = "a"+sumActualAmount.ToString(culture);;
                data[11] = "t"+timeTaken.ToString(culture);;
                PerformanceRecorder.Instance.RecordStreamData(streamName, data);
            }
        }catch(System.Exception e){
            Debug.LogError("Error in performance recorder: " + e.Message);
        }
        return totalScore;
    }

    public float calculateDrinkScore(int wrongIngredients, int idealIngredients, float timeTaken, float expectedTime, float actualAmount, float idealAmount, int mishandledIngredientCount,bool correctGlass, float maxScore)
    {
        float finalScore = maxScore;
        float ingredientPenalty = Mathf.Clamp(wrongIngredients * INGREDIENT_PENALTY_PER_MISS, 0f, MAX_INGREDIENT_PENALTY);
        finalScore -= ingredientPenalty;
        float mishandledPenalty = Mathf.Clamp(mishandledIngredientCount * 5f, 0f, MAX_INGREDIENT_PENALTY);
        finalScore -= mishandledPenalty;
        float pourPenalty = Mathf.Clamp((Mathf.Abs(actualAmount - idealAmount) / idealAmount) * POUR_PENALTY_FACTOR, 0f, MAX_POUR_PENALTY);
        finalScore -= pourPenalty;
        float timePenalty = 0f;
        if (timeTaken > expectedTime)
        {
            float overtimeRatio = (timeTaken - expectedTime) / expectedTime;
            timePenalty = Mathf.Clamp(overtimeRatio * 20f, 0f, MAX_POUR_PENALTY);
        }
        finalScore -= timePenalty;
        if (!correctGlass)
        {
            finalScore -= 10f;
        }
        return Mathf.Max(finalScore, 0f);
    }

    public float calculateDrinkAccurary(int wrongIngredients, int idealIngredient, float actualAmount, float idealAmount, int mishandledIngredientCount, bool correctGlass)
    {
        float finalScore = 100f;
        float ingredientPenalty = Mathf.Clamp(wrongIngredients * INGREDIENT_PENALTY_PER_MISS, 0f, MAX_INGREDIENT_PENALTY);
        finalScore -= ingredientPenalty;
        float mishandledPenalty = Mathf.Clamp(mishandledIngredientCount * 5f, 0f, MAX_INGREDIENT_PENALTY);
        finalScore -= mishandledPenalty;
        float pourPenalty = Mathf.Clamp((Mathf.Abs(actualAmount - idealAmount) / idealAmount) * POUR_PENALTY_FACTOR, 0f, MAX_POUR_PENALTY);
        finalScore -= pourPenalty;
        if (!correctGlass)
        {
            finalScore -= 10f;
        }
        return Mathf.Max(finalScore, 0f);
    }


    public void setAndFireMarker(string markerName){
        if (performanceRecorder != null)
        {
            PerformanceRecorder.Instance.RecordMarker(streamName, markerName);
        }
    }
}
