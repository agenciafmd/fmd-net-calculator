namespace Fmd.Net.Calculator.Execution;

public interface IConstantRegistry : IEnumerable<ConstantInfo>
{
    ConstantInfo GetConstantInfo(string constantName);
    bool IsConstantName(string constantName);
    void RegisterConstant(string constantName, decimal value);
    void RegisterConstant(string constantName, decimal value, bool isOverWritable);
}