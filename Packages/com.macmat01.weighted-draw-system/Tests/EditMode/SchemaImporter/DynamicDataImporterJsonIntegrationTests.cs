using System.Collections.Generic;
using NUnit.Framework;
using SchemaImporter.Parsers;
using SchemaImporter.Schema;
using UnityEditor;
using UnityEngine.TestTools;
namespace Tests.EditMode.SchemaImporter
{
    public class DynamicDataImporterJsonIntegrationTests
    {
        private const string jsonAssetPath = "Packages/com.macmat01.weighted-draw-system/Tests/EditMode/Fixtures/DataSchema_JSON_Example1.asset";

        [Test]
        public void ImportFromSchema_ParsesJsonExample1_AllRowsAndFields()
        {
            DataSchemaSO schema = AssetDatabase.LoadAssetAtPath<DataSchemaSO>(jsonAssetPath);
            Assert.IsNotNull(schema, $"Missing schema asset at '{jsonAssetPath}'.");
            Assert.IsTrue(schema.HasSourceDataFile(), "Schema is expected to have a source TextAsset assigned.");
            string sourceAssetPath = AssetDatabase.GetAssetPath(schema.SourceDataFile);
            Assert.That(sourceAssetPath, Does.EndWith(".json").IgnoreCase);

            List<DataRecord> records = DynamicDataImporter.ImportFromSchema(schema);

            Assert.AreEqual(7, records.Count);

            ExpectedRow[] expectedRows =
            {
                new ExpectedRow
                {
                    CardId = 101,
                    CardName = "party_invitation",
                    Category = "Friendship",
                    Character = "Sarah",
                    Question = "Hey, we are throwing a massive house party tonight! You have to come, everyone will be there!",
                    IsUnlocked = true,
                    Weight = 45,
                    LeftAnswer = "I'll be there!",
                    LeftAttribute1 = 10,
                    LeftAttribute2 = -10,
                    LeftAttribute3 = -5,
                    LeftAttribute4 = 0,
                    LeftAttribute5 = -20,
                    LeftFollowUp = "hangover_morning",
                    PreConditionVariable = "Friendship",
                    PreConditionOperator = ">",
                    PreConditionValue = 20f,
                    RightAnswer = "I need to study.",
                    RightAttribute1 = -10,
                    RightAttribute2 = 10,
                    RightAttribute3 = 0,
                    RightAttribute4 = 0,
                    RightAttribute5 = 10,
                    RightFollowUp = "study_session"
                },
                new ExpectedRow
                {
                    CardId = 102,
                    CardName = "hangover_morning",
                    Category = "Accademic Performance",
                    Character = "Professor",
                    Question = "You look exhausted. Did you even prepare for today's pop quiz?",
                    IsUnlocked = false,
                    Weight = 20,
                    LeftAnswer = "Try my best.",
                    LeftAttribute1 = 0,
                    LeftAttribute2 = -20,
                    LeftAttribute3 = 0,
                    LeftAttribute4 = 0,
                    LeftAttribute5 = -5,
                    LeftFollowUp = null,
                    PreConditionVariable = string.Empty,
                    RightAnswer = "Skip class and sleep.",
                    RightAttribute1 = 0,
                    RightAttribute2 = -40,
                    RightAttribute3 = 0,
                    RightAttribute4 = -10,
                    RightAttribute5 = 20,
                    RightFollowUp = "angry_parents"
                },
                new ExpectedRow
                {
                    CardId = 103,
                    CardName = "family_dinner",
                    Category = "Family",
                    Character = "Mom",
                    Question = "We haven't seen you in weeks. Can you please come home for dinner this Sunday?",
                    IsUnlocked = true,
                    Weight = 85,
                    LeftAnswer = "Of course, see you Sunday.",
                    LeftAttribute1 = 0,
                    LeftAttribute2 = 0,
                    LeftAttribute3 = 10,
                    LeftAttribute4 = 20,
                    LeftAttribute5 = 0,
                    LeftFollowUp = null,
                    PreConditionVariable = string.Empty,
                    RightAnswer = "I'm too busy, sorry.",
                    RightAttribute1 = 5,
                    RightAttribute2 = 5,
                    RightAttribute3 = 0,
                    RightAttribute4 = -20,
                    RightAttribute5 = 0,
                    RightFollowUp = null
                },
                new ExpectedRow
                {
                    CardId = 104,
                    CardName = "laptop_broken",
                    Category = "Finance",
                    Character = "Internal",
                    Question = "Your laptop just died completely. You need a new one for your assignments.",
                    IsUnlocked = true,
                    Weight = 15,
                    LeftAnswer = "Buy a cheap replacement.",
                    LeftAttribute1 = 0,
                    LeftAttribute2 = -5,
                    LeftAttribute3 = -20,
                    LeftAttribute4 = 0,
                    LeftAttribute5 = 0,
                    LeftFollowUp = null,
                    PreConditionVariable = string.Empty,
                    RightAnswer = "Ask parents for money.",
                    RightAttribute1 = 0,
                    RightAttribute2 = 0,
                    RightAttribute3 = 0,
                    RightAttribute4 = -10,
                    RightAttribute5 = 0,
                    RightFollowUp = null
                },
                new ExpectedRow
                {
                    CardId = 105,
                    CardName = "study_session",
                    Category = "Accademic Performance",
                    Character = "Library",
                    Question = "The library is quiet and you have 4 hours. How do you spend them?",
                    IsUnlocked = false,
                    Weight = 20,
                    LeftAnswer = "Focus entirely on the exam.",
                    LeftAttribute1 = -5,
                    LeftAttribute2 = 20,
                    LeftAttribute3 = 0,
                    LeftAttribute4 = 0,
                    LeftAttribute5 = -10,
                    LeftFollowUp = null,
                    PreConditionVariable = string.Empty,
                    RightAnswer = "Browse social media.",
                    RightAttribute1 = 5,
                    RightAttribute2 = -10,
                    RightAttribute3 = 0,
                    RightAttribute4 = 0,
                    RightAttribute5 = 0,
                    RightFollowUp = null
                },
                new ExpectedRow
                {
                    CardId = 106,
                    CardName = "weekend_trip",
                    Category = "Friendship",
                    Character = "Alfonso",
                    Question = "A few of us are renting a cabin for the weekend! It'll be expensive, but amazing.",
                    IsUnlocked = true,
                    Weight = 15,
                    LeftAnswer = "Count me in!",
                    LeftAttribute1 = 20,
                    LeftAttribute2 = 0,
                    LeftAttribute3 = -20,
                    LeftAttribute4 = 0,
                    LeftAttribute5 = 0,
                    LeftFollowUp = null,
                    PreConditionVariable = "Finance",
                    PreConditionOperator = ">",
                    PreConditionValue = 40f,
                    RightAnswer = "I can't afford it.",
                    RightAttribute1 = -10,
                    RightAttribute2 = 10,
                    RightAttribute3 = 10,
                    RightAttribute4 = 0,
                    RightAttribute5 = 0,
                    RightFollowUp = null
                },
                new ExpectedRow
                {
                    CardId = 107,
                    CardName = "angry_parents",
                    Category = "Family",
                    Character = "Dad",
                    Question = "Your professor called us about your attendance. What is going on with you?",
                    IsUnlocked = false,
                    Weight = 20,
                    LeftAnswer = "Apologize and promise to do better.",
                    LeftAttribute1 = 0,
                    LeftAttribute2 = 10,
                    LeftAttribute3 = 0,
                    LeftAttribute4 = -10,
                    LeftAttribute5 = 0,
                    LeftFollowUp = null,
                    PreConditionVariable = "Accademic_Performance",
                    PreConditionOperator = "<",
                    PreConditionValue = 30f,
                    RightAnswer = "Get defensive.",
                    RightAttribute1 = 0,
                    RightAttribute2 = -5,
                    RightAttribute3 = 0,
                    RightAttribute4 = -40,
                    RightAttribute5 = -5,
                    RightFollowUp = null
                }
            };

            for (int i = 0; i < expectedRows.Length; i++)
            {
                AssertRecord(records[i], expectedRows[i]);
            }

            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertRecord(DataRecord record, ExpectedRow expected)
        {
            Assert.AreEqual(expected.CardId, record.GetField("Card_ID"));
            Assert.AreEqual(expected.CardName, record.GetField("Card_Name"));
            Assert.AreEqual(expected.Category, record.GetField("Category"));
            Assert.AreEqual(expected.Character, record.GetField("Character"));
            Assert.AreEqual(expected.Question, record.GetField("Question"));
            Assert.AreEqual(expected.IsUnlocked, record.GetField("Is_Unlocked"));
            Assert.AreEqual(expected.Weight, record.GetField("Weight"));

            Assert.AreEqual(expected.LeftAnswer, record.GetField("Left_Answer"));
            Assert.AreEqual(expected.LeftAttribute1, record.GetField("Left_Attribute1"));
            Assert.AreEqual(expected.LeftAttribute2, record.GetField("Left_Attribute2"));
            Assert.AreEqual(expected.LeftAttribute3, record.GetField("Left_Attribute3"));
            Assert.AreEqual(expected.LeftAttribute4, record.GetField("Left_Attribute4"));
            Assert.AreEqual(expected.LeftAttribute5, record.GetField("Left_Attribute5"));
            Assert.AreEqual(expected.LeftFollowUp, record.GetField("Left_FollowUp"));

            List<ParsedCondition> conditions = record.GetField("Pre_Conditions") as List<ParsedCondition>;
            Assert.IsNotNull(conditions);
            if (string.IsNullOrWhiteSpace(expected.PreConditionVariable))
            {
                Assert.AreEqual(0, conditions.Count);
            }
            else
            {
                Assert.AreEqual(1, conditions.Count);
                Assert.AreEqual(expected.PreConditionVariable, conditions[0].VariableName);
                Assert.AreEqual(expected.PreConditionOperator, conditions[0].Operator);
                Assert.That(conditions[0].Value, Is.EqualTo(expected.PreConditionValue).Within(0.001f));
            }

            Assert.AreEqual(expected.RightAnswer, record.GetField("Right_Answer"));
            Assert.AreEqual(expected.RightAttribute1, record.GetField("Right_Attribute1"));
            Assert.AreEqual(expected.RightAttribute2, record.GetField("Right_Attribute2"));
            Assert.AreEqual(expected.RightAttribute3, record.GetField("Right_Attribute3"));
            Assert.AreEqual(expected.RightAttribute4, record.GetField("Right_Attribute4"));
            Assert.AreEqual(expected.RightAttribute5, record.GetField("Right_Attribute5"));
            Assert.AreEqual(expected.RightFollowUp, record.GetField("Right_FollowUp"));
        }

        private sealed class ExpectedRow
        {
            public int CardId { get; set; }
            public string CardName { get; set; }
            public string Category { get; set; }
            public string Character { get; set; }
            public string Question { get; set; }
            public bool IsUnlocked { get; set; }
            public int Weight { get; set; }
            public string LeftAnswer { get; set; }
            public int LeftAttribute1 { get; set; }
            public int LeftAttribute2 { get; set; }
            public int LeftAttribute3 { get; set; }
            public int LeftAttribute4 { get; set; }
            public int LeftAttribute5 { get; set; }
            public string LeftFollowUp { get; set; }
            public string PreConditionVariable { get; set; }
            public string PreConditionOperator { get; set; }
            public float PreConditionValue { get; set; }
            public string RightAnswer { get; set; }
            public int RightAttribute1 { get; set; }
            public int RightAttribute2 { get; set; }
            public int RightAttribute3 { get; set; }
            public int RightAttribute4 { get; set; }
            public int RightAttribute5 { get; set; }
            public string RightFollowUp { get; set; }
        }
    }
}
