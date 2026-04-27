// Placeholder retained so the package path stays valid; active tests now live under Assets/Tests/EditMode.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Authoring;
using Csv;
using NUnit.Framework;
using UnityEngine;
using WeightedDraw;
namespace Tests.EditMode
{
    public class WeightedDrawSystemIntegrationTests
    {
        private const string exampleCsvRelativePath = "Packages/com.macmat01.weighted-draw-system/Tests/EditMode/Fixtures/Game_Data_SciFi_Academy.csv";

        [Test]
        public void RobustCsvParser_ParsesExampleCsv_IntoSevenRowsAndKeepsQuotedCells()
        {
            string csvText = LoadExampleCsvText();
            IReadOnlyList<IReadOnlyList<string>> rows = new RobustCsvParser().Parse(csvText);

            Assert.AreEqual(7, rows.Count, "Expected one header row plus six data rows.");
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.AreEqual(21, rows[i].Count, $"Row {i} should contain all 21 CSV columns.");
            }

            Assert.AreEqual("Card_ID", rows[0][0]);
            Assert.AreEqual("Attribute Name", rows[0][2]);
            Assert.AreEqual("Question", rows[0][4]);
            Assert.AreEqual("\"Zero-G party tonight in the lower airlock! Are you coming?\"", rows[1][4]);
            Assert.AreEqual("\"Fire up the thrusters!\"", rows[1][9]);
            StringAssert.Contains("shady smuggler", rows[5][14]);
            Assert.AreEqual("2006", rows[6][0]);
            Assert.AreEqual(string.Empty, rows[6][4]);
        }

