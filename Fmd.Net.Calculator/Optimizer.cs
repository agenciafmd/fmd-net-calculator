using Fmd.Net.Calculator.Execution;
using Fmd.Net.Calculator.Operations;

namespace Fmd.Net.Calculator;

public class Optimizer(IExecutor executor)
{
    public Operation Optimize(Operation operation, IFunctionRegistry functionRegistry,
        IConstantRegistry constantRegistry)
    {
        if (!operation.DependsOnVariables && operation.IsIdempotent && operation.GetType() != typeof(IntegerConstant)
            && operation.GetType() != typeof(FloatingPointConstant))
        {
            decimal result = executor.Execute(operation, functionRegistry, constantRegistry);
            return new FloatingPointConstant(result);
        }

        if (operation.GetType() == typeof(Addition))
        {
            Addition addition = (Addition)operation;
            addition.Argument1 = Optimize(addition.Argument1, functionRegistry, constantRegistry);
            addition.Argument2 = Optimize(addition.Argument2, functionRegistry, constantRegistry);
        }
        else if (operation.GetType() == typeof(Subtraction))
        {
            Subtraction substraction = (Subtraction)operation;
            substraction.Argument1 = Optimize(substraction.Argument1, functionRegistry, constantRegistry);
            substraction.Argument2 = Optimize(substraction.Argument2, functionRegistry, constantRegistry);
        }
        else if (operation.GetType() == typeof(Multiplication))
        {
            Multiplication multiplication = (Multiplication)operation;
            multiplication.Argument1 = Optimize(multiplication.Argument1, functionRegistry, constantRegistry);
            multiplication.Argument2 = Optimize(multiplication.Argument2, functionRegistry, constantRegistry);

            if ((multiplication.Argument1.GetType() == typeof(FloatingPointConstant) &&
                 ((FloatingPointConstant)multiplication.Argument1).Value == 0.0M)
                || (multiplication.Argument2.GetType() == typeof(FloatingPointConstant) &&
                    ((FloatingPointConstant)multiplication.Argument2).Value == 0.0M))
            {
                return new FloatingPointConstant(0.0M);
            }
        }
        else if (operation.GetType() == typeof(Division))
        {
            Division division = (Division)operation;
            division.Dividend = Optimize(division.Dividend, functionRegistry, constantRegistry);
            division.Divisor = Optimize(division.Divisor, functionRegistry, constantRegistry);
        }
        else if (operation.GetType() == typeof(Exponentiation))
        {
            Exponentiation division = (Exponentiation)operation;
            division.Base = Optimize(division.Base, functionRegistry, constantRegistry);
            division.Exponent = Optimize(division.Exponent, functionRegistry, constantRegistry);
        }
        else if (operation.GetType() == typeof(Function))
        {
            Function function = (Function)operation;
            IList<Operation> arguments = function.Arguments
                .Select(a => Optimize(a, functionRegistry, constantRegistry)).ToList();
            function.Arguments = arguments;
        }

        return operation;
    }
}