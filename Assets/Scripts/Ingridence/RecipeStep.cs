using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Ingridence
{
    public class RecipeStep
    {
        public int order; 
        public DrinkAction action;
        public RecipeStep(int order,DrinkAction action = DrinkAction.None) {
            this.order = order;
            this.action = action;
        }
    }

    public enum DrinkAction { 
        Shaked,
        Stirred,
        None
    }
}