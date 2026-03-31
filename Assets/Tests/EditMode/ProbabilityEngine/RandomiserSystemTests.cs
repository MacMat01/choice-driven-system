using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using ProbabilityEngine.Core;
using SchemaImporter.Parsers;
using SchemaImporter.Schema;
using UnityEditor;
using UnityEngine;
namespace Tests.EditMode.ProbabilityEngine
{
    public class RandomiserSystemTests
    {
        private const string csvAssetPath = "Assets/Scripts/SchemaImporter/Schema/ExamplesData/DataSchema_CSV_Example1.asset";
        private const string jsonAssetPath = "Assets/Scripts/SchemaImporter/Schema/ExamplesData/DataSchema_JSON_Example1.asset";

        [SetUp]
        public void SetUp()
        {
            Random.InitState(1337);
        }

        [Test]
        public void EvaluateRandom_UsesWeightColumnFlag_WithCsvAssetPath()
        {
            AssertWeightedSelectionFromSchema(csvAssetPath);
        }

        [Test]
        public void EvaluateRandom_UsesWeightColumnFlag_WithJsonAssetPath()
        {
            AssertWeightedSelectionFromSchema(jsonAssetPath);
        }

        [Test]
        public void GetValidChoices_AutoResolvesConditionAndWeightColumns_FromSchemaFlags()
        {
            DataSchemaSO schema = CreateSchema("SpawnConditions", "Weight");
            DataRecord pass = CreateRecord(1, 10, BuildConditions(new ParsedCondition("score", ">=", 5f)));
            DataRecord fail = CreateRecord(2, 50, BuildConditions(new ParsedCondition("score", ">=", 10f)));

            RandomiserSystem system = new RandomiserSystem(new[]
            {
                pass,
                fail
            }, schema);

            List<DataRecord> valid = system.GetValidChoices(new Dictionary<string, object>
            {
                {
                    "score", 7f
                }
            });

            Assert.AreEqual(1, valid.Count);
            Assert.AreEqual(1, valid[0].GetField("Card_ID"));
            Assert.AreEqual(1, system.EvaluateRandom(new Dictionary<string, object>
            {
                {
                    "score", 7f
                }
            })?.GetField("Card_ID"));
        }

        [Test]
        public void Constructor_ExplicitColumnNames_OverrideSchemaFlags()
        {
            DataSchemaSO schema = CreateSchema("FlaggedConds", "FlaggedWeight");
            DataRecord record = new DataRecord();
            record.SetField("Card_ID", 11);
            record.SetField("CustomConds", BuildConditions(new ParsedCondition("level", ">", 0f)));
            record.SetField("CustomWeight", 3);

            RandomiserSystem system = new RandomiserSystem(
                new[]
                {
                    record
                },
                schema,
                "CustomConds",
                "CustomWeight");

            DataRecord selected = system.EvaluateRandom(new Dictionary<string, object>
            {
                {
                    "level", 1f
                }
            });

            Assert.IsNotNull(selected);
            Assert.AreEqual(11, selected.GetField("Card_ID"));
        }

        [Test]
        public void GetValidChoices_MalformedConditionPayload_MakesRowUnselectable()
        {
            DataSchemaSO schema = CreateSchema("SpawnConditions", "Weight");
            DataRecord malformed = new DataRecord();
            malformed.SetField("Card_ID", 20);
            malformed.SetField("Weight", 100);
            malformed.SetField("SpawnConditions", "not-a-condition-list");

            DataRecord valid = CreateRecord(21, 1, null);

            RandomiserSystem system = new RandomiserSystem(new[]
            {
                malformed,
                valid
            }, schema);

            List<DataRecord> choices = system.GetValidChoices(new Dictionary<string, object>());
            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual(21, choices[0].GetField("Card_ID"));
        }

        [Test]
        public void GetValidChoices_UnknownConnector_DefaultsToAnd()
        {
            DataSchemaSO schema = CreateSchema("SpawnConditions", "Weight");
            DataRecord unknownConnector = CreateRecord(30, 1, BuildConditions(
                new ParsedCondition("score", ">=", 1f),
                new ParsedCondition("luck", "==", 1f, "XOR")));

            DataRecord orConnector = CreateRecord(31, 1, BuildConditions(
                new ParsedCondition("score", ">=", 1f),
                new ParsedCondition("luck", "==", 1f, "OR")));

            RandomiserSystem system = new RandomiserSystem(new[]
            {
                unknownConnector,
                orConnector
            }, schema);

            List<DataRecord> valid = system.GetValidChoices(new Dictionary<string, object>
            {
                {
                    "score", 1f
                },
                {
                    "luck", 0f
                }
            });

            Assert.AreEqual(1, valid.Count);
            Assert.AreEqual(31, valid[0].GetField("Card_ID"));
        }

