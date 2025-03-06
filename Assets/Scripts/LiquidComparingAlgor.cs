using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Drink_interaction;
using Assets.Scripts.Ingridence;
using AYellowpaper.SerializedCollections;
using UnityEngine;

public class LiquidComparingAlgor : MonoBehaviour
{
    public LiquidContainerLimited drink;
    public LiquidContainerLimited test;

    void Start()
    {
        Compare(drink, test);
    }


    void Compare(LiquidContainerLimited ideal, LiquidContainerLimited actual)
    {
        List<IngredientBase> idealList = ideal.getIngreidentsAsOrderedeList();
        List<IngredientBase> actualList = actual.getIngreidentsAsOrderedeList();

        var idealNames = new HashSet<string>(idealList.Select(i => i.Name));
        var actualNames = new HashSet<string>(actualList.Select(i => i.Name));
        var wrongIngredients = actualNames.Except(idealNames).ToList();

        // 3. Measure Pouring Accuracy
        Dictionary<string, float> idealAmounts = idealList.ToDictionary(i => i.Name, i => i.Amount);
        Dictionary<string, float> actualAmounts = actualList.ToDictionary(i => i.Name, i => i.Amount);

        float totalDeviation = 0f;
        float totalOverpour = 0f;
        float totalUnderpour = 0f;
        List<string> overpourList = new List<string>();
        List<string> underpourList = new List<string>();

        foreach (var actualIngredient in actualAmounts)
        {
            string name = actualIngredient.Key;
            float actualAmount = actualIngredient.Value;
            float idealAmount = idealAmounts.ContainsKey(name) ? idealAmounts[name] : 0f;

            float difference = actualAmount - idealAmount;
            totalDeviation += Mathf.Abs(difference);

            if (idealAmount == 0)
            {
                wrongIngredients.Add(name);
            }
            else if (difference > 0)
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

        bool incorrectMixing = !idealList.Select(i => i.Name).SequenceEqual(actualList.Select(i => i.Name));
        float totalScore = Mathf.Max( 100 - (10 * wrongIngredients.Count) - (2*totalOverpour) - (1* totalUnderpour) , 0);

        Debug.Log("========== DRINK MIX REPORT ==========");
        Debug.Log($"Ideal Ingredients: [{string.Join(", ", idealList.Select(i => $"{i.Name} ({i.Amount})"))}]");
        Debug.Log($"Actual Ingredients: [{string.Join(", ", actualList.Select(i => $"{i.Name} ({i.Amount})"))}]");
        Debug.Log($"Wrong Ingredients: [{(wrongIngredients.Count > 0 ? string.Join(", ", wrongIngredients) : "None")}]");
        Debug.Log($"Overpour: {totalOverpour} ({(overpourList.Count > 0 ? string.Join(", ", overpourList) : "None")})");
        Debug.Log($"Underpour: {totalUnderpour} ({(underpourList.Count > 0 ? string.Join(", ", underpourList) : "None")})");
        Debug.Log($"Total Pouring Deviation: {totalDeviation}");
        Debug.Log($"Incorrect Mixing Order: {incorrectMixing}");
        Debug.Log($"\nTotal score: {totalScore}\n");
        Debug.Log("=======================================");   
    }
}
