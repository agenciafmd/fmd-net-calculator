namespace Fmd.Net.Calculator.Operations;

public class UnaryMinus(DataType dataType, Operation argument)
    : Operation(dataType, argument.DependsOnVariables, argument.IsIdempotent)
{
    public Operation Argument { get; internal set; } = argument;
}