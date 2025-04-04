using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.IO;
using System.Linq;
using Assets.Scripts.Ingridence;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Recipe_Xml_Updater : MonoBehaviour
{
    [SerializeField]
    private TextAsset recipesXmlFile;

    [SerializeField]
    public List<PhaseOrderTemplate> phaseOrderTemplates = new List<PhaseOrderTemplate>();
    
    // Internal-only cache for processing
    private Dictionary<string, IngredientScribtiableObject> uniqueIngredients = new Dictionary<string, IngredientScribtiableObject>();

    #if UNITY_EDITOR
    [ContextMenu("Generate Ingredient ScriptableObjects from XML")]
    public void GenerateIngredientScriptableObjects()
    {
        if (recipesXmlFile == null)
        {
            Debug.LogError("No XML file assigned! Please assign a TextAsset containing recipe XML data.");
            return;
        }
        
        // Clear previous results
        uniqueIngredients.Clear();
        
        // Ensure the target directory exists
        string directoryPath = "Assets/Recipes/Ingredients";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            AssetDatabase.Refresh();
        }
        
        // Parse the XML and extract ingredients
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(recipesXmlFile.text);
        
        // Find all ingredient nodes across all recipes
        XmlNodeList ingredientNodes = xmlDoc.SelectNodes("//Ingredient");
        Debug.Log($"Found {ingredientNodes.Count} ingredient entries in the XML file.");
        
        // Process each ingredient
        foreach (XmlNode ingredientNode in ingredientNodes)
        {
            ProcessIngredient(ingredientNode, directoryPath);
        }
        
        // Refresh the AssetDatabase to show the new assets
        AssetDatabase.Refresh();
        
        Debug.Log($"Successfully created {uniqueIngredients.Count} unique ingredient scriptable objects in {directoryPath}");
    }
    
    private void ProcessIngredient(XmlNode ingredientNode, string directoryPath)
    {
        string name = ingredientNode.SelectSingleNode("Name")?.InnerText;
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("Found ingredient without a name, skipping.");
            return;
        }
        
        // Skip if we already processed this ingredient
        if (uniqueIngredients.ContainsKey(name))
        {
            return;
        }
        
        // Parse the ingredient properties
        bool solid = false;
        bool.TryParse(ingredientNode.SelectSingleNode("Solid")?.InnerText, out solid);
        
        IngredientType type = IngredientType.Other;
        string typeStr = ingredientNode.SelectSingleNode("Type")?.InnerText;
        if (!string.IsNullOrEmpty(typeStr))
        {
            System.Enum.TryParse(typeStr, out type);
        }
        
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
        
        // Get Step information
        int order = 0;
        DrinkAction action = DrinkAction.None;
        XmlNode stepNode = ingredientNode.SelectSingleNode("Step");
        if (stepNode != null)
        {
            int.TryParse(stepNode.SelectSingleNode("Order")?.InnerText, out order);
            System.Enum.TryParse(stepNode.SelectSingleNode("Action")?.InnerText, out action);
        }
        
        // Create the ScriptableObject asset
        IngredientScribtiableObject ingredientSO = ScriptableObject.CreateInstance<IngredientScribtiableObject>();
        
        // Set the name of the scriptable object
        ingredientSO.name = name;
        
        // Create and set up the IngredientBase
        ingredientSO.ingredientBase = new IngredientBase(
            name,
            0, // Default amount (will be set when used in a recipe)
            type,
            color,
            alcoholContent,
            order,
            action,
            solid
        );
        
        // Format filename to be safe for file system
        string safeFileName = name.Replace(" ", "_").Replace("&", "And");
        safeFileName = string.Join("_", safeFileName.Split(Path.GetInvalidFileNameChars()));
        
        // Save the asset
        string assetPath = $"{directoryPath}/{safeFileName}.asset";
        AssetDatabase.CreateAsset(ingredientSO, assetPath);
        
        // Add to our dictionary of processed ingredients
        uniqueIngredients[name] = ingredientSO;
        
        Debug.Log($"Created ingredient: {name} at {assetPath}");
    }
    
    [ContextMenu("Load Recipes into Templates")]
    public void LoadRecipesIntoTemplates()
    {
        if (recipesXmlFile == null)
        {
            Debug.LogError("No XML file assigned! Please assign a TextAsset containing recipe XML data.");
            return;
        }
        
        // Generate/load ingredients first to ensure we have all ingredients available
        GenerateIngredientScriptableObjects();
        
        // Clear existing templates
        phaseOrderTemplates.Clear();
        
        // Load XML content
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(recipesXmlFile.text);
        
        // Process each phase
        XmlNodeList phaseNodes = xmlDoc.SelectNodes("//CocktailRecipes/Phases/Phase");
        foreach (XmlNode phaseNode in phaseNodes)
        {
            // Create a new template for this phase
            PhaseOrderTemplate template = new PhaseOrderTemplate();
            
            // Set the name of the phase
            string phaseName = phaseNode.Attributes["name"]?.Value;
            template.Name = !string.IsNullOrEmpty(phaseName) ? phaseName : "Unnamed Phase";
            
            // Process each recipe in this phase
            XmlNodeList recipeNodes = phaseNode.SelectNodes("Recipe");
            foreach (XmlNode recipeNode in recipeNodes)
            {
                ProcessRecipeForTemplate(recipeNode, template);
            }
            
            // Add the completed template to the list
            phaseOrderTemplates.Add(template);
        }
        
        Debug.Log($"Successfully loaded {phaseOrderTemplates.Count} phase templates with recipes from XML.");
        EditorUtility.SetDirty(this);
    }
    
    private void ProcessRecipeForTemplate(XmlNode recipeNode, PhaseOrderTemplate template)
    {
        string recipeName = recipeNode.SelectSingleNode("Name")?.InnerText ?? "Unnamed Recipe";
        string recipeId = recipeNode.Attributes["id"]?.Value;
        
        // Create a new recipe template entry
        RecipeTemplate recipeTemplate = new RecipeTemplate();
        recipeTemplate.Name = recipeName;
        recipeTemplate.Id = recipeId;
        
        // Add difficulty, score, etc. if your RecipeTemplate class has these properties
        int difficulty;
        if (int.TryParse(recipeNode.SelectSingleNode("Difficulty")?.InnerText, out difficulty))
        {
            recipeTemplate.Difficulty = difficulty;
        }
        
        float maxScore;
        if (float.TryParse(recipeNode.SelectSingleNode("MaxScore")?.InnerText, out maxScore))
        {
            recipeTemplate.MaxScore = maxScore;
        }
        
        float expectedTime;
        if (float.TryParse(recipeNode.SelectSingleNode("ExpectedTime")?.InnerText, out expectedTime))
        {
            recipeTemplate.ExpectedTime = expectedTime;
        }
        
        string glassTypeStr = recipeNode.SelectSingleNode("GlassType")?.InnerText;
        if (!string.IsNullOrEmpty(glassTypeStr) && System.Enum.TryParse(glassTypeStr, out GlassType glassType))
        {
            recipeTemplate.GlassType = glassType;
        }
        
        // Process all ingredients in this recipe
        XmlNodeList ingredientNodes = recipeNode.SelectNodes("Ingredients/Ingredient");
        foreach (XmlNode ingredientNode in ingredientNodes)
        {
            string ingredientName = ingredientNode.SelectSingleNode("Name")?.InnerText;
            if (string.IsNullOrEmpty(ingredientName))
            {
                Debug.LogWarning($"Found ingredient without a name in recipe {recipeName}, skipping.");
                continue;
            }
            
            // Get or find the scriptable object for this ingredient
            IngredientScribtiableObject ingredientSO = GetIngredientScriptableObject(ingredientName);
            if (ingredientSO == null)
            {
                Debug.LogWarning($"Could not find ingredient scriptable object for '{ingredientName}' in recipe {recipeName}.");
                continue;
            }
            
            // Parse ingredient properties for the template item
            float amount = 0;
            float.TryParse(ingredientNode.SelectSingleNode("Amount")?.InnerText, out amount);
            
            // Get step information
            int order = 0;
            DrinkAction action = DrinkAction.None;
            XmlNode stepNode = ingredientNode.SelectSingleNode("Step");
            if (stepNode != null)
            {
                int.TryParse(stepNode.SelectSingleNode("Order")?.InnerText, out order);
                System.Enum.TryParse(stepNode.SelectSingleNode("Action")?.InnerText, out action);
            }
            
            // Create the ingredient item for the recipe
            PhaseOrderTemplateItem item = new PhaseOrderTemplateItem(ingredientSO, amount, action, order);
            
            // Add it to the recipe
            recipeTemplate.AddIngredient(item);
        }
        
        // Add the recipe to the phase template
        template.AddRecipe(recipeTemplate);
    }
    
    private IngredientScribtiableObject GetIngredientScriptableObject(string ingredientName)
    {
        // Check if we already have this ingredient in our dictionary
        if (uniqueIngredients.TryGetValue(ingredientName, out IngredientScribtiableObject ingredient))
        {
            return ingredient;
        }
        
        // Try to find it in the project assets
        string safeFileName = ingredientName.Replace(" ", "_").Replace("&", "And");
        safeFileName = string.Join("_", safeFileName.Split(Path.GetInvalidFileNameChars()));
        
        string assetPath = $"Assets/Recipes/Ingredients/{safeFileName}.asset";
        IngredientScribtiableObject loadedIngredient = AssetDatabase.LoadAssetAtPath<IngredientScribtiableObject>(assetPath);
        
        if (loadedIngredient != null)
        {
            // Cache it for future use
            uniqueIngredients[ingredientName] = loadedIngredient;
            return loadedIngredient;
        }
        
        Debug.LogWarning($"Could not find ingredient scriptable object for '{ingredientName}'. Run 'Generate Ingredient ScriptableObjects from XML' first.");
        return null;
    }
    private void AddXmlElement(XmlDocument doc, XmlElement parent, string name, string value)
{
    XmlElement element = doc.CreateElement(name);
    element.InnerText = value;
    parent.AppendChild(element);
}
    [ContextMenu("Save Templates to XML")]
    public void SaveTemplatesToXml()
    {
        if (phaseOrderTemplates == null || phaseOrderTemplates.Count == 0)
        {
            Debug.LogError("No templates to save! Make sure phaseOrderTemplates list is populated.");
            return;
        }
        
        XmlDocument xmlDoc = new XmlDocument();
        
        // Create the document and root element
        XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
        xmlDoc.AppendChild(xmlDeclaration);
        
        XmlElement rootElement = xmlDoc.CreateElement("CocktailRecipes");
        xmlDoc.AppendChild(rootElement);
        
        XmlElement phasesElement = xmlDoc.CreateElement("Phases");
        rootElement.AppendChild(phasesElement);
        
        // Add each phase template
        foreach (PhaseOrderTemplate phaseTemplate in phaseOrderTemplates)
        {
            XmlElement phaseElement = xmlDoc.CreateElement("Phase");
            phaseElement.SetAttribute("name", phaseTemplate.Name);
            phasesElement.AppendChild(phaseElement);
            
            // Add recipes for this phase
            foreach (RecipeTemplate recipeTemplate in phaseTemplate.Recipes)
            {
                XmlElement recipeElement = xmlDoc.CreateElement("Recipe");
                recipeElement.SetAttribute("id", recipeTemplate.Id);
                phaseElement.AppendChild(recipeElement);
                
                // Add recipe details
                AddXmlElement(xmlDoc, recipeElement, "Name", recipeTemplate.Name);
                AddXmlElement(xmlDoc, recipeElement, "Difficulty", recipeTemplate.Difficulty.ToString());
                AddXmlElement(xmlDoc, recipeElement, "MaxScore", recipeTemplate.MaxScore.ToString());
                AddXmlElement(xmlDoc, recipeElement, "ExpectedTime", recipeTemplate.ExpectedTime.ToString());
                AddXmlElement(xmlDoc, recipeElement, "GlassType", recipeTemplate.GlassType.ToString());
                
                // Add ingredients container
                XmlElement ingredientsElement = xmlDoc.CreateElement("Ingredients");
                recipeElement.AppendChild(ingredientsElement);
                
                // Add each ingredient
                foreach (PhaseOrderTemplateItem item in recipeTemplate.OrderedIngredients)
                {
                    XmlElement ingredientElement = xmlDoc.CreateElement("Ingredient");
                    ingredientsElement.AppendChild(ingredientElement);
                    
                    if (item.ingredient != null && item.ingredient.ingredientBase != null)
                    {
                        // Basic ingredient properties
                        AddXmlElement(xmlDoc, ingredientElement, "Name", item.ingredient.name);
                        AddXmlElement(xmlDoc, ingredientElement, "Amount", item.amount.ToString());
                        AddXmlElement(xmlDoc, ingredientElement, "Solid", item.ingredient.ingredientBase.solid.ToString().ToLower());
                        AddXmlElement(xmlDoc, ingredientElement, "Type", item.ingredient.ingredientBase.Type.ToString());
                        
                        // Color
                        XmlElement colorElement = xmlDoc.CreateElement("Color");
                        ingredientElement.AppendChild(colorElement);
                        AddXmlElement(xmlDoc, colorElement, "r", item.ingredient.ingredientBase.Color.r.ToString());
                        AddXmlElement(xmlDoc, colorElement, "g", item.ingredient.ingredientBase.Color.g.ToString());
                        AddXmlElement(xmlDoc, colorElement, "b", item.ingredient.ingredientBase.Color.b.ToString());
                        AddXmlElement(xmlDoc, colorElement, "a", item.ingredient.ingredientBase.Color.a.ToString());
                        
                        // Alcohol content
                        AddXmlElement(xmlDoc, ingredientElement, "AlcoholContent", item.ingredient.ingredientBase.AlcoholContent.ToString());
                        
                        // Step information
                        XmlElement stepElement = xmlDoc.CreateElement("Step");
                        ingredientElement.AppendChild(stepElement);
                        AddXmlElement(xmlDoc, stepElement, "Order", item.order.ToString());
                        AddXmlElement(xmlDoc, stepElement, "Action", item.action.ToString());
                    }
                    else
                    {
                        Debug.LogWarning($"Skipping ingredient in recipe {recipeTemplate.Name} that has null references");
                    }
                }
            }
        }
        
        string filePath;
        
        // If we already have a recipesXmlFile, save directly to its path
        if (recipesXmlFile != null)
        {
            filePath = AssetDatabase.GetAssetPath(recipesXmlFile);
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("Could not determine the path of the existing XML file.");
                return;
            }
        }
        else
        {
            // If no recipesXmlFile is assigned, prompt for location
            filePath = EditorUtility.SaveFilePanel("Save Recipe XML", "Assets", "CocktailRecipes.xml", "xml");
            if (string.IsNullOrEmpty(filePath))
            {
                return; // User canceled the save dialog
            }
            
            // Convert to a project-relative path if outside Assets folder
            if (!filePath.StartsWith(Application.dataPath))
            {
                Debug.LogWarning("The file was saved outside the Assets folder. Creating a copy in Assets/Recipes.");
                
                // Ensure directory exists
                string directoryPath = "Assets/Recipes";
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                
                string fileName = Path.GetFileName(filePath);
                filePath = Path.Combine(directoryPath, fileName);
                
                // Make sure filePath uses forward slashes
                filePath = filePath.Replace('\\', '/');
            }
            else
            {
                // Convert to project-relative path
                filePath = "Assets" + filePath.Substring(Application.dataPath.Length);
            }
        }
        
        // Ensure folder exists
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // Save the file
        xmlDoc.Save(filePath);
        
        // Refresh asset database to see changes
        AssetDatabase.ImportAsset(filePath);
        
        // Update the reference
        recipesXmlFile = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
        
        Debug.Log($"Successfully saved recipes to {filePath}");
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
    
    [ContextMenu("Update All Phases From XML")]
    public void UpdatePhasesFromXml()
    {
        if (recipesXmlFile == null)
        {
            Debug.LogError("No XML file assigned! Please assign a TextAsset containing recipe XML data.");
            return;
        }
        
        // This now just calls our LoadRecipesIntoTemplates method
        LoadRecipesIntoTemplates();
        EditorUtility.SetDirty(this);
    }
    #endif
    
    // The rest of the helper methods (ParseRecipe, ParseIngredient) remain as they are
    // since they don't reference phaseOrders and are still needed
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
}

