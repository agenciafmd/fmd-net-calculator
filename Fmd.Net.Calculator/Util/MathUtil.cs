namespace Fmd.Net.Calculator.Util;

public static class MathUtil
{
    public static decimal Sin()
    {
        var sin = Math.Sin;
        return decimal.Parse(sin.ToString());
    }

    public static decimal Cos()
    {
        var cos = Math.Cos;
        return decimal.Parse(cos.ToString());
    }

    public static decimal Asin()
    {
        var asin = Math.Asin;
        return decimal.Parse(asin.ToString());
    }

    public static decimal Acos()
    {
        var acos = Math.Acos;
        return decimal.Parse(acos.ToString());
    }

    public static decimal Tan()
    {
        var tan = Math.Tan;
        return decimal.Parse(tan.ToString());
    }

    public static decimal Atan()
    {
        var atan = Math.Atan;
        return decimal.Parse(atan.ToString());
    }

    public static decimal Log10()
    {
        var log10 = Math.Log10;
        return decimal.Parse(log10.ToString());
    }

    public static decimal Sqrt()
    {
        var sqrt = Math.Sqrt;
        return decimal.Parse(sqrt.ToString());
    }

    public static decimal Pow(decimal a, decimal b)
    {
        var result = Math.Pow(double.Parse(a.ToString()), double.Parse(b.ToString()));
        return decimal.Parse(result.ToString());
    }


    public static decimal Cot(decimal a)
    {
        var resultado = 1 / Math.Tan(double.Parse(a.ToString()));
        return decimal.Parse(resultado.ToString());
    }

    public static decimal Acot(decimal d)
    {
        var result = Math.Atan(1 / double.Parse(d.ToString()));
        return decimal.Parse(result.ToString());
    }

    public static decimal Csc(decimal a)
    {
        var result = 1 / Math.Sin(double.Parse(a.ToString()));
        return decimal.Parse(result.ToString());
    }

    public static decimal Sec(decimal d)
    {
        var result = 1 / Math.Cos(double.Parse(d.ToString()));
        return decimal.Parse(result.ToString());
    }
}