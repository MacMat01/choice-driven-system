using System.Collections.Generic;
using _Old.Runtime.SchemaImporter.Parsers;
using _Old.Runtime.SchemaImporter.Schema;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = System.Diagnostics.Debug;
namespace _Old.Tests.EditMode.SchemaImporter
{
    public class CsvDataParserTests
    {
        private static DataSchemaSO CreateItemSchema()
        {
            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("ItemID", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("ItemName", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Rarity", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Price", ColumnDataType.Int));
            schema.Columns.Add(new ColumnDefinition("CritChance", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsEnabled", ColumnDataType.Bool));
            schema.Columns.Add(new ColumnDefinition("SpawnRequirements", ColumnDataType.ConditionList));

            return schema;
        }

        [Test]
        public void ParsesBasicCsvWithMultipleRows()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_sword_001,Iron Sword,Common,100,0.1,true,\"level>=5\"\n" +
                "item_armor_001,Steel Armor,Rare,500,0.0,true,\"level>=10&&strength>20\"\n" +
                "item_cursed_001,Cursed Blade,Legendary,0,0.5,false,FALSE";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(3, records.Count);

            // Row 1: Iron Sword
            Assert.AreEqual("item_sword_001", records[0].GetField("ItemID"));
            Assert.AreEqual("Iron Sword", records[0].GetField("ItemName"));
            Assert.AreEqual("Common", records[0].GetField("Rarity"));
            Assert.AreEqual(100, records[0].GetField("Price"));
            Assert.AreEqual(0.1f, (float)records[0].GetField("CritChance"), 0.001f);
            Assert.AreEqual(true, records[0].GetField("IsEnabled"));

            // Row 2: Steel Armor
            Assert.AreEqual("item_armor_001", records[1].GetField("ItemID"));
            Assert.AreEqual(500, records[1].GetField("Price"));
            Assert.AreEqual(true, records[1].GetField("IsEnabled"));

            // Row 3: Cursed Blade
            Assert.AreEqual("item_cursed_001", records[2].GetField("ItemID"));
            Assert.AreEqual(0, records[2].GetField("Price"));
            Assert.AreEqual(false, records[2].GetField("IsEnabled"));
        }

        [Test]
        public void ParsesConditionListColumn_SimpleOperators()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Sword,Common,100,0.1,true,\"power>40\"\n" +
                "item_002,Shield,Common,150,0.0,true,\"armor<=100\"\n" +
                "item_003,Potion,Common,50,0.0,true,\"health!=100\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(3, records.Count);

            // Verify operators are parsed correctly
            if (records[0].GetField("SpawnRequirements") is List<ParsedCondition> conditions1)
            {
                Assert.AreEqual(1, conditions1.Count);
                Assert.AreEqual(">", conditions1[0].Operator);
                Assert.That(conditions1[0].Value, Is.EqualTo(40f).Within(0.001f));
            }

            if (records[1].GetField("SpawnRequirements") is List<ParsedCondition> conditions2)
            {
                Assert.AreEqual(1, conditions2.Count);
                Assert.AreEqual("<=", conditions2[0].Operator);
            }

            if (records[2].GetField("SpawnRequirements") is List<ParsedCondition> conditions3)
            {
                Assert.AreEqual(1, conditions3.Count);
                Assert.AreEqual("!=", conditions3[0].Operator);
            }
        }

        [Test]
        public void ParsesConditionListColumn_ComplexLogicWithConnectors()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Weapon A,Rare,300,0.2,true,\"power>30&&level>=5\"\n" +
                "item_002,Weapon B,Rare,350,0.25,true,\"cursed||blessed;armor>10\"\n" +
                "item_003,Weapon C,Rare,400,0.3,true,\"TRUE&&power>50||FALSE\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(3, records.Count);

            // Item 1: power>30&&level>=5
            List<ParsedCondition> conds1 = records[0].GetField("SpawnRequirements") as List<ParsedCondition>;
            Debug.Assert(conds1 != null, nameof(conds1) + " != null");
            Assert.AreEqual(2, conds1.Count);
            Assert.AreEqual("power", conds1[0].VariableName);
            Assert.AreEqual("AND", conds1[1].ConnectorFromPrevious);

            // Item 2: cursed||blessed;armor>10
            List<ParsedCondition> conds2 = records[1].GetField("SpawnRequirements") as List<ParsedCondition>;
            Debug.Assert(conds2 != null, nameof(conds2) + " != null");
            Assert.AreEqual(3, conds2.Count);
            Assert.AreEqual("cursed", conds2[0].VariableName);
            Assert.AreEqual("OR", conds2[1].ConnectorFromPrevious);
            Assert.AreEqual("AND", conds2[2].ConnectorFromPrevious);

