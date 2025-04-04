using Assets.Scripts.Ingridence;
using UnityEngine;

[CreateAssetMenu(fileName = "Ingredient_ScribtiableObject", menuName = "Ingredients/IngredientScribtiableObject")]
public class IngredientScribtiableObject : ScriptableObject
{
    public IngredientBase ingredientBase;
}
