using AYellowpaper.SerializedCollections;
using UnityEngine;
using System.Xml;
using System.IO;
using Assets.Scripts.Ingridence;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PhaseManager : MonoBehaviour
{
    [SerializeField]
    PhaseOrder[] phases;
    [SerializeField]
    int currentPhaseIndex = 0;
    
    [SerializeField]
    private TextAsset recipesXmlFile;

    void Start()
    {
        GameManager.Instance.phaseManager = this;
    }

    #if UNITY_EDITOR
    [ContextMenu("Update All Phases From XML")]
    public void UpdatePhasesFromXml()
    {
        if (recipesXmlFile == null)
        {
            Debug.LogError("No XML file assigned! Please assign a TextAsset containing recipe XML data.");
            return;
        }
        
        LoadRecipesFromXml(recipesXmlFile.text);
        EditorUtility.SetDirty(this);
    }
    #endif
    
    public void LoadRecipesFromXml(string xmlContent)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);
        
        XmlNodeList phaseNodes = xmlDoc.SelectNodes("//CocktailRecipes/Phases/Phase");
        
        if (phaseNodes.Count != phases.Length)
        {
            Debug.LogWarning($"XML contains {phaseNodes.Count} phases but PhaseManager has {phases.Length} phases!");
        }
        
        for (int i = 0; i < Mathf.Min(phaseNodes.Count, phases.Length); i++)
        {
            XmlNode phaseNode = phaseNodes[i];
            PhaseOrder phase = phases[i];
            
            // Update phase name
            string phaseName = phaseNode.Attributes["name"]?.Value;
            if (!string.IsNullOrEmpty(phaseName))
            {
                phase.Name = phaseName;
            }
            
            // Clear existing recipes and add new ones
            phase.recipes.Clear();
            
            XmlNodeList recipeNodes = phaseNode.SelectNodes("Recipe");
            foreach (XmlNode recipeNode in recipeNodes)
            {
                string recipeId = recipeNode.Attributes["id"]?.Value;
                if (string.IsNullOrEmpty(recipeId))
                {
                    Debug.LogWarning("Recipe without ID found, skipping.");
                    continue;
                }
                
                CocktailRecipe recipe = ParseRecipe(recipeNode);
                phase.recipes[recipeId] = recipe;
            }
        }
        
        Debug.Log("Recipes updated successfully from XML!");
    }
    
    private CocktailRecipe ParseRecipe(XmlNode recipeNode)
    {
        CocktailRecipe recipe = new CocktailRecipe();
        
        recipe.Name = recipeNode.SelectSingleNode("Name")?.InnerText ?? "Unnamed Recipe";
        
        int difficulty;
        if (int.TryParse(recipeNode.SelectSingleNode("Difficulty")?.InnerText, out difficulty))
        {
            recipe.diffuculty = Mathf.Clamp(difficulty, 1, 10);
        }
        
        float maxScore;
        if (float.TryParse(recipeNode.SelectSingleNode("MaxScore")?.InnerText, out maxScore))
        {
            recipe.maxScore = maxScore;
        }
        
        float expectedTime;
        if (float.TryParse(recipeNode.SelectSingleNode("ExpectedTime")?.InnerText, out expectedTime))
        {
            recipe.expectedTime = expectedTime;
        }
        
        string glassTypeStr = recipeNode.SelectSingleNode("GlassType")?.InnerText;
        if (!string.IsNullOrEmpty(glassTypeStr) && System.Enum.TryParse(glassTypeStr, out GlassType glassType))
        {
            recipe.glassType = glassType;
        }
        
        // Parse ingredients
        XmlNodeList ingredientNodes = recipeNode.SelectNodes("Ingredients/Ingredient");
        List<IngredientBase> ingredients = new List<IngredientBase>();
        
        foreach (XmlNode ingredientNode in ingredientNodes)
        {
            IngredientBase ingredient = ParseIngredient(ingredientNode);
            ingredients.Add(ingredient);
        }
        
        recipe.ingredients = ingredients.ToArray();
        
        return recipe;
    }
    
    private IngredientBase ParseIngredient(XmlNode ingredientNode)
    {
        string name = ingredientNode.SelectSingleNode("Name")?.InnerText ?? "Unnamed Ingredient";
        
        float amount = 0;
        float.TryParse(ingredientNode.SelectSingleNode("Amount")?.InnerText, out amount);
        
        bool solid = false;
        bool.TryParse(ingredientNode.SelectSingleNode("Solid")?.InnerText, out solid);
        
        IngredientType type = IngredientType.Other; 
        System.Enum.TryParse(ingredientNode.SelectSingleNode("Type")?.InnerText, out type);
        
        Color color = Color.magenta;
        XmlNode colorNode = ingredientNode.SelectSingleNode("Color");
        if (colorNode != null)
        {
            float r = 1, g = 0, b = 1, a = 1;
            float.TryParse(colorNode.SelectSingleNode("r")?.InnerText, out r);
            float.TryParse(colorNode.SelectSingleNode("g")?.InnerText, out g);
            float.TryParse(colorNode.SelectSingleNode("b")?.InnerText, out b);
            float.TryParse(colorNode.SelectSingleNode("a")?.InnerText, out a);
            color = new Color(r, g, b, a);
        }
        
        float alcoholContent = 0;
        float.TryParse(ingredientNode.SelectSingleNode("AlcoholContent")?.InnerText, out alcoholContent);
        
        int order = 0;
        DrinkAction action = DrinkAction.None;
        
        XmlNode stepNode = ingredientNode.SelectSingleNode("Step");
        if (stepNode != null)
        {
            int.TryParse(stepNode.SelectSingleNode("Order")?.InnerText, out order);
            System.Enum.TryParse(stepNode.SelectSingleNode("Action")?.InnerText, out action);
        }
        
        IngredientBase ingredient = new IngredientBase(name, amount, type, color, alcoholContent, order, action, solid);
        
        XmlNodeList subIngredientNodes = ingredientNode.SelectNodes("Ingredients/Ingredient");
        if (subIngredientNodes != null && subIngredientNodes.Count > 0)
        {
            foreach (XmlNode subIngredientNode in subIngredientNodes)
            {
                IngredientBase subIngredient = ParseIngredient(subIngredientNode);
                ingredient.ingredients[subIngredient.Name] = subIngredient;
            }
        }
        
        return ingredient;
    }

    // Existing methods
    public CocktailRecipe getRecipe(string RecipeId, int phaseIndex){
        if (phaseIndex < 0 || phaseIndex >= phases.Length)
        {
            Debug.LogError("Invalid phase index: " + phaseIndex);
            return  null;
        }
        PhaseOrder phase = phases[phaseIndex];
        if (phase != null && phase.recipes.ContainsKey(RecipeId))
        {
            return phase.recipes[RecipeId];
        }
        else
        {
            Debug.Log("Recipe not found in phase " + phaseIndex + ": " + RecipeId);
            return null;
        }
    }

    public CocktailRecipe getRecipe(string RecipeId){
        return getRecipe(RecipeId, currentPhaseIndex);
    }

    public SerializedDictionary<string, CocktailRecipe> getPhasRecipes(){
        return phases[currentPhaseIndex].recipes;
    }

    public bool updatePhaseIndex(){
        currentPhaseIndex++;
        GameManager.Instance.onGamePhaseChange.Invoke();
        if(currentPhaseIndex >= phases.Length ){
            currentPhaseIndex = phases.Length - 1;
            return false; 
        }
        return true;
    }
}