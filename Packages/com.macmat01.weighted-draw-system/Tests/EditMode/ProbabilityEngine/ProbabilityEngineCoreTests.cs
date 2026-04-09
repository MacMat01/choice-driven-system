using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProbabilityEngine.Core;
using ProbabilityEngine.Interfaces;
using Random = UnityEngine.Random;

namespace Tests.EditMode.ProbabilityEngine
{
    public class ProbabilityEngineCoreTests
    {
        [SetUp]
        public void SetUp()
        {
            Random.InitState(24680);
        }

        [Test]
        public void GetValidChoices_FiltersByConditions_AndSkipsNullItems()
        {
            TestState state = new TestState(5, true);
            List<ProbabilityItem<TestState, string>> items = new List<ProbabilityItem<TestState, string>>
            {
                null,
                CreateItem("always", 1f, "always", null),
                CreateItem("score_ok", 1f, "score_ok", static s => s.Score >= 5),
                CreateItem("score_fail", 1f, "score_fail", static s => s.Score >= 10)
            };

            ProbabilityEngine<TestState, string> engine = new ProbabilityEngine<TestState, string>(items);

            List<ProbabilityItem<TestState, string>> valid = engine.GetValidChoices(state);
            List<string> ids = valid.Select(static item => item.Id).ToList();

            CollectionAssert.AreEquivalent(new[]
            {
                "always",
                "score_ok"
            }, ids);
        }

        [Test]
        public void EvaluateRandom_ReturnsNull_WhenNoValidChoices()
        {
            TestState state = new TestState(0, false);
            ProbabilityEngine<TestState, string> engine = new ProbabilityEngine<TestState, string>(new[]
            {
                CreateItem("blocked", 10f, "blocked", static s => s.Flag)
            });

            ProbabilityItem<TestState, string> selected = engine.EvaluateRandom(state);

            Assert.IsNull(selected);
        }

        [Test]
        public void EvaluateRandom_UsesWeightRatios_ForPositiveWeights()
        {
            TestState state = new TestState(0, true);
            ProbabilityEngine<TestState, string> engine = new ProbabilityEngine<TestState, string>(new[]
            {
                CreateItem("low", 1f, "low", null),
                CreateItem("high", 9f, "high", null)
            });

            int highHits = 0;
            const int draws = 10000;
            for (int i = 0; i < draws; i++)
            {
                ProbabilityItem<TestState, string> selected = engine.EvaluateRandom(state);
                if (selected?.Id == "high")
                {
                    highHits++;
                }
            }

            float highShare = highHits / (float)draws;
            Assert.Greater(highShare, 0.84f);
            Assert.Less(highShare, 0.95f);
        }

        [Test]
        public void EvaluateRandom_FallsBackToUniform_WhenAllWeightsAreNonPositive()
        {
            TestState state = new TestState(0, true);
            ProbabilityEngine<TestState, string> engine = new ProbabilityEngine<TestState, string>(new[]
            {
                CreateItem("a", 0f, "a", null),
                CreateItem("b", -3f, "b", null),
                CreateItem("c", 0f, "c", null)
            });

            Dictionary<string, int> hitCounts = new Dictionary<string, int>
            {
                {
                    "a", 0
                },
                {
                    "b", 0
                },
                {
                    "c", 0
                }
            };

            const int draws = 9000;
            for (int i = 0; i < draws; i++)
            {
                ProbabilityItem<TestState, string> selected = engine.EvaluateRandom(state);
                Assert.IsNotNull(selected);
                hitCounts[selected.Id]++;
            }

            foreach (KeyValuePair<string, int> pair in hitCounts)
            {
                float share = pair.Value / (float)draws;
                Assert.That(share, Is.EqualTo(1f / 3f).Within(0.07f), $"Uniform fallback share mismatch for '{pair.Key}'.");
            }
        }

        private static ProbabilityItem<TestState, string> CreateItem(string id, float weight, string value, Func<TestState, bool> condition)
        {
            return new ProbabilityItem<TestState, string>
            {
                Id = id,
                BaseWeight = weight,
                Value = value,
                Conditions = condition == null
                    ? null
                    : new List<ICondition<TestState>>
                    {
                        new LambdaCondition(condition)
                    }
            };
        }

        private sealed class TestState : IGameState
        {
            public TestState(int score, bool flag)
            {
                Score = score;
                Flag = flag;
            }

            public int Score { get; }
            public bool Flag { get; }
        }

        private sealed class LambdaCondition : ICondition<TestState>
        {
            private readonly Func<TestState, bool> predicate;

            public LambdaCondition(Func<TestState, bool> predicate)
            {
                this.predicate = predicate;
            }

            public bool Evaluate(TestState state)
            {
                return predicate(state);
            }
        }
    }
}