        [Test]
        public void EvaluateRandom_OnlyPositiveWeightsCanBeDrawn_WhenAnyPositiveExists()
        {
            DataSchemaSO schema = CreateSchema("SpawnConditions", "Weight");
            DataRecord positive = CreateRecord(40, "2", null);
            DataRecord invalid = CreateRecord(41, "invalid", null);
            DataRecord nonPositive = CreateRecord(42, 0, null);

            RandomiserSystem system = new RandomiserSystem(new[]
            {
                positive,
                invalid,
                nonPositive
            }, schema);

            for (int i = 0; i < 400; i++)
            {
                DataRecord selected = system.EvaluateRandom(new Dictionary<string, object>());
                Assert.AreEqual(40, selected?.GetField("Card_ID"));
            }
        }

        [Test]
        public void EvaluateRandom_FallsBackToUniform_WhenAllWeightsAreNonPositive()
        {
            DataSchemaSO schema = CreateSchema("SpawnConditions", "Weight");
            DataRecord a = CreateRecord(50, -1, null);
            DataRecord b = CreateRecord(51, 0, null);
            DataRecord c = CreateRecord(52, "invalid", null);

            RandomiserSystem system = new RandomiserSystem(new[]
            {
                a,
                b,
                c
            }, schema);

            Dictionary<int, int> hitCounts = new Dictionary<int, int>
            {
                {
                    50, 0
                },
                {
                    51, 0
                },
                {
                    52, 0
                }
            };

            const int draws = 9000;
            for (int i = 0; i < draws; i++)
            {
                DataRecord selected = system.EvaluateRandom(new Dictionary<string, object>());
                Assert.IsNotNull(selected);
                int id = (int)selected.GetField("Card_ID");
                hitCounts[id]++;
            }

            foreach (KeyValuePair<int, int> pair in hitCounts)
            {
                float share = pair.Value / (float)draws;
                Assert.That(share, Is.EqualTo(1f / 3f).Within(0.07f), $"Uniform fallback share mismatch for card {pair.Key}.");
            }
        }

        [Test]
        public void GetValidChoices_MissingContextVariable_FailsCondition()
        {
            DataSchemaSO schema = CreateSchema("SpawnConditions", "Weight");
            DataRecord needsMissingStat = CreateRecord(60, 1, BuildConditions(new ParsedCondition("missingStat", ">", 0f)));
            DataRecord noConditions = CreateRecord(61, 1, null);

            RandomiserSystem system = new RandomiserSystem(new[]
            {
                needsMissingStat,
                noConditions
            }, schema);

            List<DataRecord> valid = system.GetValidChoices(new Dictionary<string, object>
            {
                {
                    "otherStat", 10f
                }
            });

            Assert.AreEqual(1, valid.Count);
            Assert.AreEqual(61, valid[0].GetField("Card_ID"));
        }

