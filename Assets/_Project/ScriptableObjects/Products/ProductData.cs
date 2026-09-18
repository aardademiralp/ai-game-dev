using UnityEngine;
using System;

namespace GameDevStudio.Products
{
    public enum ProductType
    {
        Game,
        Software,
        Website,
        Application,
        AITool
    }

    /// <summary>
    /// ScriptableObject defining a product template in the Product Development catalog.
    /// Create via: Assets → Create → GameDevStudio → Product Data
    /// </summary>
    [CreateAssetMenu(fileName = "ProductData_", menuName = "GameDevStudio/Product Data")]
    public class ProductData : ScriptableObject
    {
        [Header("Identity")]
        public string      productId   = "PROD_BASIC_AI_GAME";
        public string      productName = "Basic AI Game Generator";
        [TextArea(2, 4)]
        public string      description = "Entry-level procedural 2D game generator utilizing basic neural network models.";
        public ProductType productType = ProductType.Game;

        [Header("Technology & Capability Prerequisites")]
        public string[] requiredCapabilityIds = new[] { "GAME_DEV", "NEURAL_NET" };
        public string[] requiredTechIds       = new[] { "BASIC_GAME_DEV", "NEURAL_NETWORKS" };

        [Header("Infrastructure Requirements & Usage")]
        public int minComputeRequired   = 5;   // Minimum threshold to start
        public int minEnergyRequired    = 2;   // Minimum threshold to start
        public int computeUsage         = 5;   // Active runtime consumption
        public int energyUsage          = 2;   // Active runtime consumption
        public int minEmployeesRequired = 1;

        [Header("Economics & Timing")]
        public int   baseCost            = 2500;
        public float baseDevelopmentDays = 3.0f; // In game-days

        [Header("Quality & AI Influencers")]
        public int                        targetQuality   = 50;
        public GameDevStudio.AI.AIStatType primaryAIStat   = GameDevStudio.AI.AIStatType.Creativity;
        public int                        complexityLevel = 1;

        public string Id => !string.IsNullOrEmpty(productId) ? productId : name;
    }
}
