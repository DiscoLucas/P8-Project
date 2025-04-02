using AYellowpaper.SerializedCollections;
using UnityEngine;

public class PhaseManager : MonoBehaviour
{
    [SerializeField]
    PhaseOrder[] phases;
    [SerializeField]
    int currentPhaseIndex = 0;


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
        if(currentPhaseIndex >= phases.Length ){
            currentPhaseIndex = phases.Length - 1;
            return false; 
        }
        return true;
    }

}