using System.Collections.Generic;
using Core.DynamicData;
using NUnit.Framework;
using UnityEngine;
namespace Tests.EditMode.Core.DynamicData
{
    public class DataRecordConditionParsingTests
    {
        private static DataSchemaSO CreateTestSchema()
        {
            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("Id", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Name", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Power", ColumnDataType.Int));
            schema.Columns.Add(new ColumnDefinition("Speed", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsActive", ColumnDataType.Bool));
            schema.Columns.Add(new ColumnDefinition("Requirements", ColumnDataType.ConditionList));

            return schema;
        }

        [Test]
        public void SchemaDrivenCsvParser_ParsesSimpleCondition()
        {
            const string csv = "Id,Name,Power,Speed,IsActive,Requirements\n" +
                "c1,Warrior,50,3.5,true,\"power>40\"";

            DataSchemaSO schema = CreateTestSchema();
            SchemaDrivenCsvParser parser = new SchemaDrivenCsvParser();
            List<DataRecord> records = parser.Parse(csv, schema);

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("c1", records[0].GetField("Id"));
            Assert.AreEqual("Warrior", records[0].GetField("Name"));
            Assert.AreEqual(50, records[0].GetField("Power"));

            List<ParsedCondition> conditions = records[0].GetField("Requirements") as List<ParsedCondition>;
            Assert.IsNotNull(conditions);
            Assert.AreEqual(1, conditions.Count);
            Assert.AreEqual("power", conditions[0].VariableName);
            Assert.AreEqual(">", conditions[0].Operator);
            Assert.AreEqual(40f, conditions[0].Value, 0.001f);
        }

        [Test]
        public void ConditionParserUtility_ParsesMultipleConditionsWithAnd()
        {
            const string conditionString = "power>40&&speed<5";

            List<ParsedCondition> conditions = ConditionParserUtility.Parse(conditionString);

            Assert.AreEqual(2, conditions.Count);

            Assert.AreEqual("power", conditions[0].VariableName);
            Assert.AreEqual(">", conditions[0].Operator);
            Assert.AreEqual(40f, conditions[0].Value, 0.001f);

            Assert.AreEqual("speed", conditions[1].VariableName);
            Assert.AreEqual("<", conditions[1].Operator);
            Assert.AreEqual(5f, conditions[1].Value, 0.001f);
        }

        [Test]
        public void ConditionParserUtility_SupportsAdditionalConnectors()
        {
            const string conditionString = "power>40 || speed<5 and !alchemy;level>=2";

            List<ParsedCondition> conditions = ConditionParserUtility.Parse(conditionString);

            Assert.AreEqual(4, conditions.Count);
            Assert.AreEqual("power", conditions[0].VariableName);
            Assert.AreEqual(">", conditions[0].Operator);
            Assert.AreEqual(40f, conditions[0].Value, 0.001f);

            Assert.AreEqual("speed", conditions[1].VariableName);
            Assert.AreEqual("<", conditions[1].Operator);
            Assert.AreEqual(5f, conditions[1].Value, 0.001f);

            Assert.AreEqual("alchemy", conditions[2].VariableName);
            Assert.AreEqual("!=", conditions[2].Operator);
            Assert.AreEqual(1f, conditions[2].Value, 0.001f);

            Assert.AreEqual("level", conditions[3].VariableName);
            Assert.AreEqual(">=", conditions[3].Operator);
            Assert.AreEqual(2f, conditions[3].Value, 0.001f);
        }

        [Test]
        public void ConditionParserUtility_ParsesBooleanFlags()
        {
            const string conditionString = "alchemy&&!cursed";

            List<ParsedCondition> conditions = ConditionParserUtility.Parse(conditionString);

            Assert.AreEqual(2, conditions.Count);

            // "alchemy" should be alchemy == 1
            Assert.AreEqual("alchemy", conditions[0].VariableName);
            Assert.AreEqual("==", conditions[0].Operator);
            Assert.AreEqual(1f, conditions[0].Value, 0.001f);

            // "!cursed" should be cursed != 1
            Assert.AreEqual("cursed", conditions[1].VariableName);
            Assert.AreEqual("!=", conditions[1].Operator);
            Assert.AreEqual(1f, conditions[1].Value, 0.001f);
        }

        [Test]
        public void ConditionParserUtility_SupportsAllOperators()
        {
            (string, string, string, float)[] testCases =
            {
                ("level==10", "level", "==", 10f),
                ("mana!=0", "mana", "!=", 0f),
                ("attack>=20", "attack", ">=", 20f),
                ("defense<=15", "defense", "<=", 15f),
                ("gold>100", "gold", ">", 100f),
                ("health<50", "health", "<", 50f)
            };

            foreach ((string input, string expectedVar, string expectedOp, float expectedVal) in testCases)
            {
                List<ParsedCondition> conditions = ConditionParserUtility.Parse(input);
                Assert.AreEqual(1, conditions.Count, $"Failed parsing: {input}");
                Assert.AreEqual(expectedVar, conditions[0].VariableName, $"Variable mismatch for: {input}");
                Assert.AreEqual(expectedOp, conditions[0].Operator, $"Operator mismatch for: {input}");
                Assert.AreEqual(expectedVal, conditions[0].Value, 0.001f, $"Value mismatch for: {input}");
            }
        }

        [Test]
        public void ConditionParserUtility_HandlesEmptyConditionString()
        {
            List<ParsedCondition> conditions1 = ConditionParserUtility.Parse("");
            List<ParsedCondition> conditions2 = ConditionParserUtility.Parse(null);
            List<ParsedCondition> conditions3 = ConditionParserUtility.Parse("   ");

            Assert.AreEqual(0, conditions1.Count);
            Assert.AreEqual(0, conditions2.Count);
            Assert.AreEqual(0, conditions3.Count);
        }

        [Test]
        public void SchemaDrivenCsvParser_ParsesMultipleRows()
        {
            const string csv = "Id,Name,Power,Speed,IsActive,Requirements\n" +
                "c1,Warrior,50,3.5,true,\"power>40\"\n" +
                "c2,Mage,30,5.2,false,\"level>=10||mana>50\"";

            DataSchemaSO schema = CreateTestSchema();
            SchemaDrivenCsvParser parser = new SchemaDrivenCsvParser();
            List<DataRecord> records = parser.Parse(csv, schema);

            Assert.AreEqual(2, records.Count);

            // First record
            if (records[0].GetField("Requirements") is List<ParsedCondition> conditions1)
            {
                Assert.AreEqual(1, conditions1.Count);
            }

            // Second record
            if (records[1].GetField("Requirements") is List<ParsedCondition> conditions2)
            {
                Assert.AreEqual(2, conditions2.Count);
            }
        }

        [Test]
        public void SchemaDrivenCsvParser_IgnoresMissingSchemaColumn()
        {
            const string csv = "Id,Name,UnknownColumn\n" +
                "c1,Warrior,SomeValue";

            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("Id", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Name", ColumnDataType.String));

            SchemaDrivenCsvParser parser = new SchemaDrivenCsvParser();
            List<DataRecord> records = parser.Parse(csv, schema);

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("c1", records[0].GetField("Id"));
            Assert.AreEqual("Warrior", records[0].GetField("Name"));
            // UnknownColumn is not in schema, so it shouldn't be in the record
            Assert.IsNull(records[0].GetField("UnknownColumn"));
        }

        [Test]
        public void SchemaDrivenCsvParser_HandlesTypeConversions()
        {
            const string csv = "Id,Name,Power,Speed,IsActive,Requirements\n" +
                "c1,Test,42,3.14,true,\"\"";

            DataSchemaSO schema = CreateTestSchema();
            SchemaDrivenCsvParser parser = new SchemaDrivenCsvParser();
            List<DataRecord> records = parser.Parse(csv, schema);

            Assert.AreEqual(1, records.Count);
            Assert.IsInstanceOf<string>(records[0].GetField("Id"));
            Assert.IsInstanceOf<int>(records[0].GetField("Power"));
            Assert.IsInstanceOf<float>(records[0].GetField("Speed"));
            Assert.IsInstanceOf<bool>(records[0].GetField("IsActive"));
            Assert.IsInstanceOf<List<ParsedCondition>>(records[0].GetField("Requirements"));
        }

        [Test]
        public void SchemaDrivenJsonParser_ParsesConditionListFromJsonArray()
        {
            const string json = "[{\"Id\":\"j1\",\"Name\":\"Runner\",\"Power\":12,\"Speed\":4.5,\"IsActive\":true,\"Requirements\":\"power>10&&speed<5\"}]";

            DataSchemaSO schema = CreateTestSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = parser.Parse(json, schema);

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("j1", records[0].GetField("Id"));
            Assert.AreEqual("Runner", records[0].GetField("Name"));

            List<ParsedCondition> conditions = records[0].GetField("Requirements") as List<ParsedCondition>;
            Assert.IsNotNull(conditions);
            Assert.AreEqual(2, conditions.Count);
            Assert.AreEqual("power", conditions[0].VariableName);
            Assert.AreEqual(">", conditions[0].Operator);
            Assert.AreEqual(10f, conditions[0].Value, 0.001f);
            Assert.AreEqual("speed", conditions[1].VariableName);
            Assert.AreEqual("<", conditions[1].Operator);
            Assert.AreEqual(5f, conditions[1].Value, 0.001f);
        }

        [Test]
        public void DynamicDataImporter_RoutesCsvAndJsonByExtension()
        {
            const string csv = "Id,Name,Power,Speed,IsActive,Requirements\n" +
                "c9,Chooser,20,3,true,\"power>=20\"";
            const string json = "[{\"Id\":\"j9\",\"Name\":\"ChooserJson\",\"Power\":20,\"Speed\":3,\"IsActive\":true,\"Requirements\":\"power>=20\"}]";

            DataSchemaSO schema = CreateTestSchema();

            List<DataRecord> csvRecords = DynamicDataImporter.ImportRaw(csv, ".csv", schema);
            List<DataRecord> jsonRecords = DynamicDataImporter.ImportRaw(json, ".json", schema);

            Assert.AreEqual(1, csvRecords.Count);
            Assert.AreEqual("Chooser", csvRecords[0].GetField("Name"));

            Assert.AreEqual(1, jsonRecords.Count);
            Assert.AreEqual("ChooserJson", jsonRecords[0].GetField("Name"));
        }
    }
}
