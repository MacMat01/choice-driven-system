using System;
using System.Collections.Generic;
using NUnit.Framework;
using WeightedDraw;
namespace Tests.EditMode
{
    public class WeightedDrawEngineTests
    {
        [Test]
        public void GetValidEntries_WhenSourceIsNull_ReturnsEmpty()
        {
            WeightedDrawEngine<TestEntry, int> engine = new WeightedDrawEngine<TestEntry, int>(
                static (entry, threshold) => entry.Weight >= threshold,
                static entry => entry.Weight);

            IReadOnlyList<TestEntry> valid = engine.GetValidEntries(null, 5);

            Assert.AreEqual(0, valid.Count);
        }

        [Test]
        public void GetValidEntries_FiltersByEligibilityPredicate()
        {
            List<TestEntry> source = new List<TestEntry>
            {
                new TestEntry("A", 5),
                new TestEntry("B", 10),
                new TestEntry("C", 15)
            };

            WeightedDrawEngine<TestEntry, int> engine = new WeightedDrawEngine<TestEntry, int>(
                static (entry, threshold) => entry.Weight >= threshold,
                static entry => entry.Weight);

            IReadOnlyList<TestEntry> valid = engine.GetValidEntries(source, 10);

            Assert.AreEqual(2, valid.Count);
            Assert.AreEqual("B", valid[0].Id);
            Assert.AreEqual("C", valid[1].Id);
        }

        [Test]
        public void Draw_WhenNoEntriesAreEligible_ReturnsDefault()
        {
            List<TestEntry> source = new List<TestEntry>
            {
                new TestEntry("A", 5)
            };

            RecordingRandomProvider random = new RecordingRandomProvider();
            WeightedDrawEngine<TestEntry, int> engine = new WeightedDrawEngine<TestEntry, int>(
                static (_, _) => false,
                static entry => entry.Weight,
                random);

            TestEntry selected = engine.Draw(source, 0);

            Assert.IsNull(selected);
            Assert.IsFalse(random.NextFloatCalled);
            Assert.IsFalse(random.NextIntCalled);
        }

        [Test]
        public void Draw_WhenTotalWeightIsZeroOrLess_UsesUniformSelection()
        {
            List<TestEntry> source = new List<TestEntry>
            {
                new TestEntry("A", 0),
                new TestEntry("B", -2),
                new TestEntry("C", 0)
            };

            RecordingRandomProvider random = new RecordingRandomProvider
            {
                NextIntValue = 2
            };

            WeightedDrawEngine<TestEntry, object> engine = new WeightedDrawEngine<TestEntry, object>(
                static (_, _) => true,
                static entry => entry.Weight,
                random);

            TestEntry selected = engine.Draw(source, null);

            Assert.AreEqual("C", selected.Id);
            Assert.IsFalse(random.NextFloatCalled);
            Assert.IsTrue(random.NextIntCalled);
        }

        [Test]
        public void Draw_UsesWeightedSelectionAcrossBoundaryTargets()
        {
            List<TestEntry> source = new List<TestEntry>
            {
                new TestEntry("A", 10),
                new TestEntry("B", 20),
                new TestEntry("C", 30)
            };

            WeightedDrawEngine<TestEntry, object> engineA = CreateEngineWithFloatTarget(0f);
            WeightedDrawEngine<TestEntry, object> engineB1 = CreateEngineWithFloatTarget(10f);
            WeightedDrawEngine<TestEntry, object> engineB2 = CreateEngineWithFloatTarget(29.99f);
            WeightedDrawEngine<TestEntry, object> engineC = CreateEngineWithFloatTarget(59.99f);

            Assert.AreEqual("A", engineA.Draw(source, null).Id);
            Assert.AreEqual("A", engineB1.Draw(source, null).Id);
            Assert.AreEqual("B", engineB2.Draw(source, null).Id);
            Assert.AreEqual("C", engineC.Draw(source, null).Id);
        }

        [Test]
        public void Draw_WhenRandomTargetExceedsTotalWeight_ReturnsLastValidEntry()
        {
            List<TestEntry> source = new List<TestEntry>
            {
                new TestEntry("A", 1),
                new TestEntry("B", 1),
                new TestEntry("C", 1)
            };

            WeightedDrawEngine<TestEntry, object> engine = CreateEngineWithFloatTarget(999f);

            TestEntry selected = engine.Draw(source, null);

            Assert.AreEqual("C", selected.Id);
        }

        private static WeightedDrawEngine<TestEntry, object> CreateEngineWithFloatTarget(float target)
        {
            RecordingRandomProvider random = new RecordingRandomProvider
            {
                NextFloatValue = target
            };

            return new WeightedDrawEngine<TestEntry, object>(
                static (_, _) => true,
                static entry => entry.Weight,
                random);
        }

        private sealed class TestEntry
        {
            public TestEntry(string id, float weight)
            {
                Id = id;
                Weight = weight;
            }

            public string Id { get; }
            public float Weight { get; }
        }

        private sealed class RecordingRandomProvider : IRandomValueProvider
        {
            public float NextFloatValue { get; set; }
            public int NextIntValue { get; set; }
            public bool NextFloatCalled { get; private set; }
            public bool NextIntCalled { get; private set; }

            public float NextFloat(float minInclusive, float maxExclusive)
            {
                NextFloatCalled = true;
                return NextFloatValue;
            }

            public int NextInt(int minInclusive, int maxExclusive)
            {
                NextIntCalled = true;
                return Math.Max(minInclusive, Math.Min(NextIntValue, maxExclusive - 1));
            }
        }
    }
}
