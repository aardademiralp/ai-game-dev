using UnityEngine;
using System.Collections.Generic;

namespace GameDevStudio.AI
{
    /// <summary>
    /// Singleton. Owns and initializes every TechnologyData definition.
    /// Also tracks which capabilities are unlocked (for future Product systems).
    /// Call RefreshAvailability() whenever a technology completes or the game starts.
    /// </summary>
    public class TechnologyDatabase : MonoBehaviour
    {
        public static TechnologyDatabase Instance { get; private set; }

        private readonly List<TechnologyData> _allTechs = new List<TechnologyData>();
        private readonly HashSet<string>      _unlockedCapabilities = new HashSet<string>();

        public IReadOnlyList<TechnologyData> AllTechnologies => _allTechs;

        // Events
        public event System.Action<TechnologyData> OnTechnologyCompleted;
        public event System.Action<string>         OnCapabilityUnlocked;  // capabilityId

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildTechnologyTree();
            RefreshAvailability();
        }

        // ── Query helpers ────────────────────────────────────────────────────
        public TechnologyData GetById(string id)
        {
            return _allTechs.Find(t => t.Id == id);
        }

        public List<TechnologyData> GetByCategory(TechCategory cat)
        {
            return _allTechs.FindAll(t => t.Category == cat);
        }

        public bool IsCapabilityUnlocked(string capId) => _unlockedCapabilities.Contains(capId);

        // ── Called by TechnologyResearchManager when research completes ───────
        public void MarkCompleted(TechnologyData tech)
        {
            if (tech == null || tech.IsCompleted) return;
            tech.State = TechState.Completed;

            // Apply effects
            foreach (var effect in tech.Effects)
            {
                if (!string.IsNullOrEmpty(effect.capabilityId))
                {
                    _unlockedCapabilities.Add(effect.capabilityId);
                    Debug.Log($"[TechDB] Capability unlocked: {effect.capabilityId}");
                    OnCapabilityUnlocked?.Invoke(effect.capabilityId);
                }
                if (effect.statBonus.HasValue && AICore.Instance != null)
                {
                    AICore.Instance.ImproveStat(effect.statBonus.Value, effect.statBonusAmount);
                }
                if (effect.computeCapacityBonus > 0 && AICore.Instance != null)
                {
                    AICore.Instance.AddComputeCapacity(effect.computeCapacityBonus);
                }
                if (effect.energyCapacityBonus > 0 && AICore.Instance != null)
                {
                    AICore.Instance.AddEnergyCapacity(effect.energyCapacityBonus);
                }
            }

            Debug.Log($"[TechDB] Technology completed: {tech.Name}");
            OnTechnologyCompleted?.Invoke(tech);

            RefreshAvailability();
        }

        // ── Recalculates Locked / Available states ───────────────────────────
        public void RefreshAvailability()
        {
            foreach (var tech in _allTechs)
            {
                if (tech.IsCompleted || tech.IsResearching) continue;

                bool prereqsMet = true;
                foreach (var prereqId in tech.PrerequisiteIds)
                {
                    var prereq = GetById(prereqId);
                    if (prereq == null || !prereq.IsCompleted)
                    {
                        prereqsMet = false;
                        break;
                    }
                }
                tech.State = prereqsMet ? TechState.Available : TechState.Locked;
            }
        }