            // Item 3: TRUE&&power>50||FALSE
            List<ParsedCondition> conds3 = records[2].GetField("SpawnRequirements") as List<ParsedCondition>;
            Debug.Assert(conds3 != null, nameof(conds3) + " != null");
            Assert.AreEqual(3, conds3.Count);
            Assert.IsTrue(conds3[0].IsBooleanLiteral);
            Assert.IsTrue(conds3[0].BooleanLiteralValue);
            Assert.AreEqual("power", conds3[1].VariableName);
            Assert.IsTrue(conds3[2].IsBooleanLiteral);
            Assert.IsFalse(conds3[2].BooleanLiteralValue);
            Assert.AreEqual("OR", conds3[2].ConnectorFromPrevious);
        }

        [Test]
        public void ParsesLiteralBooleanConstants_TrueAndFalse()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Always Available,Common,100,0.0,true,TRUE\n" +
                "item_002,Never Available,Common,50,0.0,false,FALSE\n" +
                "item_003,Mixed Logic,Rare,200,0.15,true,\"TRUE&&power>20||FALSE\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(3, records.Count);

            // TRUE constant
            List<ParsedCondition> conds1 = records[0].GetField("SpawnRequirements") as List<ParsedCondition>;
            Debug.Assert(conds1 != null, nameof(conds1) + " != null");
            Assert.AreEqual(1, conds1.Count);
            Assert.IsTrue(conds1[0].IsBooleanLiteral);
            Assert.IsTrue(conds1[0].BooleanLiteralValue);
            Assert.That(conds1[0].Value, Is.EqualTo(1f).Within(0.001f));

            // FALSE constant
            List<ParsedCondition> conds2 = records[1].GetField("SpawnRequirements") as List<ParsedCondition>;
            Debug.Assert(conds2 != null, nameof(conds2) + " != null");
            Assert.AreEqual(1, conds2.Count);
            Assert.IsTrue(conds2[0].IsBooleanLiteral);
            Assert.IsFalse(conds2[0].BooleanLiteralValue);
            Assert.That(conds2[0].Value, Is.EqualTo(0f).Within(0.001f));

