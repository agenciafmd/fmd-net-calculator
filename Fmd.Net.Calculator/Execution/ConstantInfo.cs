namespace Fmd.Net.Calculator.Execution;

public class ConstantInfo(string constantName, decimal value, bool isOverWritable)
{
    public string ConstantName { get; private set; } = constantName;

    public decimal Value { get; private set; } = value;

    public bool IsOverWritable { get; set; } = isOverWritable;
}