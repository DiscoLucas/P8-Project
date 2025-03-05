using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Ingridence;
using Assets.Scripts.Orders;
using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;

/// <summary>
/// Contains and controll the recipes
/// </summary>
public class RecipeManager : MonoBehaviour
{
    [SerializedDictionary("Key", "Cocktail Recipe")]
    public SerializedDictionary<string, CocktailRecipe> recipes;

    /// <summary>
    /// Get a random cocktail recipe
    /// </summary>
    /// <param name="recipeKey">return the key of this recipe in the list</param>
    /// <returns></returns>
    public CocktailRecipe getRandomCocktailRecipe(out string recipeKey) {
        int index = Random.Range(0, recipes.Count);
        CocktailRecipe recipe = recipes.Values.ElementAt(index);
        recipeKey = recipes.Keys.ElementAt(index);
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
    public float compareTwoIngridienseList(List<IngredientBase> idealList, List<IngredientBase> actualList, out int wrongIngreidentCount, out float totalDeviation, out float totalOverpour, out float totalUnderpour)
    {
        wrongIngreidentCount = 0;
        totalDeviation = 0f;
        totalOverpour = 0f;
        totalUnderpour = 0f;

        var idealNames = new HashSet<string>(idealList.Select(i => i.Name));
        var actualNames = new HashSet<string>(actualList.Select(i => i.Name));

        List<string> wrongIngredients = new List<string>();

        Dictionary<string, float> idealAmounts = idealList.ToDictionary(i => i.Name, i => i.Amount);
        Dictionary<string, float> actualAmounts = actualList.ToDictionary(i => i.Name, i => i.Amount);

        List<string> overpourList = new List<string>();
        List<string> underpourList = new List<string>();

        foreach (var actualIngredient in actualAmounts)
        {
            string name = actualIngredient.Key;
            float actualAmount = actualIngredient.Value;
            float idealAmount = idealAmounts.ContainsKey(name) ? idealAmounts[name] : 0f;

            if (idealAmount == 0)
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
        }

        float totalScore = Mathf.Max(100 - (10 * wrongIngredients.Count) - (2 * totalOverpour) - (1 * totalUnderpour), 0);

        Debug.Log("========== DRINK MIX REPORT ==========");
        Debug.Log($"Ideal Ingredients: [{string.Join(", ", idealList.Select(i => $"{i.Name} ({i.Amount})"))}]");
        Debug.Log($"Actual Ingredients: [{string.Join(", ", actualList.Select(i => $"{i.Name} ({i.Amount})"))}]");
        Debug.Log($"Wrong Ingredients: [{(wrongIngredients.Count > 0 ? string.Join(", ", wrongIngredients) : "None")}]");
        Debug.Log($"Overpour: {totalOverpour} ({(overpourList.Count > 0 ? string.Join(", ", overpourList) : "None")})");
        Debug.Log($"Underpour: {totalUnderpour} ({(underpourList.Count > 0 ? string.Join(", ", underpourList) : "None")})");
        Debug.Log($"Total Pouring Deviation: {totalDeviation}");
        Debug.Log($"Total score: {totalScore}");
        Debug.Log("=======================================");

        wrongIngreidentCount = wrongIngredients.Count;

        return totalScore;
    }

}
