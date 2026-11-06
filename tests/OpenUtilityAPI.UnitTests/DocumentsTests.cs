using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.UnitTests;

public sealed class DocumentsTests
{
    [Theory]
    [InlineData("cpf", "529.982.247-25", true)]
    [InlineData("cpf", "52998224726", false)]
    [InlineData("cpf", "11111111111", false)]
    [InlineData("cnpj", "04.252.011/0001-10", true)]
    [InlineData("cnpj", "12.abc.345/01de-35", true)]
    [InlineData("cnpj", "12ABC34501DE36", false)]
    [InlineData("cnpj", "00000000000000", false)]
    [InlineData("pis", "120.44558.95-7", true)]
    [InlineData("pis", "12044558951", false)]
    [InlineData("pis", "00000000000", false)]
    public void ChecksDocuments(string type, string value, bool expected) =>
        Assert.Equal(expected, Documents.IsValid(type, value));

    [Fact]
    public void FormatsAlphaNumericCnpj() =>
        Assert.Equal("12.ABC.345/01DE-35", Documents.Format("cnpj", "12abc34501de35"));
}

