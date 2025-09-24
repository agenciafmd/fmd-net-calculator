namespace Fmd.Net.Calculator.Operations;

public class Subtraction(DataType dataType, Operation argument1, Operation argument2)
    : Operation(dataType, argument1.DependsOnVariables || argument2.DependsOnVariables,
        argument1.IsIdempotent && argument2.IsIdempotent)
{
    public Operation Argument1 { get; internal set; } = argument1;
    public Operation Argument2 { get; internal set; } = argument2;
}