        // ── Technology tree definition ────────────────────────────────────────
        private void BuildTechnologyTree()
        {
            _allTechs.Clear();

            // ── CORE TECHNOLOGY ──────────────────────────────────────────────
            Add("BASIC_COMPUTING",
                "Basic Computing",
                "Foundational computing infrastructure for AI development.",
                TechCategory.CoreTechnology,
                researchDays: 1f, cost: 500, compute: 0, energy: 0, minEmp: 1,
                prereqs: new string[0],
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Reliability, 2)
                });

            Add("NEURAL_NETWORKS",
                "Neural Networks",
                "Core architecture that mimics the human brain's structure.",
                TechCategory.CoreTechnology,
                researchDays: 2f, cost: 1000, compute: 5, energy: 2, minEmp: 1,
                prereqs: new[] { "BASIC_COMPUTING" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Learning, 3),
                    TechnologyEffect.Capability("NEURAL_NET")
                });

            Add("ADVANCED_COMPUTING",
                "Advanced Computing",
                "High-performance computing substrate. Increases Compute Capacity.",
                TechCategory.CoreTechnology,
                researchDays: 4f, cost: 2500, compute: 10, energy: 5, minEmp: 2,
                prereqs: new[] { "NEURAL_NETWORKS" },
                effects: new[] {
                    TechnologyEffect.ComputeBoost(20),
                    TechnologyEffect.StatBoost(AIStatType.Reliability, 3)
                });

            // ── INTELLIGENCE ────────────────────────────────────────────────
            Add("BASIC_LOGIC",
                "Basic Logic",
                "Propositional and predicate logic as a foundation for reasoning.",
                TechCategory.Intelligence,
                researchDays: 2f, cost: 750, compute: 3, energy: 1, minEmp: 1,
                prereqs: new[] { "BASIC_COMPUTING" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Reasoning, 3)
                });

            Add("PATTERN_RECOGNITION",
                "Pattern Recognition",
                "Ability to detect patterns and regularities in large datasets.",
                TechCategory.Intelligence,
                researchDays: 3f, cost: 1500, compute: 5, energy: 2, minEmp: 1,
                prereqs: new[] { "BASIC_LOGIC" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Reasoning, 4),
                    TechnologyEffect.StatBoost(AIStatType.Quality, 2)
                });

            Add("PROBLEM_SOLVING",
                "Problem Solving",
                "Structured approach to identifying and resolving complex problems.",
                TechCategory.Intelligence,
                researchDays: 4f, cost: 2000, compute: 5, energy: 2, minEmp: 1,
                prereqs: new[] { "BASIC_LOGIC" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Reasoning, 3),
                    TechnologyEffect.StatBoost(AIStatType.Speed, 2)
                });

            Add("MULTI_STEP_REASONING",
                "Multi-Step Reasoning",
                "Chain-of-thought reasoning across multiple logical steps.",
                TechCategory.Intelligence,
                researchDays: 7f, cost: 5000, compute: 12, energy: 5, minEmp: 2,
                prereqs: new[] { "PATTERN_RECOGNITION", "PROBLEM_SOLVING" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Reasoning, 6),
                    TechnologyEffect.Capability("ADVANCED_REASONING")
                });

            Add("AUTONOMOUS_PLANNING",
                "Autonomous Planning",
                "Self-directed planning and goal decomposition for autonomous agents.",
                TechCategory.Intelligence,
                researchDays: 12f, cost: 10000, compute: 20, energy: 8, minEmp: 3,
                prereqs: new[] { "MULTI_STEP_REASONING" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Reasoning, 8),
                    TechnologyEffect.Capability("AUTONOMOUS_AGENT")
                });

            // ── MACHINE LEARNING ─────────────────────────────────────────────
            Add("ML_FUNDAMENTALS",
                "Machine Learning Fundamentals",
                "Core statistical learning algorithms and training pipelines.",
                TechCategory.MachineLearning,
                researchDays: 3f, cost: 1200, compute: 8, energy: 3, minEmp: 1,
                prereqs: new[] { "NEURAL_NETWORKS" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Learning, 5),
                    TechnologyEffect.Capability("MACHINE_LEARNING")
                });

            Add("SUPERVISED_LEARNING",
                "Supervised Learning",
                "Training models from labeled datasets for classification and regression.",
                TechCategory.MachineLearning,
                researchDays: 4f, cost: 1800, compute: 10, energy: 4, minEmp: 2,
                prereqs: new[] { "ML_FUNDAMENTALS" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Quality, 4),
                    TechnologyEffect.StatBoost(AIStatType.Learning, 3)
                });

            Add("DEEP_LEARNING",
                "Deep Learning",
                "Multi-layer neural architectures enabling advanced feature extraction.",
                TechCategory.MachineLearning,
                researchDays: 7f, cost: 4000, compute: 15, energy: 7, minEmp: 2,
                prereqs: new[] { "SUPERVISED_LEARNING" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Quality, 6),
                    TechnologyEffect.StatBoost(AIStatType.Learning, 5),
                    TechnologyEffect.Capability("DEEP_LEARNING")
                });

            Add("REINFORCEMENT_LEARNING",
                "Reinforcement Learning",
                "Agents that learn through trial, error, and reward signals.",
                TechCategory.MachineLearning,
                researchDays: 8f, cost: 5500, compute: 15, energy: 6, minEmp: 2,
                prereqs: new[] { "ML_FUNDAMENTALS" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Reasoning, 5),
                    TechnologyEffect.StatBoost(AIStatType.Speed, 3),
                    TechnologyEffect.Capability("REINFORCEMENT_LEARNING")
                });

            // ── GENERATIVE AI ────────────────────────────────────────────────
            Add("TEXT_GENERATION",
                "Text Generation",
                "Large language models capable of generating coherent text.",
                TechCategory.GenerativeAI,
                researchDays: 10f, cost: 8000, compute: 25, energy: 10, minEmp: 3,
                prereqs: new[] { "DEEP_LEARNING" },
                effects: new[] {
                    TechnologyEffect.Capability("TEXT_AI"),
                    TechnologyEffect.StatBoost(AIStatType.Creativity, 5)
                });

            Add("IMAGE_GENERATION",
                "Image Generation",
                "Diffusion and GAN-based models for synthetic image creation.",
                TechCategory.GenerativeAI,
                researchDays: 10f, cost: 9000, compute: 30, energy: 12, minEmp: 3,
                prereqs: new[] { "DEEP_LEARNING", "ADVANCED_COMPUTING" },
                effects: new[] {
                    TechnologyEffect.Capability("IMAGE_AI"),
                    TechnologyEffect.StatBoost(AIStatType.Creativity, 5)
                });

            Add("AUDIO_GENERATION",
                "Audio Generation",
                "Neural audio synthesis for music, voice, and sound effects.",
                TechCategory.GenerativeAI,
                researchDays: 8f, cost: 7000, compute: 20, energy: 9, minEmp: 2,
                prereqs: new[] { "DEEP_LEARNING" },
                effects: new[] {
                    TechnologyEffect.Capability("AUDIO_AI"),
                    TechnologyEffect.StatBoost(AIStatType.Creativity, 4)
                });

            Add("CODE_GENERATION",
                "Code Generation",
                "AI systems that write, review, and debug code autonomously.",
                TechCategory.GenerativeAI,
                researchDays: 12f, cost: 12000, compute: 25, energy: 10, minEmp: 3,
                prereqs: new[] { "MULTI_STEP_REASONING", "DEEP_LEARNING" },
                effects: new[] {
                    TechnologyEffect.Capability("CODE_AI"),
                    TechnologyEffect.StatBoost(AIStatType.Speed, 6)
                });

            // ── SOFTWARE ─────────────────────────────────────────────────────
            Add("BASIC_SOFTWARE_DEV",
                "Basic Software Development",
                "Core software engineering principles and development workflows.",
                TechCategory.Software,
                researchDays: 2f, cost: 800, compute: 2, energy: 1, minEmp: 1,
                prereqs: new[] { "BASIC_COMPUTING" },
                effects: new[] {
                    TechnologyEffect.Capability("SOFTWARE_DEV"),
                    TechnologyEffect.StatBoost(AIStatType.Speed, 2)
                });

            Add("WEB_APPLICATIONS",
                "Web Applications",
                "Full-stack web development including frontend and backend systems.",
                TechCategory.Software,
                researchDays: 3f, cost: 1500, compute: 3, energy: 1, minEmp: 1,
                prereqs: new[] { "BASIC_SOFTWARE_DEV" },
                effects: new[] { TechnologyEffect.Capability("WEB_APP") });

            Add("DESKTOP_APPLICATIONS",
                "Desktop Applications",
                "Cross-platform native desktop application development.",
                TechCategory.Software,
                researchDays: 3f, cost: 1500, compute: 3, energy: 1, minEmp: 1,
                prereqs: new[] { "BASIC_SOFTWARE_DEV" },
                effects: new[] { TechnologyEffect.Capability("DESKTOP_APP") });

            Add("MOBILE_APPLICATIONS",
                "Mobile Applications",
                "iOS and Android application development pipelines.",
                TechCategory.Software,
                researchDays: 4f, cost: 2000, compute: 4, energy: 2, minEmp: 2,
                prereqs: new[] { "BASIC_SOFTWARE_DEV" },
                effects: new[] { TechnologyEffect.Capability("MOBILE_APP") });

            Add("AI_POWERED_SOFTWARE",
                "AI-Powered Software",
                "Integration of AI capabilities into production software products.",
                TechCategory.Software,
                researchDays: 8f, cost: 6000, compute: 15, energy: 6, minEmp: 2,
                prereqs: new[] { "ML_FUNDAMENTALS", "BASIC_SOFTWARE_DEV" },
                effects: new[] { TechnologyEffect.Capability("AI_SOFTWARE") });

            // ── GAME DEVELOPMENT ─────────────────────────────────────────────
            Add("BASIC_GAME_DEV",
                "Basic Game Development",
                "Game loops, rendering pipelines, and core engine fundamentals.",
                TechCategory.GameDevelopment,
                researchDays: 2f, cost: 1000, compute: 3, energy: 1, minEmp: 1,
                prereqs: new[] { "BASIC_SOFTWARE_DEV" },
                effects: new[] { TechnologyEffect.Capability("GAME_DEV") });

            Add("GAME_LOGIC_SYSTEMS",
                "Game Logic Systems",
                "State machines, event systems, and complex game rule implementation.",
                TechCategory.GameDevelopment,
                researchDays: 3f, cost: 1500, compute: 4, energy: 2, minEmp: 1,
                prereqs: new[] { "BASIC_GAME_DEV" },
                effects: new[] { TechnologyEffect.Capability("ADVANCED_GAME_DEV") });

            Add("PROCEDURAL_CONTENT",
                "Procedural Content",
                "Algorithmic generation of levels, items, and game world content.",
                TechCategory.GameDevelopment,
                researchDays: 5f, cost: 3000, compute: 8, energy: 3, minEmp: 2,
                prereqs: new[] { "GAME_LOGIC_SYSTEMS" },
                effects: new[] { TechnologyEffect.Capability("PROCEDURAL_GEN") });

            Add("AI_NPC_SYSTEMS",
                "AI NPC Systems",
                "Behaviour trees and learning agents for intelligent game characters.",
                TechCategory.GameDevelopment,
                researchDays: 6f, cost: 4000, compute: 12, energy: 5, minEmp: 2,
                prereqs: new[] { "GAME_LOGIC_SYSTEMS", "ML_FUNDAMENTALS" },
                effects: new[] { TechnologyEffect.Capability("AI_NPC") });

            // ── AI SAFETY ───────────────────────────────────────────────────
            Add("BASIC_AI_SAFETY",
                "Basic AI Safety",
                "Foundational principles for safe and predictable AI behaviour.",
                TechCategory.AISafety,
                researchDays: 2f, cost: 1000, compute: 2, energy: 1, minEmp: 1,
                prereqs: new[] { "BASIC_COMPUTING" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Reliability, 5)
                });

            Add("ERROR_DETECTION",
                "Error Detection",
                "Automated systems to identify and flag model errors and edge cases.",
                TechCategory.AISafety,
                researchDays: 3f, cost: 1500, compute: 4, energy: 2, minEmp: 1,
                prereqs: new[] { "BASIC_AI_SAFETY" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Reliability, 4),
                    TechnologyEffect.StatBoost(AIStatType.Quality, 2)
                });

            Add("MODEL_EVALUATION",
                "Model Evaluation",
                "Rigorous benchmarking and evaluation frameworks for AI systems.",
                TechCategory.AISafety,
                researchDays: 4f, cost: 2500, compute: 5, energy: 2, minEmp: 2,
                prereqs: new[] { "ERROR_DETECTION", "ML_FUNDAMENTALS" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Reliability, 5),
                    TechnologyEffect.StatBoost(AIStatType.Quality, 3)
                });

            Add("ALIGNMENT_RESEARCH",
                "Alignment Research",
                "Ensuring AI goals remain aligned with human values and intentions.",
                TechCategory.AISafety,
                researchDays: 10f, cost: 8000, compute: 15, energy: 6, minEmp: 3,
                prereqs: new[] { "MODEL_EVALUATION", "MULTI_STEP_REASONING" },
                effects: new[] {
                    TechnologyEffect.StatBoost(AIStatType.Reliability, 8),
                    TechnologyEffect.Capability("ALIGNMENT")
                });

            Debug.Log($"[TechDB] Technology tree built: {_allTechs.Count} technologies.");
        }

        private void Add(
            string id, string name, string description,
            TechCategory category,
            float researchDays, int cost, int compute, int energy, int minEmp,
            string[] prereqs, TechnologyEffect[] effects)
        {
            _allTechs.Add(new TechnologyData(
                id, name, description, category,
                researchDays, cost, compute, energy, minEmp,
                prereqs, effects));
        }
    }
}
