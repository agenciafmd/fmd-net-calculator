namespace Fmd.Net.Calculator.Operations;

public class LessThan : Operation
{
    public LessThan(DataType dataType, Operation argument1, Operation argument2)
        : base(dataType, argument1.DependsOnVariables || argument2.DependsOnVariables, argument1.IsIdempotent && argument2.IsIdempotent)
    {
        Argument1 = argument1;
        Argument2 = argument2;
    }

    public Operation Argument1 { get; internal set; }
    public Operation Argument2 { get; internal set; }
}