            // Mixed with TRUE and FALSE
            List<ParsedCondition> conds3 = records[2].GetField("SpawnRequirements") as List<ParsedCondition>;
            Debug.Assert(conds3 != null, nameof(conds3) + " != null");
            Assert.AreEqual(3, conds3.Count);
            Assert.IsTrue(conds3[0].IsBooleanLiteral);
            Assert.IsTrue(conds3[0].BooleanLiteralValue);
            Assert.IsTrue(conds3[2].IsBooleanLiteral);
            Assert.IsFalse(conds3[2].BooleanLiteralValue);
        }

        [Test]
        public void HandlesEmptyAndNullConditions()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Item With Empty Condition,Common,100,0.0,true,\"\"\n" +
                "item_002,Item With Space Condition,Common,150,0.0,true,\"   \"\n" +
                "item_003,Normal Item,Common,200,0.0,true,\"level>=1\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(3, records.Count);

            // Empty condition
            List<ParsedCondition> conds1 = records[0].GetField("SpawnRequirements") as List<ParsedCondition>;
            Assert.IsNotNull(conds1);
            Assert.AreEqual(0, conds1.Count);

            // Space condition (treated as empty)
            List<ParsedCondition> conds2 = records[1].GetField("SpawnRequirements") as List<ParsedCondition>;
            Assert.IsNotNull(conds2);
            Assert.AreEqual(0, conds2.Count);

            // Normal condition
            List<ParsedCondition> conds3 = records[2].GetField("SpawnRequirements") as List<ParsedCondition>;
            Debug.Assert(conds3 != null, nameof(conds3) + " != null");
            Assert.AreEqual(1, conds3.Count);
        }

        [Test]
        public void HandlesQuotedFieldsWithCommas()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,\"Sword, Legendary Edition\",Legendary,999,0.9,true,\"power>100\"\n" +
                "item_002,\"Shield, Knight's, Special\",Rare,500,0.0,true,\"armor>50&&strength>30\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("Sword, Legendary Edition", records[0].GetField("ItemName"));
            Assert.AreEqual("Shield, Knight's, Special", records[1].GetField("ItemName"));
        }

        [Test]
        public void HandlesBooleanColumnTypeCorrectly()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Item A,Common,100,0.0,true,\"power>5\"\n" +
                "item_002,Item B,Common,150,0.0,false,\"armor>10\"\n" +
                "item_003,Item C,Common,200,0.0,1,\"level>=1\"\n" +
                "item_004,Item D,Common,250,0.0,0,\"TRUE\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(4, records.Count);
            Assert.AreEqual(true, records[0].GetField("IsEnabled"));
            Assert.AreEqual(false, records[1].GetField("IsEnabled"));
            Assert.AreEqual(true, records[2].GetField("IsEnabled")); // "1" converts to true
            Assert.AreEqual(false, records[3].GetField("IsEnabled")); // "0" converts to false
        }

        [Test]
        public void HandlesFloatPrecision()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Precise Item,Rare,100,0.333333,true,\"power>5\"\n" +
                "item_002,Another Item,Rare,200,3.14159,true,\"armor>10\"\n" +
                "item_003,Float Item,Rare,300,0.000001,true,\"level>=1\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(3, records.Count);
            Assert.That((float)records[0].GetField("CritChance"), Is.EqualTo(0.333333f).Within(0.0001f));
            Assert.That((float)records[1].GetField("CritChance"), Is.EqualTo(3.14159f).Within(0.0001f));
            Assert.That((float)records[2].GetField("CritChance"), Is.EqualTo(0.000001f).Within(0.000001f));
        }

        [Test]
        public void HandlesInvalidTypeConversionsWithDefaults()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Bad Int,Common,notANumber,0.5,true,\"power>5\"\n" +
                "item_002,Bad Float,Common,100,notAFloat,false,\"armor>10\"\n" +
                "item_003,Bad Bool,Common,200,0.3,notABool,\"level>=1\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(3, records.Count);

            // Bad Int defaults to 0
            Assert.AreEqual(0, records[0].GetField("Price"));

            // Bad Float defaults to 0.0
            Assert.AreEqual(0f, records[1].GetField("CritChance"));

            // Bad Bool defaults to false
            Assert.AreEqual(false, records[2].GetField("IsEnabled"));
        }

        [Test]
        public void SkipsMissingSchemaColumnsInCsv()
        {
            const string csv = "ItemID,ItemName,UnknownColumn,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Sword,ShouldBeIgnored,100,0.1,true,\"power>10\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("item_001", records[0].GetField("ItemID"));
            Assert.IsNull(records[0].GetField("UnknownColumn")); // Not in schema, so ignored
        }

        [Test]
        public void HandlesMissingSchemaColumnsInCsv()
        {
            const string csv = "ItemID,ItemName,Price\n" +
                "item_001,Sword,100\n" +
                "item_002,Shield,150";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("item_001", records[0].GetField("ItemID"));
            Assert.IsNull(records[0].GetField("CritChance")); // Missing from CSV
            Assert.IsNull(records[0].GetField("SpawnRequirements")); // Missing from CSV
        }

        [Test]
        public void HandlesMultilineQuotedFields()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,\"Sword\nWith\nDescription\",Common,100,0.1,true,\"power>5\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Sword\nWith\nDescription", records[0].GetField("ItemName"));
        }

        [Test]
        public void HandlesEscapedQuotesInQuotedFields()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,\"Sword \"\"Legendary\"\"\",Common,100,0.1,true,\"power>5\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Sword \"Legendary\"", records[0].GetField("ItemName"));
        }

        [Test]
        public void SkipsBlankRows()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Sword,Common,100,0.1,true,\"power>5\"\n" +
                "\n" +
                "item_002,Shield,Common,150,0.0,false,\"armor>10\"\n" +
                "\n" +
                "\n" +
                "item_003,Potion,Common,50,0.0,true,TRUE";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(3, records.Count);
            Assert.AreEqual("item_001", records[0].GetField("ItemID"));
            Assert.AreEqual("item_002", records[1].GetField("ItemID"));
            Assert.AreEqual("item_003", records[2].GetField("ItemID"));
        }

        [Test]
        public void HandlesCrlfLineEndings()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\r\n" +
                "item_001,Sword,Common,100,0.1,true,\"power>5\"\r\n" +
                "item_002,Shield,Common,150,0.0,false,\"armor>10\"\r\n" +
                "item_003,Potion,Common,50,0.0,true,TRUE\r\n";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(3, records.Count);
            Assert.AreEqual("item_001", records[0].GetField("ItemID"));
            Assert.AreEqual("item_002", records[1].GetField("ItemID"));
            Assert.AreEqual("item_003", records[2].GetField("ItemID"));
        }

        [Test]
        public void SkipsMalformedConditionSegmentsAndKeepsValid()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Sword,Common,100,0.1,true,\"power>5&&invalid$$$||armor>10\"\n" +
                "item_002,Shield,Common,150,0.0,false,\"level>=1&&&&power>20\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(2, records.Count);

            // Item 1: Should have power and armor, skipping invalid
            List<ParsedCondition> conds1 = records[0].GetField("SpawnRequirements") as List<ParsedCondition>;
            Debug.Assert(conds1 != null, nameof(conds1) + " != null");
            Assert.AreEqual(2, conds1.Count);
            Assert.AreEqual("power", conds1[0].VariableName);
            Assert.AreEqual("armor", conds1[1].VariableName);

            // Item 2: Should have level and power, skipping invalid connector
            List<ParsedCondition> conds2 = records[1].GetField("SpawnRequirements") as List<ParsedCondition>;
            Debug.Assert(conds2 != null, nameof(conds2) + " != null");
            Assert.AreEqual(2, conds2.Count);
            Assert.AreEqual("level", conds2[0].VariableName);
            Assert.AreEqual("power", conds2[1].VariableName);
        }

        [Test]
        public void ParsesAllConditionConnectorVariants()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Item A,Common,100,0.0,true,\"power>5&&armor>10\"\n" +
                "item_002,Item B,Common,100,0.0,true,\"power>5||armor>10\"\n" +
                "item_003,Item C,Common,100,0.0,true,\"power>5 and armor>10\"\n" +
                "item_004,Item D,Common,100,0.0,true,\"power>5 or armor>10\"\n" +
                "item_005,Item E,Common,100,0.0,true,\"power>5;armor>10\"";

            DataSchemaSO schema = CreateItemSchema();
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            Assert.AreEqual(5, records.Count);

            // Check connector normalization
            Assert.AreEqual("AND", (records[0].GetField("SpawnRequirements") as List<ParsedCondition>)?[1].ConnectorFromPrevious);
            Assert.AreEqual("OR", (records[1].GetField("SpawnRequirements") as List<ParsedCondition>)?[1].ConnectorFromPrevious);
            Assert.AreEqual("AND", (records[2].GetField("SpawnRequirements") as List<ParsedCondition>)?[1].ConnectorFromPrevious);
            Assert.AreEqual("OR", (records[3].GetField("SpawnRequirements") as List<ParsedCondition>)?[1].ConnectorFromPrevious);
            Assert.AreEqual("AND", (records[4].GetField("SpawnRequirements") as List<ParsedCondition>)?[1].ConnectorFromPrevious);
        }

        [Test]
        public void OptionalFieldsAllowEmptyValues()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Sword,Common,100,0.1,true,\"\"\n" +
                "item_002,Shield,Common,150,,false,\n" +
                "item_003,Potion,Common,50,0.0,true,\"level>=1\"";

            DataSchemaSO schema = CreateItemSchema();
            // All columns are optional by default (IsRequired = false)
            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            // All rows should be accepted since columns are optional
            Assert.AreEqual(3, records.Count);
            Assert.AreEqual("item_001", records[0].GetField("ItemID"));
            Assert.AreEqual("item_002", records[1].GetField("ItemID"));
            Assert.AreEqual("item_003", records[2].GetField("ItemID"));
        }

        [Test]
        public void RequiredFieldsRejectEmptyValues()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Sword,Common,100,0.1,true,\"power>5\"\n" +
                ",Shield,Common,150,0.0,false,\"armor>10\"\n" +
                "item_003,,Common,200,0.0,true,\"level>=1\"";

            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("ItemID", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("ItemName", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("Rarity", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Price", ColumnDataType.Int));
            schema.Columns.Add(new ColumnDefinition("CritChance", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsEnabled", ColumnDataType.Bool));
            schema.Columns.Add(new ColumnDefinition("SpawnRequirements", ColumnDataType.ConditionList));

            LogAssert.Expect(LogType.Error, "CsvDataParser: Required column 'ItemID' is empty at row 3.");
            LogAssert.Expect(LogType.Warning, "CsvDataParser: Skipping row 3 due to missing required fields.");
            LogAssert.Expect(LogType.Error, "CsvDataParser: Required column 'ItemName' is empty at row 4.");
            LogAssert.Expect(LogType.Warning, "CsvDataParser: Skipping row 4 due to missing required fields.");

            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            // Only the first row should be accepted (rows 2 and 3 have empty required fields)
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("item_001", records[0].GetField("ItemID"));
        }

        [Test]
        public void RequiredConditionColumnEnforcesNonEmptyConditions()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Sword,Common,100,0.1,true,\"power>5\"\n" +
                "item_002,Shield,Common,150,0.0,false,\"\"\n" +
                "item_003,Potion,Common,50,0.0,true,\"level>=1\"";

            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("ItemID", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("ItemName", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("Rarity", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Price", ColumnDataType.Int));
            schema.Columns.Add(new ColumnDefinition("CritChance", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsEnabled", ColumnDataType.Bool));
            schema.Columns.Add(new ColumnDefinition("SpawnRequirements", ColumnDataType.ConditionList, true));

            LogAssert.Expect(LogType.Error, "CsvDataParser: Required column 'SpawnRequirements' is empty at row 3.");
            LogAssert.Expect(LogType.Warning, "CsvDataParser: Skipping row 3 due to missing required fields.");

            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            // Only rows 1 and 3 should be accepted (row 2 has empty required SpawnRequirements)
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("item_001", records[0].GetField("ItemID"));
            Assert.AreEqual("item_003", records[1].GetField("ItemID"));
        }

        [Test]
        public void MixedRequiredAndOptionalFields()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,Sword,,100,,true,\"\"\n" +
                "item_002,Shield,Rare,,0.5,true,\"armor>10\"\n" +
                "item_003,Potion,Common,50,,false,\"TRUE\"";

            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("ItemID", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("ItemName", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("Rarity", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Price", ColumnDataType.Int, true));
            schema.Columns.Add(new ColumnDefinition("CritChance", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsEnabled", ColumnDataType.Bool, true));
            schema.Columns.Add(new ColumnDefinition("SpawnRequirements", ColumnDataType.ConditionList));

            LogAssert.Expect(LogType.Warning, "CsvDataParser: Failed to parse 'CritChance' as Float at row 2. Value: ''. Defaulting to 0.");
            LogAssert.Expect(LogType.Error, "CsvDataParser: Required column 'Price' is empty at row 3.");
            LogAssert.Expect(LogType.Warning, "CsvDataParser: Skipping row 3 due to missing required fields.");
            LogAssert.Expect(LogType.Warning, "CsvDataParser: Failed to parse 'CritChance' as Float at row 4. Value: ''. Defaulting to 0.");

            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            // Only row 3 should be accepted
            // Row 1: missing required Price
            // Row 2: missing required ItemName (Shield is present but needs to check - it's present)
            // Actually row 2 has ItemName, Rarity, CritChance, so it should fail only if Price is required and missing
            // Let me reconsider: Row 1 has Price, Row 2 missing Price, Row 3 has Price
            // So only Rows 1 and 3 should work
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("item_001", records[0].GetField("ItemID"));
            Assert.AreEqual("item_003", records[1].GetField("ItemID"));
        }

        [Test]
        public void WhitespaceOnlyEmptyCellsTreatedAsEmpty()
        {
            const string csv = "ItemID,ItemName,Rarity,Price,CritChance,IsEnabled,SpawnRequirements\n" +
                "item_001,   ,Common,100,0.1,true,\"power>5\"\n" +
                "item_002,Shield,Common,150,0.0,false,\"   \"";

            DataSchemaSO schema = ScriptableObject.CreateInstance<DataSchemaSO>();
            schema.Columns.Add(new ColumnDefinition("ItemID", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("ItemName", ColumnDataType.String, true));
            schema.Columns.Add(new ColumnDefinition("Rarity", ColumnDataType.String));
            schema.Columns.Add(new ColumnDefinition("Price", ColumnDataType.Int));
            schema.Columns.Add(new ColumnDefinition("CritChance", ColumnDataType.Float));
            schema.Columns.Add(new ColumnDefinition("IsEnabled", ColumnDataType.Bool));
            schema.Columns.Add(new ColumnDefinition("SpawnRequirements", ColumnDataType.ConditionList, true));

            LogAssert.Expect(LogType.Error, "CsvDataParser: Required column 'ItemName' is empty at row 2.");
            LogAssert.Expect(LogType.Warning, "CsvDataParser: Skipping row 2 due to missing required fields.");
            LogAssert.Expect(LogType.Error, "CsvDataParser: Required column 'SpawnRequirements' is empty at row 3.");
            LogAssert.Expect(LogType.Warning, "CsvDataParser: Skipping row 3 due to missing required fields.");

            CsvDataParser dataParser = new CsvDataParser();
            List<DataRecord> records = dataParser.Parse(csv, schema);

            // Both rows fail: row 1 has whitespace ItemName (fails required check), row 2 has whitespace SpawnRequirements (fails required check)
            Assert.AreEqual(0, records.Count);
        }
    }
}