        private static void AssertWeightedSelectionFromSchema(string schemaAssetPath)
        {
            DataSchemaSO schema = AssetDatabase.LoadAssetAtPath<DataSchemaSO>(schemaAssetPath);
            Assert.IsNotNull(schema, $"Missing schema asset at '{schemaAssetPath}'.");
            Assert.IsTrue(schema.HasSourceDataFile(), "Schema is expected to have a source TextAsset assigned.");

            List<DataRecord> records = DynamicDataImporter.ImportFromSchema(schema);
            Assert.AreEqual(7, records.Count);

            RandomiserSystem system = new RandomiserSystem(records, schema);
            string weightColumnName = ResolveWeightColumnName(schema);
            Assert.IsNotNull(weightColumnName, "Schema must define exactly one WeightColumn.");

            Dictionary<string, object> notMetContext = new Dictionary<string, object>
            {
                {
                    "Friendship", 0f
                },
                {
                    "Finance", 0f
                },
                {
                    "Accademic_Performance", 100f
                },
                {
                    "Accademic Performance", 100f
                }
            };

            List<DataRecord> notMetChoices = system.GetValidChoices(notMetContext);
            Assert.Less(notMetChoices.Count, records.Count, "Expected at least one row to be filtered when conditions are not met.");

            // Draw with unmet conditions: selected rows must always come from the filtered subset.
            HashSet<int> notMetAllowedIds = notMetChoices
                .Select(static record => record.GetField("Card_ID"))
                .OfType<int>()
                .ToHashSet();
            Assert.Greater(notMetAllowedIds.Count, 0);

            const int notMetDraws = 5000;
            for (int i = 0; i < notMetDraws; i++)
            {
                DataRecord selected = system.EvaluateRandom(notMetContext);
                Assert.IsNotNull(selected);
                Assert.IsTrue(selected.GetField("Card_ID") is int selectedId && notMetAllowedIds.Contains(selectedId));
            }

            Dictionary<string, object> metContext = new Dictionary<string, object>
            {
                {
                    "Friendship", 100f
                },
                {
                    "Finance", 100f
                },
                {
                    "Accademic_Performance", 0f
                },
                {
                    "Accademic Performance", 0f
                }
            };

            List<DataRecord> validChoices = system.GetValidChoices(metContext);
            Assert.AreEqual(records.Count, validChoices.Count);

            Dictionary<int, float> weightsByCardId = validChoices
                .Where(static record => record.GetField("Card_ID") is int)
                .ToDictionary(
                    static record => (int)record.GetField("Card_ID"),
                    record => ReadWeightValue(record.GetField(weightColumnName)));

            float totalWeight = weightsByCardId.Values.Sum();
            Assert.Greater(totalWeight, 0f, "Expected positive total weight from imported rows.");

            float maxWeight = weightsByCardId.Values.Max();
            HashSet<int> topWeightIds = weightsByCardId
                .Where(pair => Mathf.Approximately(pair.Value, maxWeight))
                .Select(static pair => pair.Key)
                .ToHashSet();
            Assert.Greater(topWeightIds.Count, 0);

            int topWeightHits = 0;
            const int draws = 25000;
            Dictionary<int, int> drawCountByCardId = records
                .Select(static record => record.GetField("Card_ID"))
                .OfType<int>()
                .ToDictionary(static id => id, static _ => 0);

            for (int i = 0; i < draws; i++)
            {
                DataRecord selected = system.EvaluateRandom(metContext);
                Assert.IsNotNull(selected);

                if (selected.GetField("Card_ID") is int selectedCardId && drawCountByCardId.ContainsKey(selectedCardId))
                {
                    drawCountByCardId[selectedCardId]++;
                }

                if (selected.GetField("Card_ID") is int selectedId && topWeightIds.Contains(selectedId))
                {
                    topWeightHits++;
                }
            }

            foreach (KeyValuePair<int, int> pair in drawCountByCardId)
            {
                Assert.Greater(pair.Value, 0, $"Expected card {pair.Key} to be drawn at least once in {draws} draws.");
            }

            float expectedTopShare = topWeightIds.Sum(id => weightsByCardId[id]) / totalWeight;
            float actualTopShare = topWeightHits / (float)draws;
            Assert.That(actualTopShare, Is.EqualTo(expectedTopShare).Within(0.05f));
        }

        private static float ReadWeightValue(object rawWeight)
        {
            if (rawWeight == null)
            {
                return 0f;
            }

            return rawWeight switch
            {
                int intValue => intValue,
                long longValue => longValue,
                float floatValue => floatValue,
                double doubleValue => (float)doubleValue,
                string stringValue when float.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float parsedValue) => parsedValue,
                _ => 0f
            };
        }

        private static string ResolveWeightColumnName(DataSchemaSO schema)
        {
            List<ColumnDefinition> weightColumns = schema.Columns
                .Where(static column => column is { DataType: ColumnDataType.WeightColumn } && !string.IsNullOrWhiteSpace(column.ColumnName))
                .ToList();

            return weightColumns.Count != 1 ? null : weightColumns[0].ColumnName;

        }

        private static DataSchemaSO CreateSchema(string conditionColumnName, string weightColumnName)
        {
            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("Card_ID", ColumnDataType.Int));

            if (!string.IsNullOrWhiteSpace(conditionColumnName))
            {
                schema.Columns.Add(new ColumnDefinition(conditionColumnName, ColumnDataType.ConditionList));
            }

            if (!string.IsNullOrWhiteSpace(weightColumnName))
            {
                schema.Columns.Add(new ColumnDefinition(weightColumnName, ColumnDataType.WeightColumn));
            }

            return schema;
        }

        private static DataRecord CreateRecord(int cardId, object weightValue, List<ParsedCondition> conditions)
        {
            DataRecord record = new DataRecord();
            record.SetField("Card_ID", cardId);
            record.SetField("Weight", weightValue);

            if (conditions != null)
            {
                record.SetField("SpawnConditions", conditions);
            }

            return record;
        }

        private static List<ParsedCondition> BuildConditions(params ParsedCondition[] conditions)
        {
            return conditions?.ToList() ?? new List<ParsedCondition>();
        }
    }
}