        [Test]
        public void CsvRowCompiler_ParsesExampleCsv_IntoSixTypedRows()
        {
            List<ExampleCardRow> rows = CompileExampleRows();

            Assert.AreEqual(6, rows.Count);
            AssertRow(rows[0], 2001, "nebula_party", "Social Status, Finance", "Orion Sigma (Flight Crew)", "\"Zero-G party tonight in the lower airlock! Are you coming?\"", true, 1, string.Empty, 45f, "\"Fire up the thrusters!\"", string.Empty, "\"I need to recalibrate my sleep cycle.\"", string.Empty);
            AssertRow(rows[1], 2002, "flight_club_dues", "Finance, Social Status", "Orion Sigma (Flight Crew)", "\"Your hyperdrive certification is about to expire. You need to pay the guild dues in stellar credits to keep flying with us.\"", true, 1, string.Empty, 45f, "\"Transferring credits now.\"", string.Empty, "\"I'll fly under the radar.\"", string.Empty);
            AssertRow(rows[2], 2003, "simulation_trip", "Social Status, Finance", "Starfleet Command", "\"We're organizing a field trip to the Martian Terraforming Colony to study advanced atmospheres. Are you in?\"", true, 1, string.Empty, 40f, "\"Count me in.\"", string.Empty, "\"I'll study the holovids instead.\"", string.Empty);
            AssertRow(rows[3], 2004, "late_night_tinkering", "Sleep, Academic Performance", "Engineer Dalia", "\"I've snuck into the engine bay to overclock the quantum core. Wanna help me out? Could be good for our engineering finals.\"", true, 1, "Academic_Performance<50", 30f, "\"Grab the plasma wrench!\"", string.Empty,
                "\"Too risky, the Captain might catch us.\"", string.Empty);
            AssertRow(rows[4], 2005, "black_market_stims", "Sleep, Finance", "Medical Bay AI", "\"Warning: Cadet, your vital signs indicate extreme fatigue. I am not authorized to dispense stimulants, but... I know a guy in sector 4.\"", true, 2, "HasDiedOnce&&Sleep<30", 15f, "\"Give me the coordinates.\"",
                "\"The stims worked, but you owe a favor to a shady smuggler.\"", "\"I will just rest normally.\"", string.Empty);
            AssertRow(rows[5], 2006, "empty_card_1", string.Empty, "Captain Vance", string.Empty, false, 2, string.Empty, 0f, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        [Test]
        public void CsvDataSourceSO_OnValidate_SyncsColumnsAndCompilesInEditor()
        {
            string csvText = LoadExampleCsvText();
            ExampleCardSource authoring = ScriptableObject.CreateInstance<ExampleCardSource>();
            authoring.CompiledTable = ScriptableObject.CreateInstance<ExampleCardCompiledTableSO>();
            authoring.SourceCsvFiles.Add(CreateTextAsset(csvText));

            InvokeOnValidate(authoring);

            string[] expectedHeaders = csvText.Split('\n')[0].TrimEnd('\r').Split(',');
            Assert.That(authoring.Columns.Select(static column => column.ColumnName), Is.EquivalentTo(expectedHeaders));
            Assert.AreEqual(expectedHeaders.Length, authoring.Columns.Count);
            Assert.IsNotNull(authoring.CompiledTable, "CompiledTable was null after OnValidate; editor import did not produce a table instance.");
            Assert.AreEqual(6, authoring.CompiledTable.Rows.Count);
            Assert.AreEqual(2001, authoring.CompiledTable.Rows[0].CardId);
            Assert.AreEqual(2006, authoring.CompiledTable.Rows[5].CardId);

            InvokeOnValidate(authoring);
            Assert.AreEqual(expectedHeaders.Length, authoring.Columns.Count, "Repeated validation should not duplicate columns.");
        }

        [Test]
        public void WeightedDrawEngine_CanDrawEachExampleRow_AtLeastOnce()
        {
            List<ExampleCardRow> rows = CompileExampleRows();
            QueueRandomValueProvider randomProvider = new QueueRandomValueProvider(Array.Empty<float>());
            WeightedDrawEngine<ExampleCardRow, object> engine = new WeightedDrawEngine<ExampleCardRow, object>(
                static (row, context) => row.CardId == (int)context,
                static row => row.Weight,
                randomProvider);

            for (int i = 0; i < rows.Count; i++)
            {
                ExampleCardRow selected = engine.Draw(rows, rows[i].CardId);
                Assert.AreSame(rows[i], selected, $"Draw {i + 1} should select Card_ID {rows[i].CardId}.");
            }
        }

        private static string LoadExampleCsvText()
        {
            string csvPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", exampleCsvRelativePath));
            Assert.IsTrue(File.Exists(csvPath), $"Missing example CSV fixture at '{csvPath}'.");
            return File.ReadAllText(csvPath);
        }

        private static TextAsset CreateTextAsset(string csvText)
        {
            TextAsset asset = new TextAsset(csvText)
            {
                name = "CSV_Example1.csv"
            };

            return asset;
        }

        private static List<CsvColumnDefinition> BuildRequiredColumns(string csvText)
        {
            string headerLine = csvText.Split('\n')[0].TrimEnd('\r');
            return headerLine
                .Split(',')
                .Select(static header => new CsvColumnDefinition(header.Trim(), true))
                .ToList();
        }

        private static List<ExampleCardRow> CompileExampleRows()
        {
            string csvText = LoadExampleCsvText();
            CsvRowCompiler<ExampleCardRow> compiler = new CsvRowCompiler<ExampleCardRow>(null, new ExampleCardRowDeserializer());
            return compiler.Compile(new[]
            {
                csvText
            }, BuildRequiredColumns(csvText));
        }

        private static void InvokeOnValidate(CsvDataSourceSO<ExampleCardRow> authoring)
        {
            MethodInfo onValidate = typeof(CsvDataSourceSO<ExampleCardRow>).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(onValidate, "Unable to locate CsvDataSourceSO.OnValidate() via reflection.");
            onValidate.Invoke(authoring, null);
        }

        private static void AssertRow(
            ExampleCardRow actual,
            int cardId,
            string cardName,
            string attributes,
            string character,
            string question,
            bool isDrawable,
            int yearUnlock,
            string preConditions,
            float weight,
            string leftAnswer,
            string leftFollowUp,
            string rightAnswer,
            string rightFollowUp)
        {
            Assert.AreEqual(cardId, actual.CardId);
            Assert.AreEqual(cardName, actual.CardName);
            Assert.AreEqual(attributes, actual.Attributes);
            Assert.AreEqual(character, actual.Character);
            Assert.AreEqual(question, actual.Question);
            Assert.AreEqual(isDrawable, actual.IsDrawable);
            Assert.AreEqual(yearUnlock, actual.YearUnlock);
            Assert.AreEqual(preConditions, actual.PreConditions);
            Assert.AreEqual(weight, actual.Weight);
            Assert.AreEqual(leftAnswer, actual.LeftAnswer);
            Assert.AreEqual(leftFollowUp, actual.LeftFollowUp);
            Assert.AreEqual(rightAnswer, actual.RightAnswer);
            Assert.AreEqual(rightFollowUp, actual.RightFollowUp);
        }

        private sealed class ExampleCardSource : CsvDataSourceSO<ExampleCardRow>
        {
            protected override IRowDeserializer<ExampleCardRow> GetDeserializer()
            {
                return new ExampleCardRowDeserializer();
            }
        }

        private sealed class ExampleCardRowDeserializer : IRowDeserializer<ExampleCardRow>
        {
            public ExampleCardRow DeserializeRow(IReadOnlyDictionary<string, string> rowData, int rowNumber)
            {
                _ = rowNumber;

                return new ExampleCardRow
                {
                    CardId = ParseInt(rowData, "Card_ID"),
                    CardName = Get(rowData, "Card_Name"),
                    Attributes = Get(rowData, "Attribute Name"),
                    Character = Get(rowData, "Character_Name"),
                    Question = Get(rowData, "Question"),
                    IsDrawable = ParseBool(rowData, "Is_Drawable"),
                    YearUnlock = ParseInt(rowData, "Year_Unlock"),
                    PreConditions = Get(rowData, "Pre_Conditions"),
                    Weight = ParseFloat(rowData, "Weight"),
                    LeftAnswer = Get(rowData, "Left_Answer"),
                    LeftFollowUp = Get(rowData, "Left_FollowUp"),
                    RightAnswer = Get(rowData, "Right_Answer"),
                    RightFollowUp = Get(rowData, "Right_FollowUp")
                };
            }

            private static string Get(IReadOnlyDictionary<string, string> rowData, string key)
            {
                return rowData != null && rowData.TryGetValue(key, out string value) ? value : string.Empty;
            }

            private static int ParseInt(IReadOnlyDictionary<string, string> rowData, string key)
            {
                string value = Get(rowData, key);
                return int.TryParse(value, out int parsedValue) ? parsedValue : 0;
            }

            private static float ParseFloat(IReadOnlyDictionary<string, string> rowData, string key)
            {
                string value = Get(rowData, key);
                return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue) ? parsedValue : 0f;
            }

            private static bool ParseBool(IReadOnlyDictionary<string, string> rowData, string key)
            {
                string value = Get(rowData, key);
                return bool.TryParse(value, out bool parsedValue) ? parsedValue : string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

            }
        }

        private sealed class QueueRandomValueProvider : IRandomValueProvider
        {
            private readonly Queue<float> floatValues;

            public QueueRandomValueProvider(IEnumerable<float> values)
            {
                floatValues = new Queue<float>(values ?? Array.Empty<float>());
            }

            public float NextFloat(float minInclusive, float maxExclusive)
            {
                if (floatValues.Count == 0)
                {
                    return minInclusive;
                }

                float candidate = floatValues.Dequeue();
                if (candidate < minInclusive)
                {
                    return minInclusive;
                }

                if (candidate >= maxExclusive)
                {
                    return maxExclusive > minInclusive ? maxExclusive - 0.0001f : minInclusive;
                }

                return candidate;
            }

            public int NextInt(int minInclusive, int maxExclusive)
            {
                return minInclusive;
            }
        }
    }

    [Serializable]
    public sealed class ExampleCardRow
    {
        public int CardId;
        public string CardName;
        public string Attributes;
        public string Character;
        public string Question;
        public bool IsDrawable;
        public int YearUnlock;
        public string PreConditions;
        public float Weight;
        public string LeftAnswer;
        public string LeftFollowUp;
        public string RightAnswer;
        public string RightFollowUp;
    }

    public sealed class ExampleCardCompiledTableSO : CompiledCsvTableSO<ExampleCardRow>
    {
    }
}