// Add these supporting classes if you don't already have them
[System.Serializable]
public class RecipeTemplate
{
    public string Name;
    public string Id;
    public int Difficulty = 1;
    public float MaxScore = 100;
    public float ExpectedTime = 30;
    public GlassType GlassType;
    
    // List of ingredients for this recipe
    public List<PhaseOrderTemplateItem> Ingredients = new List<PhaseOrderTemplateItem>();
    
    // Returns ingredients sorted by order
    public List<PhaseOrderTemplateItem> OrderedIngredients 
    { 
        get { return Ingredients.OrderBy(i => i.order).ToList(); } 
    }
    
    public void AddIngredient(PhaseOrderTemplateItem item)
    {
        Ingredients.Add(item);
    }
}

[System.Serializable]
public class PhaseOrderTemplate
{
    public string Name;
    public List<RecipeTemplate> Recipes = new List<RecipeTemplate>();
    
    public void AddRecipe(RecipeTemplate recipe)
    {
        Recipes.Add(recipe);
    }
}

[System.Serializable]
public class PhaseOrderTemplateItem
{
    public IngredientScribtiableObject ingredient;
    public float amount;
    public DrinkAction action;
    public int order;
    
    public PhaseOrderTemplateItem(IngredientScribtiableObject ingredient, float amount, DrinkAction action, int order)
    {
        this.ingredient = ingredient;
        this.amount = amount;
        this.action = action;
        this.order = order;
    }
}