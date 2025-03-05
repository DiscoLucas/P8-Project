using Assets.Scripts.Drink_interaction;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Orders
{
    /// <summary>
    /// Class containing the order
    /// </summary>
    [Serializable]
    public class Order
    {
        public string recipieID;
        public string orderID;
        public LiquidContainerLimited containerLimited;
        public bool isFinnished = false;

        public Order(string recipieId, string orderID ) { 
            recipieID = recipieId;
            this.orderID = orderID;
        }
    }
}