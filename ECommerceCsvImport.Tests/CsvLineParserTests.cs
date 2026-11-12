using ECommerceCsvImport.Core.Services;

namespace ECommerceCsvImport.Tests
{
    public class CsvLineParserTests
    {
        [Fact]
        public void Parse_SimpleLine_ReturnsCorrectFields()
        {
            var line = "1001,Ноутбук Lenovo IdeaPad 3,45990";
            var result = CsvLineParser.Parse(line);

            Assert.Equal(3, result.Count);
            Assert.Equal("1001", result[0]);
        }

        [Fact]
        public void Parse_EmptyFields_ReturnsEmptyStrings()
        {
            var line = "1011,Блок питания Corsair CV550,,4990";
            var result = CsvLineParser.Parse(line);

            Assert.Equal(4, result.Count);
            Assert.Equal("", result[2]);
        }

        [Fact]
        public void Parse_CommaInsideQuotes_DoesNotSplitField()
        {
            var line = "1012,\"Ноутбук, игровой\",Тестовая строка,59990";
            var result = CsvLineParser.Parse(line);

            Assert.Equal(4, result.Count);
            Assert.Equal("Ноутбук, игровой", result[1]);
        }

        [Fact]
        public void Parse_EscapedQuotes_ReturnsCorrectValue()
        {
            var line = "1013,\"Название \"\"Special\"\"\",Описание";
            var result = CsvLineParser.Parse(line);

            Assert.Equal(3, result.Count);
            Assert.Equal("Название \"Special\"", result[1]);
        }
    }
}
