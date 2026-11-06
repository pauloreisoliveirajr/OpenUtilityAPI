namespace OpenUtilityAPI.Core;

public static class NumberWords
{
    private static readonly string[] EnglishSmall = "zero one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen nineteen".Split();
    private static readonly string[] EnglishTens = "twenty thirty forty fifty sixty seventy eighty ninety".Split();
    private static readonly string[] PortugueseSmall = "zero um dois três quatro cinco seis sete oito nove dez onze doze treze catorze quinze dezesseis dezessete dezoito dezenove".Split();
    private static readonly string[] PortugueseTens = "vinte trinta quarenta cinquenta sessenta setenta oitenta noventa".Split();
    private static readonly string[] PortugueseHundreds = "cento duzentos trezentos quatrocentos quinhentos seiscentos setecentos oitocentos novecentos".Split();

    public static string Convert(int number, string culture)
    {
        if (number is < -999999 or > 999999)
            throw new ArgumentOutOfRangeException(nameof(number));
        var portuguese = culture switch
        {
            "pt-BR" => true,
            "en-US" => false,
            _ => throw new ArgumentException("Use pt-BR or en-US.", nameof(culture))
        };
        var value = Math.Abs(number);
        var prefix = number < 0 ? (portuguese ? "menos " : "minus ") : "";
        if (value < 1000)
            return prefix + Group(value, portuguese);

        var thousands = value / 1000;
        var remainder = value % 1000;
        var first = portuguese && thousands == 1 ? "mil" :
            Group(thousands, portuguese) + (portuguese ? " mil" : " thousand");
        var separator = portuguese && (remainder < 100 || remainder % 100 == 0) ? " e " : " ";
        return prefix + first + (remainder == 0 ? "" : separator + Group(remainder, portuguese));
    }

    private static string Group(int value, bool portuguese)
    {
        var small = portuguese ? PortugueseSmall : EnglishSmall;
        if (value < 20)
            return small[value];
        if (value < 100)
        {
            var tens = (portuguese ? PortugueseTens : EnglishTens)[value / 10 - 2];
            return value % 10 == 0 ? tens : tens + (portuguese ? " e " : "-") + small[value % 10];
        }
        if (portuguese && value == 100)
            return "cem";

        var hundreds = portuguese ? PortugueseHundreds[value / 100 - 1] : small[value / 100] + " hundred";
        return value % 100 == 0 ? hundreds : hundreds + (portuguese ? " e " : " ") + Group(value % 100, portuguese);
    }
}
