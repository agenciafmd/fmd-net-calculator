using Fmd.Net.Calculator.Execution;

namespace Fmd.Net.Calculator;

public class FormulaContext(
    IDictionary<string, decimal> variables,
    IFunctionRegistry functionRegistry,
    IConstantRegistry constantRegistry
    )
{
    public IDictionary<string, decimal> Variables { get; private set; } = variables;

    public IFunctionRegistry FunctionRegistry { get; private set; } = functionRegistry;
    public IConstantRegistry ConstantRegistry { get; private set; } = constantRegistry;
}