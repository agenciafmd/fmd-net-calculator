
namespace Fmd.Net.Calculator.Operations;

public abstract class Operation(DataType dataType, bool dependsOnVariables, bool isIdempotent)
{
    public DataType DataType { get; private set; } = dataType;

    public bool DependsOnVariables { get; internal set; } = dependsOnVariables;

    public bool IsIdempotent { get; private set; } = isIdempotent;
}