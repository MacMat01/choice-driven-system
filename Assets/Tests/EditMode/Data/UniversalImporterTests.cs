using System.Collections.Generic;
using Data;
using NUnit.Framework;
namespace Tests.EditMode.Data
{
    public class UniversalImporterTests
    {

        [Test]
        public void ImportRawText_Csv_MapsRowsToExampleCardData()
        {
            const string csv = "Id,Name,Cost,IsLegendary,Attack\n" +
                "c001,\"Fire, Mage\",3,true,4.5\n" +
                "c002,Guardian,5,false,6";

            List<ExampleCardData> cards = UniversalImporter.ImportRawText<ExampleCardData>(csv, ".csv");

            Assert.AreEqual(2, cards.Count);
            Assert.AreEqual("c001", cards[0].Id);
            Assert.AreEqual("Fire, Mage", cards[0].Name);
            Assert.AreEqual(3, cards[0].Cost);
            Assert.IsTrue(cards[0].IsLegendary);
            Assert.AreEqual(4.5f, cards[0].Attack, 0.001f);

            Assert.AreEqual("c002", cards[1].Id);
            Assert.AreEqual("Guardian", cards[1].Name);
            Assert.AreEqual(5, cards[1].Cost);
            Assert.IsFalse(cards[1].IsLegendary);
            Assert.AreEqual(6f, cards[1].Attack, 0.001f);
        }

        [Test]
        public void ImportRawText_JsonArray_MapsItemsToExampleCardData()
        {
            const string json = "[" +
                "{\"Id\":\"c001\",\"Name\":\"Arcane Bolt\",\"Cost\":2,\"IsLegendary\":false,\"Attack\":3.25}," +
                "{\"Id\":\"c002\",\"Name\":\"Titan\",\"Cost\":8,\"IsLegendary\":true,\"Attack\":9}" +
                "]";

            List<ExampleCardData> cards = UniversalImporter.ImportRawText<ExampleCardData>(json, ".json");

            Assert.AreEqual(2, cards.Count);
            Assert.AreEqual("Arcane Bolt", cards[0].Name);
            Assert.IsTrue(cards[1].IsLegendary);
            Assert.AreEqual(9f, cards[1].Attack, 0.001f);
        }

        [Test]
        public void ImportRawText_Csv_IgnoresUnknownColumns_AndMapsKnownColumns()
        {
            const string csv = "Id,Name,Cost,IsLegendary,Attack,UnknownStat\n" +
                "c010,Sentinel,2,false,1.5,999";

            List<ExampleCardData> cards = UniversalImporter.ImportRawText<ExampleCardData>(csv, ".csv");

            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual("c010", cards[0].Id);
            Assert.AreEqual("Sentinel", cards[0].Name);
            Assert.AreEqual(2, cards[0].Cost);
            Assert.IsFalse(cards[0].IsLegendary);
            Assert.AreEqual(1.5f, cards[0].Attack, 0.001f);
        }

        [Test]
        public void ImportRawText_Csv_ParsesEscapedQuotesInQuotedValue()
        {
            const string csv = "Id,Name,Cost,IsLegendary,Attack\n" +
                "c011,\"He said \"\"Hi\"\"\",1,false,2";

            List<ExampleCardData> cards = UniversalImporter.ImportRawText<ExampleCardData>(csv, ".csv");

            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual("He said \"Hi\"", cards[0].Name);
        }

        [Test]
        public void ImportRawText_Csv_ConvertsBooleanFromZeroAndOne()
        {
            const string csv = "Id,Name,Cost,IsLegendary,Attack\n" +
                "c012,ZeroLegend,1,0,1\n" +
                "c013,OneLegend,1,1,1";

            List<ExampleCardData> cards = UniversalImporter.ImportRawText<ExampleCardData>(csv, ".csv");

            Assert.AreEqual(2, cards.Count);
            Assert.IsFalse(cards[0].IsLegendary);
            Assert.IsTrue(cards[1].IsLegendary);
        }

        [Test]
        public void ImportRawText_Csv_InvalidConversions_KeepDefaultsWithoutThrowing()
        {
            const string csv = "Id,Name,Cost,IsLegendary,Attack\n" +
                "c014,Invalids,notAnInt,notABool,notAFloat";

            List<ExampleCardData> cards = UniversalImporter.ImportRawText<ExampleCardData>(csv, ".csv");

            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual("c014", cards[0].Id);
            Assert.AreEqual("Invalids", cards[0].Name);
            Assert.AreEqual(0, cards[0].Cost);
            Assert.IsFalse(cards[0].IsLegendary);
            Assert.AreEqual(0f, cards[0].Attack, 0.001f);
        }

        [Test]
        public void ImportRawText_Csv_MapsWritablePropertiesViaReflection()
        {
            const string csv = "Name,Cost\n" +
                "PropertyMapped,7";

            List<ExamplePropertyData> data = UniversalImporter.ImportRawText<ExamplePropertyData>(csv, ".csv");

            Assert.AreEqual(1, data.Count);
            Assert.AreEqual("PropertyMapped", data[0].Name);
            Assert.AreEqual(7, data[0].Cost);
        }

        [Test]
        public void ImportRawText_JsonObject_MapsSingleItem()
        {
            const string json = "{\"Id\":\"c020\",\"Name\":\"Solo\",\"Cost\":4,\"IsLegendary\":false,\"Attack\":2.5}";

            List<ExampleCardData> cards = UniversalImporter.ImportRawText<ExampleCardData>(json, ".json");

            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual("c020", cards[0].Id);
            Assert.AreEqual("Solo", cards[0].Name);
            Assert.AreEqual(4, cards[0].Cost);
            Assert.IsFalse(cards[0].IsLegendary);
            Assert.AreEqual(2.5f, cards[0].Attack, 0.001f);
        }

        [Test]
        public void ImportRawText_WithMissingExtension_GuessesJson()
        {
            const string json = "[{\"Id\":\"c030\",\"Name\":\"Guessed\",\"Cost\":1,\"IsLegendary\":false,\"Attack\":1}]";

            List<ExampleCardData> cards = UniversalImporter.ImportRawText<ExampleCardData>(json, null);

            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual("Guessed", cards[0].Name);
        }

        [Test]
        public void ImportRawText_WithMissingExtension_GuessesCsv()
        {
            const string csv = "Id,Name,Cost,IsLegendary,Attack\n" +
                "c031,GuessedCsv,3,false,4";

            List<ExampleCardData> cards = UniversalImporter.ImportRawText<ExampleCardData>(csv, string.Empty);

            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual("GuessedCsv", cards[0].Name);
        }

        [Test]
        public void ImportRawText_WithUnsupportedExtension_ReturnsEmptyList()
        {
            const string rawText = "anything";

            List<ExampleCardData> cards = UniversalImporter.ImportRawText<ExampleCardData>(rawText, ".xml");

            Assert.IsNotNull(cards);
            Assert.AreEqual(0, cards.Count);
        }
        private sealed class ExamplePropertyData
        {
            public string Name { get; set; }
            public int Cost { get; set; }
        }
    }
}
