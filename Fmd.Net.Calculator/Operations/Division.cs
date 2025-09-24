namespace Fmd.Net.Calculator.Operations;

public class Division : Operation
{
    public Division(DataType dataType, Operation dividend, Operation divisor)
        : base(dataType, dividend.DependsOnVariables || divisor.DependsOnVariables, dividend.IsIdempotent && divisor.IsIdempotent)
    {
        Dividend = dividend;
        Divisor = divisor;
    }

    public Operation Dividend { get; internal set; }
    public Operation Divisor { get; internal set; }
}