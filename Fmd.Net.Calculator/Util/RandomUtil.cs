namespace Fmd.Net.Calculator.Util;

public static class RandomUtil
{
    public static decimal NextDecimal()
    {
        var random = new Random();
        var doubleValue = random.NextDouble();
        return decimal.Parse(doubleValue.ToString());
    }
}