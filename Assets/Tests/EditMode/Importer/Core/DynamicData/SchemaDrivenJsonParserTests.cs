using System.Collections.Generic;
using Importer.Core.DynamicData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = System.Diagnostics.Debug;
namespace Tests.EditMode.Importer.Core.DynamicData
{
    public class SchemaDrivenJsonParserTests
    {
        private static DataSchemaSO CreateNpcSchema()
        {
            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("NpcID", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("NpcName", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Role", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Level", ColumnDataType.Int));
            schema.Columns.Add(new ColumnDefinition("Reputation", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsActive", ColumnDataType.Bool));
            schema.Columns.Add(new ColumnDefinition("SpawnConditions", ColumnDataType.ConditionList));

            return schema;
        }

        [Test]
        public void ParsesJsonArrayWithMultipleObjects()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_merchant_001\",\"NpcName\":\"John the Merchant\",\"Role\":\"Seller\",\"Level\":5,\"Reputation\":0.8,\"IsActive\":true,\"SpawnConditions\":\"level>=3\"}," +
                "{\"NpcID\":\"npc_guard_001\",\"NpcName\":\"Captain Guard\",\"Role\":\"Guard\",\"Level\":10,\"Reputation\":0.6,\"IsActive\":true,\"SpawnConditions\":\"level>=1&&joined_guard_faction\"}," +
                "{\"NpcID\":\"npc_boss_001\",\"NpcName\":\"Dark Lord\",\"Role\":\"Boss\",\"Level\":50,\"Reputation\":-1.0,\"IsActive\":false,\"SpawnConditions\":\"FALSE\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(3, records.Count);

            // NPC 1: Merchant
            Assert.AreEqual("npc_merchant_001", records[0].GetField("NpcID"));
            Assert.AreEqual("John the Merchant", records[0].GetField("NpcName"));
            Assert.AreEqual("Seller", records[0].GetField("Role"));
            Assert.AreEqual(5, records[0].GetField("Level"));
            Assert.AreEqual(0.8f, (float)records[0].GetField("Reputation"), 0.001f);
            Assert.AreEqual(true, records[0].GetField("IsActive"));

            // NPC 2: Guard
            Assert.AreEqual("npc_guard_001", records[1].GetField("NpcID"));
            Assert.AreEqual(10, records[1].GetField("Level"));
            Assert.AreEqual(true, records[1].GetField("IsActive"));

            // NPC 3: Boss
            Assert.AreEqual("npc_boss_001", records[2].GetField("NpcID"));
            Assert.AreEqual(50, records[2].GetField("Level"));
            Assert.AreEqual(false, records[2].GetField("IsActive"));
            Assert.That((float)records[2].GetField("Reputation"), Is.EqualTo(-1.0f).Within(0.001f));
        }

        [Test]
        public void ParsesConditionListColumn_VariousOperators()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"NPC A\",\"Role\":\"A\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>5\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"NPC B\",\"Role\":\"B\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"reputation<0.5\"}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":\"NPC C\",\"Role\":\"C\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level==10\"}," +
                "{\"NpcID\":\"npc_004\",\"NpcName\":\"NPC D\",\"Role\":\"D\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"reputation>=0.75\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(4, records.Count);

            Assert.AreEqual(">", (records[0].GetField("SpawnConditions") as List<ParsedCondition>)?[0].Operator);
            Assert.AreEqual("<", (records[1].GetField("SpawnConditions") as List<ParsedCondition>)?[0].Operator);
            Assert.AreEqual("==", (records[2].GetField("SpawnConditions") as List<ParsedCondition>)?[0].Operator);
            Assert.AreEqual(">=", (records[3].GetField("SpawnConditions") as List<ParsedCondition>)?[0].Operator);
        }

        [Test]
        public void ParsesComplexConditionsWithMultipleConnectors()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Complex 1\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>=5&&reputation>0.5\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"Complex 2\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"evil||blessed;strength>20\"}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":\"Complex 3\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"TRUE&&level>10||FALSE\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(3, records.Count);

            // Complex 1: Two conditions with AND
            List<ParsedCondition> conds1 = records[0].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds1 != null, nameof(conds1) + " != null");
            Assert.AreEqual(2, conds1.Count);
            Assert.AreEqual("AND", conds1[1].ConnectorFromPrevious);

