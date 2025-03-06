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
        public float startPoint;
        public Transform location;

        public Order(string recipieId, string orderID, Transform location) { 
            recipieID = recipieId;
            this.orderID = orderID;
            this.location = location;
            startPoint = Time.timeSinceLevelLoad;
        }
    }
}