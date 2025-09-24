namespace Fmd.Net.Calculator.Execution;

public class FunctionInfo(
    string functionName,
    int numberOfParameters,
    bool isIdempotent,
    bool isOverWritable,
    bool isDynamicFunc,
    Delegate function)
{
    public string FunctionName { get; private set; } = functionName;

    public int NumberOfParameters { get; private set; } = numberOfParameters;

    public bool IsOverWritable { get; set; } = isOverWritable;

    public bool IsIdempotent { get; set; } = isIdempotent;

    public bool IsDynamicFunc { get; private set; } = isDynamicFunc;

    public Delegate Function { get; private set; } = function;
}