            // Complex 2: Three conditions (OR, AND)
            List<ParsedCondition> conds2 = records[1].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds2 != null, nameof(conds2) + " != null");
            Assert.AreEqual(3, conds2.Count);
            Assert.AreEqual("OR", conds2[1].ConnectorFromPrevious);
            Assert.AreEqual("AND", conds2[2].ConnectorFromPrevious);

            // Complex 3: Three conditions with literals (AND, OR)
            List<ParsedCondition> conds3 = records[2].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds3 != null, nameof(conds3) + " != null");
            Assert.AreEqual(3, conds3.Count);
            Assert.IsTrue(conds3[0].IsBooleanLiteral);
            Assert.IsTrue(conds3[0].BooleanLiteralValue);
            Assert.IsTrue(conds3[2].IsBooleanLiteral);
            Assert.IsFalse(conds3[2].BooleanLiteralValue);
        }

        [Test]
        public void ParsesBooleanFlagsInConditions()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"NPC A\",\"Role\":\"A\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"blessed\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"NPC B\",\"Role\":\"B\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"!cursed\"}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":\"NPC C\",\"Role\":\"C\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"blessed&&!evil\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(3, records.Count);

            // blessed flag
            List<ParsedCondition> conds1 = records[0].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds1 != null, nameof(conds1) + " != null");
            Assert.AreEqual(1, conds1.Count);
            Assert.AreEqual("blessed", conds1[0].VariableName);
            Assert.AreEqual("==", conds1[0].Operator);
            Assert.That(conds1[0].Value, Is.EqualTo(1f).Within(0.001f));

            // !cursed flag
            List<ParsedCondition> conds2 = records[1].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds2 != null, nameof(conds2) + " != null");
            Assert.AreEqual(1, conds2.Count);
            Assert.AreEqual("cursed", conds2[0].VariableName);
            Assert.AreEqual("!=", conds2[0].Operator);

            // blessed&&!evil
            List<ParsedCondition> conds3 = records[2].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds3 != null, nameof(conds3) + " != null");
            Assert.AreEqual(2, conds3.Count);
            Assert.AreEqual("blessed", conds3[0].VariableName);
            Assert.AreEqual("evil", conds3[1].VariableName);
        }

        [Test]
        public void ParsesLiteralBooleanConstants()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Always Spawn\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"TRUE\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"Never Spawn\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":false,\"SpawnConditions\":\"FALSE\"}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":\"Case Insensitive\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"true\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(3, records.Count);

            // TRUE
            List<ParsedCondition> conds1 = records[0].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds1 != null, nameof(conds1) + " != null");
            Assert.AreEqual(1, conds1.Count);
            Assert.IsTrue(conds1[0].IsBooleanLiteral);
            Assert.IsTrue(conds1[0].BooleanLiteralValue);
            Assert.That(conds1[0].Value, Is.EqualTo(1f).Within(0.001f));

            // FALSE
            List<ParsedCondition> conds2 = records[1].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds2 != null, nameof(conds2) + " != null");
            Assert.AreEqual(1, conds2.Count);
            Assert.IsTrue(conds2[0].IsBooleanLiteral);
            Assert.IsFalse(conds2[0].BooleanLiteralValue);
            Assert.That(conds2[0].Value, Is.EqualTo(0f).Within(0.001f));

            // lowercase "true" (case-insensitive)
            List<ParsedCondition> conds3 = records[2].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds3 != null, nameof(conds3) + " != null");
            Assert.AreEqual(1, conds3.Count);
            Assert.IsTrue(conds3[0].IsBooleanLiteral);
            Assert.IsTrue(conds3[0].BooleanLiteralValue);
        }

        [Test]
        public void ParsesSingleObjectRoot()
        {
            const string json = "{\"NpcID\":\"npc_solo\",\"NpcName\":\"Solo NPC\",\"Role\":\"Lone\",\"Level\":7,\"Reputation\":0.5,\"IsActive\":true,\"SpawnConditions\":\"level>=5\"}";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("npc_solo", records[0].GetField("NpcID"));
            Assert.AreEqual("Solo NPC", records[0].GetField("NpcName"));
            Assert.AreEqual(7, records[0].GetField("Level"));
        }

        [Test]
        public void HandlesNullAndMissingFields()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Partial Data\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":null,\"Level\":5}," +
                "{\"NpcID\":\"npc_003\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(3, records.Count);

            // Missing fields
            Assert.IsNull(records[0].GetField("Level"));
            Assert.IsNull(records[0].GetField("SpawnConditions"));

            // Null field
            Assert.IsNull(records[1].GetField("NpcName"));
            Assert.AreEqual(5, records[1].GetField("Level"));

            // Only ID
            Assert.IsNull(records[2].GetField("NpcName"));
            Assert.IsNull(records[2].GetField("Level"));
        }

        [Test]
        public void HandlesInvalidTypeConversionsWithDefaults()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Bad Int\",\"Role\":\"R\",\"Level\":\"notAnInt\",\"Reputation\":0.5,\"IsActive\":true,\"SpawnConditions\":\"level>5\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"Bad Float\",\"Role\":\"R\",\"Level\":10,\"Reputation\":\"notAFloat\",\"IsActive\":true,\"SpawnConditions\":\"TRUE\"}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":\"Bad Bool\",\"Role\":\"R\",\"Level\":10,\"Reputation\":0.5,\"IsActive\":\"notABool\",\"SpawnConditions\":\"FALSE\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(3, records.Count);

            // Bad Int defaults to 0
            Assert.AreEqual(0, records[0].GetField("Level"));

            // Bad Float defaults to 0.0
            Assert.AreEqual(0f, records[1].GetField("Reputation"));

            // Bad Bool defaults to false
            Assert.AreEqual(false, records[2].GetField("IsActive"));
        }

        [Test]
        public void IgnoresExtraFieldsNotInSchema()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"NPC\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"TRUE\",\"ExtraField\":\"ShouldBeIgnored\",\"AnotherExtra\":123}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("npc_001", records[0].GetField("NpcID"));
            Assert.IsNull(records[0].GetField("ExtraField")); // Not in schema
            Assert.IsNull(records[0].GetField("AnotherExtra")); // Not in schema
        }

        [Test]
        public void HandlesEmptyAndNullConditionStrings()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Empty Condition\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"Null Condition\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":null}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":\"Normal\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>=1\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(3, records.Count);

            // Empty condition
            List<ParsedCondition> conds1 = records[0].GetField("SpawnConditions") as List<ParsedCondition>;
            Assert.IsNotNull(conds1);
            Assert.AreEqual(0, conds1.Count);

            // Null condition
            List<ParsedCondition> conds2 = records[1].GetField("SpawnConditions") as List<ParsedCondition>;
            Assert.IsNotNull(conds2);
            Assert.AreEqual(0, conds2.Count);

            // Normal condition
            List<ParsedCondition> conds3 = records[2].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds3 != null, nameof(conds3) + " != null");
            Assert.AreEqual(1, conds3.Count);
        }

        [Test]
        public void HandlesFloatPrecision()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Float Test 1\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.333333,\"IsActive\":true,\"SpawnConditions\":\"reputation>0.3\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"Float Test 2\",\"Role\":\"R\",\"Level\":1,\"Reputation\":3.14159,\"IsActive\":true,\"SpawnConditions\":\"reputation<10.0\"}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":\"Float Test 3\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.000001,\"IsActive\":true,\"SpawnConditions\":\"reputation>=0.0\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(3, records.Count);
            Assert.That((float)records[0].GetField("Reputation"), Is.EqualTo(0.333333f).Within(0.0001f));
            Assert.That((float)records[1].GetField("Reputation"), Is.EqualTo(3.14159f).Within(0.0001f));
            Assert.That((float)records[2].GetField("Reputation"), Is.EqualTo(0.000001f).Within(0.000001f));
        }

        [Test]
        public void HandlesNegativeNumbers()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Negative Int\",\"Role\":\"R\",\"Level\":-5,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level<0\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"Negative Float\",\"Role\":\"R\",\"Level\":1,\"Reputation\":-0.5,\"IsActive\":true,\"SpawnConditions\":\"reputation<=-0.3\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(2, records.Count);
            Assert.AreEqual(-5, records[0].GetField("Level"));
            Assert.That((float)records[1].GetField("Reputation"), Is.EqualTo(-0.5f).Within(0.001f));
        }

        [Test]
        public void ParsesBooleanColumnCorrectly()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Bool True\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>=1\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"Bool False\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":false,\"SpawnConditions\":\"level>=1\"}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":\"Bool 1\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":1,\"SpawnConditions\":\"level>=1\"}," +
                "{\"NpcID\":\"npc_004\",\"NpcName\":\"Bool 0\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":0,\"SpawnConditions\":\"level>=1\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(4, records.Count);
            Assert.AreEqual(true, records[0].GetField("IsActive"));
            Assert.AreEqual(false, records[1].GetField("IsActive"));
            Assert.AreEqual(true, records[2].GetField("IsActive")); // 1 becomes true
            Assert.AreEqual(false, records[3].GetField("IsActive")); // 0 becomes false
        }

        [Test]
        public void ParsesSpecialCharactersInStringFields()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"NPC with \\\"quotes\\\"\",\"Role\":\"Guard, Special\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>=1\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"NPC with \\\\backslash\",\"Role\":\"Role/Other\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"TRUE\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("npc_001", records[0].GetField("NpcID"));
            Assert.AreEqual("npc_002", records[1].GetField("NpcID"));
        }

        [Test]
        public void SkipsMalformedConditionSegmentsAndKeepsValid()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Malformed\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>5&&invalid$$$||reputation>0.5\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"Double Connector\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>=1&&&&reputation<1.0\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(2, records.Count);

            // Should have level and reputation, skipping invalid
            List<ParsedCondition> conds1 = records[0].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds1 != null, nameof(conds1) + " != null");
            Assert.AreEqual(2, conds1.Count);
            Assert.AreEqual("level", conds1[0].VariableName);
            Assert.AreEqual("reputation", conds1[1].VariableName);

            // Should have level and reputation, skipping double connector
            List<ParsedCondition> conds2 = records[1].GetField("SpawnConditions") as List<ParsedCondition>;
            Debug.Assert(conds2 != null, nameof(conds2) + " != null");
            Assert.AreEqual(2, conds2.Count);
            Assert.AreEqual("level", conds2[0].VariableName);
            Assert.AreEqual("reputation", conds2[1].VariableName);
        }

        [Test]
        public void ParsesAllConnectorVariantsInJson()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"NPC A\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>1&&reputation>0\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"NPC B\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>1 and reputation>0\"}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":\"NPC C\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>1;reputation>0\"}," +
                "{\"NpcID\":\"npc_004\",\"NpcName\":\"NPC D\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>1||reputation>0\"}," +
                "{\"NpcID\":\"npc_005\",\"NpcName\":\"NPC E\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>1 or reputation>0\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(5, records.Count);

            Assert.AreEqual("AND", ((List<ParsedCondition>)records[0].GetField("SpawnConditions"))[1].ConnectorFromPrevious);
            Assert.AreEqual("AND", ((List<ParsedCondition>)records[1].GetField("SpawnConditions"))[1].ConnectorFromPrevious);
            Assert.AreEqual("AND", ((List<ParsedCondition>)records[2].GetField("SpawnConditions"))[1].ConnectorFromPrevious);
            Assert.AreEqual("OR", ((List<ParsedCondition>)records[3].GetField("SpawnConditions"))[1].ConnectorFromPrevious);
            Assert.AreEqual("OR", ((List<ParsedCondition>)records[4].GetField("SpawnConditions"))[1].ConnectorFromPrevious);
        }

        [Test]
        public void OptionalFieldsAllowNullAndEmptyValues()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"NPC A\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"NPC B\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":null}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":\"NPC C\",\"Role\":null,\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>=1\"}" +
                "]";

            DataSchemaSO schema = CreateNpcSchema();
            // All columns are optional by default
            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            // All records should be accepted since columns are optional
            Assert.AreEqual(3, records.Count);
            Assert.AreEqual("npc_001", records[0].GetField("NpcID"));
            Assert.AreEqual("npc_002", records[1].GetField("NpcID"));
            Assert.AreEqual("npc_003", records[2].GetField("NpcID"));
        }

        [Test]
        public void RequiredFieldsRejectNullAndEmptyValues()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Valid NPC\",\"Role\":\"R\",\"Level\":5,\"Reputation\":0.5,\"IsActive\":true,\"SpawnConditions\":\"level>=1\"}," +
                "{\"NpcID\":\"\",\"NpcName\":\"NPC B\",\"Role\":\"R\",\"Level\":10,\"Reputation\":0.6,\"IsActive\":true,\"SpawnConditions\":\"level>=2\"}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":null,\"Role\":\"R\",\"Level\":15,\"Reputation\":0.7,\"IsActive\":true,\"SpawnConditions\":\"level>=3\"}" +
                "]";

            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("NpcID", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("NpcName", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("Role", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Level", ColumnDataType.Int));
            schema.Columns.Add(new ColumnDefinition("Reputation", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsActive", ColumnDataType.Bool));
            schema.Columns.Add(new ColumnDefinition("SpawnConditions", ColumnDataType.ConditionList));

            LogAssert.Expect(LogType.Error, "SchemaDrivenJsonParser: Required field 'NpcID' is empty at item 2.");
            LogAssert.Expect(LogType.Warning, "SchemaDrivenJsonParser: Skipping item 2 due to missing required fields.");
            LogAssert.Expect(LogType.Error, "SchemaDrivenJsonParser: Required field 'NpcName' is empty at item 3.");
            LogAssert.Expect(LogType.Warning, "SchemaDrivenJsonParser: Skipping item 3 due to missing required fields.");

            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            // Only the first NPC should be accepted (2nd has empty NpcID, 3rd has null NpcName)
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("npc_001", records[0].GetField("NpcID"));
        }

        [Test]
        public void RequiredConditionColumnEnforcesNonEmpty()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Valid\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>=1\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"Empty Condition\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"\"}," +
                "{\"NpcID\":\"npc_003\",\"NpcName\":\"Missing Condition\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true}" +
                "]";

            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("NpcID", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("NpcName", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("Role", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Level", ColumnDataType.Int));
            schema.Columns.Add(new ColumnDefinition("Reputation", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsActive", ColumnDataType.Bool));
            schema.Columns.Add(new ColumnDefinition("SpawnConditions", ColumnDataType.ConditionList, true));

            LogAssert.Expect(LogType.Error, "SchemaDrivenJsonParser: Required field 'SpawnConditions' is empty at item 2.");
            LogAssert.Expect(LogType.Warning, "SchemaDrivenJsonParser: Skipping item 2 due to missing required fields.");
            LogAssert.Expect(LogType.Error, "SchemaDrivenJsonParser: Required field 'SpawnConditions' not found at item 3.");
            LogAssert.Expect(LogType.Warning, "SchemaDrivenJsonParser: Skipping item 3 due to missing required fields.");

            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            // Only npc_001 should be accepted (npc_002 has empty, npc_003 is missing SpawnConditions)
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("npc_001", records[0].GetField("NpcID"));
        }

        [Test]
        public void MixedRequiredAndOptionalFields()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Complete\",\"Role\":\"\",\"Level\":5,\"Reputation\":0.5,\"IsActive\":true,\"SpawnConditions\":\"TRUE\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":null,\"Role\":\"Warrior\",\"Level\":10,\"Reputation\":0.6,\"IsActive\":true,\"SpawnConditions\":\"level>=1\"}," +
                "{\"NpcID\":\"\",\"NpcName\":\"Archer\",\"Role\":\"Archer\",\"Level\":15,\"Reputation\":0.7,\"IsActive\":false,\"SpawnConditions\":\"level>=5\"}" +
                "]";

            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("NpcID", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("NpcName", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("Role", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Level", ColumnDataType.Int, true));
            schema.Columns.Add(new ColumnDefinition("Reputation", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsActive", ColumnDataType.Bool));
            schema.Columns.Add(new ColumnDefinition("SpawnConditions", ColumnDataType.ConditionList));

            LogAssert.Expect(LogType.Error, "SchemaDrivenJsonParser: Required field 'NpcName' is empty at item 2.");
            LogAssert.Expect(LogType.Warning, "SchemaDrivenJsonParser: Skipping item 2 due to missing required fields.");
            LogAssert.Expect(LogType.Error, "SchemaDrivenJsonParser: Required field 'NpcID' is empty at item 3.");
            LogAssert.Expect(LogType.Warning, "SchemaDrivenJsonParser: Skipping item 3 due to missing required fields.");

            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            // Only npc_001 should be accepted
            // npc_001: has all required fields (NpcID, NpcName, Level)
            // npc_002: fails - NpcName is null (required)
            // npc_003: fails - NpcID is empty (required)
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("npc_001", records[0].GetField("NpcID"));
        }

        [Test]
        public void WhitespaceOnlyValuesOnRequiredFieldsFail()
        {
            const string json = "[" +
                "{\"NpcID\":\"   \",\"NpcName\":\"NPC A\",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"TRUE\"}," +
                "{\"NpcID\":\"npc_002\",\"NpcName\":\"   \",\"Role\":\"R\",\"Level\":1,\"Reputation\":0.0,\"IsActive\":true,\"SpawnConditions\":\"level>=1\"}" +
                "]";

            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("NpcID", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("NpcName", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("Role", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Level", ColumnDataType.Int));
            schema.Columns.Add(new ColumnDefinition("Reputation", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsActive", ColumnDataType.Bool));
            schema.Columns.Add(new ColumnDefinition("SpawnConditions", ColumnDataType.ConditionList));

            LogAssert.Expect(LogType.Error, "SchemaDrivenJsonParser: Required field 'NpcID' is empty at item 1.");
            LogAssert.Expect(LogType.Warning, "SchemaDrivenJsonParser: Skipping item 1 due to missing required fields.");
            LogAssert.Expect(LogType.Error, "SchemaDrivenJsonParser: Required field 'NpcName' is empty at item 2.");
            LogAssert.Expect(LogType.Warning, "SchemaDrivenJsonParser: Skipping item 2 due to missing required fields.");

            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            // Both should fail - whitespace-only values on required fields are treated as empty
            Assert.AreEqual(0, records.Count);
        }

        [Test]
        public void MissingRequiredFieldInJson()
        {
            const string json = "[" +
                "{\"NpcID\":\"npc_001\",\"NpcName\":\"Complete\",\"Role\":\"R\",\"Level\":5,\"Reputation\":0.5,\"IsActive\":true,\"SpawnConditions\":\"TRUE\"}," +
                "{\"NpcID\":\"npc_002\",\"Role\":\"R\",\"Level\":10,\"Reputation\":0.6,\"IsActive\":true,\"SpawnConditions\":\"level>=1\"}" +
                "]";

            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("NpcID", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("NpcName", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("Role", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Level", ColumnDataType.Int));
            schema.Columns.Add(new ColumnDefinition("Reputation", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsActive", ColumnDataType.Bool));
            schema.Columns.Add(new ColumnDefinition("SpawnConditions", ColumnDataType.ConditionList));

            LogAssert.Expect(LogType.Error, "SchemaDrivenJsonParser: Required field 'NpcName' not found at item 2.");
            LogAssert.Expect(LogType.Warning, "SchemaDrivenJsonParser: Skipping item 2 due to missing required fields.");

            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            // Only npc_001 should be accepted (npc_002 is missing required NpcName field)
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("npc_001", records[0].GetField("NpcID"));
        }

        [Test]
        public void ParsesNestedObjects_WithCompoundLeafKeys_ToGenericNumberedAliases()
        {
            const string json = "[" +
                "{\"Card_ID\":101,\"Left_Choice\":{\"Attributes\":{\"Friendship\":10,\"Accademic_Performance\":-10,\"Finance\":-5}},\"Right_Choice\":{\"Attributes\":{\"Friendship\":-10,\"Accademic_Performance\":10,\"Finance\":0}}}" +
                "]";

            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("Card_ID", ColumnDataType.Int, true));
            schema.Columns.Add(new ColumnDefinition("Left_Attribute1", ColumnDataType.Int, true));
            schema.Columns.Add(new ColumnDefinition("Left_Attribute2", ColumnDataType.Int, true));
            schema.Columns.Add(new ColumnDefinition("Right_Attribute1", ColumnDataType.Int, true));
            schema.Columns.Add(new ColumnDefinition("Right_Attribute2", ColumnDataType.Int, true));

            SchemaDrivenJsonParser parser = new SchemaDrivenJsonParser();
            List<DataRecord> records = SchemaDrivenJsonParser.Parse(json, schema);

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(101, records[0].GetField("Card_ID"));
            Assert.AreEqual(10, records[0].GetField("Left_Attribute1"));
            Assert.AreEqual(-10, records[0].GetField("Left_Attribute2"));
            Assert.AreEqual(-10, records[0].GetField("Right_Attribute1"));
            Assert.AreEqual(10, records[0].GetField("Right_Attribute2"));
        }
    }
}
