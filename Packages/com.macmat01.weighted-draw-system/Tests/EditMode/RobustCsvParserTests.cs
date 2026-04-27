using System.Collections.Generic;
using Csv;
using NUnit.Framework;
namespace Tests.EditMode
{
    public class RobustCsvParserTests
    {
        [Test]
        public void Parse_WhenInputIsNullOrWhitespace_ReturnsEmptyRows()
        {
            RobustCsvParser parser = new RobustCsvParser();

            Assert.AreEqual(0, parser.Parse(null).Count);
            Assert.AreEqual(0, parser.Parse(string.Empty).Count);
            Assert.AreEqual(0, parser.Parse("   ").Count);
        }

        [Test]
        public void Parse_ParsesQuotedCommasAndEscapedQuotes()
        {
            const string csv = "Name,Quote\n\"Ada, Lovelace\",\"She said \"\"Hello\"\"\"";
            RobustCsvParser parser = new RobustCsvParser();

            IReadOnlyList<IReadOnlyList<string>> rows = parser.Parse(csv);

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(2, rows[0].Count);
            Assert.AreEqual("Ada, Lovelace", rows[1][0]);
            Assert.AreEqual("She said \"Hello\"", rows[1][1]);
        }

        [Test]
        public void Parse_ParsesMultilineQuotedCell()
        {
            const string csv = "Id,Description\n1,\"Line 1\nLine 2\"\n2,Done";
            RobustCsvParser parser = new RobustCsvParser();

            IReadOnlyList<IReadOnlyList<string>> rows = parser.Parse(csv);

            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual("Line 1\nLine 2", rows[1][1]);
            Assert.AreEqual("Done", rows[2][1]);
        }

        [Test]
        public void Parse_HandlesCrLfAndTrailingNewlineWithoutExtraRow()
        {
            const string csv = "A,B\r\n1,2\r\n3,4\r\n";
            RobustCsvParser parser = new RobustCsvParser();

            IReadOnlyList<IReadOnlyList<string>> rows = parser.Parse(csv);

            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual("3", rows[2][0]);
            Assert.AreEqual("4", rows[2][1]);
        }

        [Test]
        public void Parse_NormalizesLegacyWrappedRecordIntoMultipleColumns()
        {
            const string csv = "\"Id,Name,Weight\"\n\"1,Sword,10\"";
            RobustCsvParser parser = new RobustCsvParser();

            IReadOnlyList<IReadOnlyList<string>> rows = parser.Parse(csv);

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(3, rows[0].Count);
            Assert.AreEqual("Id", rows[0][0]);
            Assert.AreEqual("Name", rows[0][1]);
            Assert.AreEqual("Weight", rows[0][2]);
            Assert.AreEqual("1", rows[1][0]);
            Assert.AreEqual("Sword", rows[1][1]);
            Assert.AreEqual("10", rows[1][2]);
        }

        [Test]
        public void Parse_DoesNotApplyLegacyNormalizationToNormalSingleCell()
        {
            const string csv = "\"hello\"";
            RobustCsvParser parser = new RobustCsvParser();

            IReadOnlyList<IReadOnlyList<string>> rows = parser.Parse(csv);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(1, rows[0].Count);
            Assert.AreEqual("hello", rows[0][0]);
        }
    }
}
