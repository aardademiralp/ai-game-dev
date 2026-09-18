using UnityEngine;
using System;
using System.Collections.Generic;
using GameDevStudio.AI;

namespace GameDevStudio.Products
{
    /// <summary>
    /// Singleton. Owns and manages the catalog of ProductData definitions.
    /// Evaluates technology capability unlocks against TechnologyDatabase.
    /// </summary>
    public class ProductDatabase : MonoBehaviour
    {
        public static ProductDatabase Instance { get; private set; }

        [SerializeField] private List<ProductData> customCatalog = new List<ProductData>();

        private readonly List<ProductData> _catalog = new List<ProductData>();
        public IReadOnlyList<ProductData> AllProducts => _catalog;

        public event Action<ProductData> OnProductUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            BuildProductCatalog();
        }

        public ProductData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _catalog.Find(p => p != null && string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public List<ProductData> GetByType(ProductType type)
        {
            return _catalog.FindAll(p => p != null && p.productType == type);
        }

        /// <summary>
        /// Evaluates whether all required technology capability unlocked prerequisites for a product are met.
        /// Uses TechnologyDatabase's existing capability and completed tech system.
        /// </summary>
        public bool IsProductUnlocked(ProductData product)
        {
            if (product == null) return false;
            if (TechnologyDatabase.Instance == null) return false;

            // Check required capability IDs
            if (product.requiredCapabilityIds != null)
            {
                foreach (var capId in product.requiredCapabilityIds)
                {
                    if (string.IsNullOrEmpty(capId)) continue;
                    if (!TechnologyDatabase.Instance.IsCapabilityUnlocked(capId))
                        return false;
                }
            }

            // Check required tech IDs directly
            if (product.requiredTechIds != null)
            {
                foreach (var techId in product.requiredTechIds)
                {
                    if (string.IsNullOrEmpty(techId)) continue;
                    var tech = TechnologyDatabase.Instance.GetById(techId);
                    if (tech == null || !tech.IsCompleted)
                        return false;
                }
            }

            return true;
        }

        private void BuildProductCatalog()
        {
            _catalog.Clear();

            // Include Inspector-assigned ScriptableObjects if any
            foreach (var p in customCatalog)
            {
                if (p != null && !_catalog.Contains(p))
                    _catalog.Add(p);
            }

            // Always ensure standard catalog instances are present:
            AddDefaultIfMissing("PROD_BASIC_AI_GAME", "Basic AI Game Generator",
                "Entry-level 2D procedural game generator using basic neural networks.",
                ProductType.Game,
                new[] { "GAME_DEV", "NEURAL_NET" },
                new[] { "BASIC_GAME_DEV", "NEURAL_NETWORKS" },
                minCompute: 5, minEnergy: 2, computeUsage: 5, energyUsage: 2, minEmployees: 1,
                cost: 2500, devDays: 3.0f, quality: 50, primaryStat: AIStatType.Creativity, complexity: 1);

            AddDefaultIfMissing("PROD_AI_CHATBOT", "AI Customer Assistant",
                "Automated text-based customer support bot.",
                ProductType.AITool,
                new[] { "TEXT_AI", "NEURAL_NET" },
                new[] { "TEXT_GENERATION", "NEURAL_NETWORKS" },
                minCompute: 8, minEnergy: 3, computeUsage: 8, energyUsage: 3, minEmployees: 1,
                cost: 3500, devDays: 4.0f, quality: 55, primaryStat: AIStatType.Reasoning, complexity: 1);

            AddDefaultIfMissing("PROD_WEB_ANALYTICS", "Smart Web Analytics Engine",
                "AI-driven analytical dashboard for web platform metrics.",
                ProductType.Website,
                new[] { "WEB_APP", "MACHINE_LEARNING" },
                new[] { "WEB_APPLICATIONS", "ML_FUNDAMENTALS" },
                minCompute: 6, minEnergy: 2, computeUsage: 6, energyUsage: 2, minEmployees: 1,
                cost: 3000, devDays: 3.5f, quality: 50, primaryStat: AIStatType.Speed, complexity: 1);

            AddDefaultIfMissing("PROD_MOBILE_AI_CAM", "Neural Camera App",
                "Mobile camera app featuring real-time neural filter enhancements.",
                ProductType.Application,
                new[] { "MOBILE_APP", "IMAGE_AI" },
                new[] { "MOBILE_APPLICATIONS", "IMAGE_GENERATION" },
                minCompute: 10, minEnergy: 4, computeUsage: 10, energyUsage: 4, minEmployees: 2,
                cost: 5000, devDays: 5.0f, quality: 65, primaryStat: AIStatType.Quality, complexity: 2);

            AddDefaultIfMissing("PROD_DEV_COPILOT", "Code Auto-Complete Suite",
                "AI coding assistant plugin for IDEs.",
                ProductType.Software,
                new[] { "CODE_AI", "SOFTWARE_DEV" },
                new[] { "CODE_GENERATION", "BASIC_SOFTWARE_DEV" },
                minCompute: 15, minEnergy: 6, computeUsage: 15, energyUsage: 6, minEmployees: 2,
                cost: 7500, devDays: 6.0f, quality: 75, primaryStat: AIStatType.Reliability, complexity: 3);

            Debug.Log($"[ProductDatabase] Catalog initialized with {_catalog.Count} products.");
        }

        private void AddDefaultIfMissing(
            string id, string name, string desc, ProductType type,
            string[] caps, string[] techs,
            int minCompute, int minEnergy, int computeUsage, int energyUsage, int minEmployees,
            int cost, float devDays, int quality, AIStatType primaryStat, int complexity)
        {
            if (_catalog.Exists(p => p != null && p.Id == id)) return;

            var p = ScriptableObject.CreateInstance<ProductData>();
            p.productId             = id;
            p.productName           = name;
            p.description           = desc;
            p.productType           = type;
            p.requiredCapabilityIds = caps;
            p.requiredTechIds       = techs;
            p.minComputeRequired   = minCompute;
            p.minEnergyRequired    = minEnergy;
            p.computeUsage         = computeUsage;
            p.energyUsage          = energyUsage;
            p.minEmployeesRequired = minEmployees;
            p.baseCost              = cost;
            p.baseDevelopmentDays   = devDays;
            p.targetQuality         = quality;
            p.primaryAIStat         = primaryStat;
            p.complexityLevel       = complexity;

            _catalog.Add(p);
        }
    }
}
