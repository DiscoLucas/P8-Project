using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Ingridence
{
    [Serializable]
    public class DrinkStep
    {
        public int order; 
        public DrinkAction action;
        public DrinkStep(int order,DrinkAction action = DrinkAction.None) {
            this.order = order;
            this.action = action;
        }
    }

    [Serializable]
    public enum DrinkAction { 
        Shaked,
        Stirred,
        None
    }
}