using Fmd.Net.Calculator.Operations;

namespace Fmd.Net.Calculator.Execution;

public interface IExecutor
{
    decimal Execute(Operation operation, IFunctionRegistry functionRegistry, IConstantRegistry constantRegistry);

    decimal Execute(Operation operation, IFunctionRegistry functionRegistry, IConstantRegistry constantRegistry,
        IDictionary<string, decimal> variables);

    Func<IDictionary<string, decimal>, decimal> BuildFormula(Operation operation, IFunctionRegistry functionRegistry,
        IConstantRegistry constantRegistry);
}