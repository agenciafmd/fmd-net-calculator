using System.Linq.Expressions;
using System.Reflection;
using Fmd.Net.Calculator.Operations;
using Fmd.Net.Calculator.Util;

namespace Fmd.Net.Calculator.Execution;

public class DynamicCompiler : IExecutor
{
    private readonly string _funcAssemblyQualifiedName;
    private readonly bool _caseSensitive;

    public DynamicCompiler() : this(false)
    {
    }

    public DynamicCompiler(bool caseSensitive)
    {
        _caseSensitive = caseSensitive;
        // The lower func reside in mscorelib, the higher ones in another assembly.
        // This is  an easy cross platform way to to have this AssemblyQualifiedName.
        _funcAssemblyQualifiedName =
            typeof(Func<decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal>)
                .GetTypeInfo()
                .Assembly.FullName;
    }

    public decimal Execute(Operation operation, IFunctionRegistry functionRegistry, IConstantRegistry constantRegistry)
    {
        return Execute(operation, functionRegistry, constantRegistry, new Dictionary<string, decimal>());
    }

    public decimal Execute(Operation operation, IFunctionRegistry functionRegistry, IConstantRegistry constantRegistry,
        IDictionary<string, decimal> variables)
    {
        return BuildFormula(operation, functionRegistry, constantRegistry)(variables);
    }

    public Func<IDictionary<string, decimal>, decimal> BuildFormula(Operation operation,
        IFunctionRegistry functionRegistry, IConstantRegistry constantRegistry)
    {
        Func<FormulaContext, decimal> func = BuildFormulaInternal(operation, functionRegistry);
        return _caseSensitive
            ? (Func<IDictionary<string, decimal>, decimal>)(variables =>
            {
                return func(new FormulaContext(variables, functionRegistry, constantRegistry));
            })
            : (Func<IDictionary<string, decimal>, decimal>)(variables =>
            {
                variables = EngineUtil.ConvertVariableNamesToLowerCase(variables);
                FormulaContext context = new FormulaContext(variables, functionRegistry, constantRegistry);
                return func(context);
            });
    }

    private Func<FormulaContext, decimal> BuildFormulaInternal(Operation operation,
        IFunctionRegistry functionRegistry)
    {
        ParameterExpression contextParameter = Expression.Parameter(typeof(FormulaContext), "context");

        Expression<Func<FormulaContext, decimal>> lambda = Expression.Lambda<Func<FormulaContext, decimal>>(
            GenerateMethodBody(operation, contextParameter, functionRegistry),
            contextParameter
        );
        return lambda.Compile();
    }


    private Expression GenerateMethodBody(Operation operation, ParameterExpression contextParameter,
        IFunctionRegistry functionRegistry)
    {
        if (operation == null)
            throw new ArgumentNullException("operation");

        if (operation.GetType() == typeof(IntegerConstant))
        {
            IntegerConstant constant = (IntegerConstant)operation;

            decimal value = constant.Value;
            return Expression.Constant(value, typeof(decimal));
        }

        if (operation.GetType() == typeof(FloatingPointConstant))
        {
            FloatingPointConstant constant = (FloatingPointConstant)operation;

            return Expression.Constant(constant.Value, typeof(decimal));
        }

        if (operation.GetType() == typeof(Variable))
        {
            Variable variable = (Variable)operation;

            Func<string, FormulaContext, decimal> getVariableValueOrThrow = PrecompiledMethods.GetVariableValueOrThrow;
            return Expression.Call(null,
                getVariableValueOrThrow.GetMethodInfo(),
                Expression.Constant(variable.Name),
                contextParameter);
        }

        if (operation.GetType() == typeof(Multiplication))
        {
            Multiplication multiplication = (Multiplication)operation;
            Expression argument1 = GenerateMethodBody(multiplication.Argument1, contextParameter, functionRegistry);
            Expression argument2 = GenerateMethodBody(multiplication.Argument2, contextParameter, functionRegistry);

            return Expression.Multiply(argument1, argument2);
        }

        if (operation.GetType() == typeof(Addition))
        {
            Addition addition = (Addition)operation;
            Expression argument1 = GenerateMethodBody(addition.Argument1, contextParameter, functionRegistry);
            Expression argument2 = GenerateMethodBody(addition.Argument2, contextParameter, functionRegistry);

            return Expression.Add(argument1, argument2);
        }

        if (operation.GetType() == typeof(Subtraction))
        {
            Subtraction addition = (Subtraction)operation;
            Expression argument1 = GenerateMethodBody(addition.Argument1, contextParameter, functionRegistry);
            Expression argument2 = GenerateMethodBody(addition.Argument2, contextParameter, functionRegistry);

            return Expression.Subtract(argument1, argument2);
        }

        if (operation.GetType() == typeof(Division))
        {
            Division division = (Division)operation;
            Expression dividend = GenerateMethodBody(division.Dividend, contextParameter, functionRegistry);
            Expression divisor = GenerateMethodBody(division.Divisor, contextParameter, functionRegistry);

            return Expression.Divide(dividend, divisor);
        }

        if (operation.GetType() == typeof(Modulo))
        {
            Modulo modulo = (Modulo)operation;
            Expression dividend = GenerateMethodBody(modulo.Dividend, contextParameter, functionRegistry);
            Expression divisor = GenerateMethodBody(modulo.Divisor, contextParameter, functionRegistry);

            return Expression.Modulo(dividend, divisor);
        }

        if (operation.GetType() == typeof(Exponentiation))
        {
            Exponentiation exponentation = (Exponentiation)operation;
            Expression @base = GenerateMethodBody(exponentation.Base, contextParameter, functionRegistry);
            Expression exponent = GenerateMethodBody(exponentation.Exponent, contextParameter, functionRegistry);

            return Expression.Call(null,
                typeof(Math).GetRuntimeMethod("Pow", new Type[] { typeof(decimal), typeof(decimal) }), @base, exponent);
        }

        if (operation.GetType() == typeof(UnaryMinus))
        {
            UnaryMinus unaryMinus = (UnaryMinus)operation;
            Expression argument = GenerateMethodBody(unaryMinus.Argument, contextParameter, functionRegistry);
            return Expression.Negate(argument);
        }

        if (operation.GetType() == typeof(And))
        {
            And and = (And)operation;
            Expression argument1 =
                Expression.NotEqual(GenerateMethodBody(and.Argument1, contextParameter, functionRegistry),
                    Expression.Constant(0.0));
            Expression argument2 =
                Expression.NotEqual(GenerateMethodBody(and.Argument2, contextParameter, functionRegistry),
                    Expression.Constant(0.0));

            return Expression.Condition(Expression.And(argument1, argument2),
                Expression.Constant(1.0),
                Expression.Constant(0.0));
        }

        if (operation.GetType() == typeof(Or))
        {
            Or and = (Or)operation;
            Expression argument1 =
                Expression.NotEqual(GenerateMethodBody(and.Argument1, contextParameter, functionRegistry),
                    Expression.Constant(0.0));
            Expression argument2 =
                Expression.NotEqual(GenerateMethodBody(and.Argument2, contextParameter, functionRegistry),
                    Expression.Constant(0.0));

            return Expression.Condition(Expression.Or(argument1, argument2),
                Expression.Constant(1.0),
                Expression.Constant(0.0));
        }

        if (operation.GetType() == typeof(LessThan))
        {
            LessThan lessThan = (LessThan)operation;
            Expression argument1 = GenerateMethodBody(lessThan.Argument1, contextParameter, functionRegistry);
            Expression argument2 = GenerateMethodBody(lessThan.Argument2, contextParameter, functionRegistry);

            return Expression.Condition(Expression.LessThan(argument1, argument2),
                Expression.Constant(1.0),
                Expression.Constant(0.0));
        }

        if (operation.GetType() == typeof(LessOrEqualThan))
        {
            LessOrEqualThan lessOrEqualThan = (LessOrEqualThan)operation;
            Expression argument1 = GenerateMethodBody(lessOrEqualThan.Argument1, contextParameter, functionRegistry);
            Expression argument2 = GenerateMethodBody(lessOrEqualThan.Argument2, contextParameter, functionRegistry);

            return Expression.Condition(Expression.LessThanOrEqual(argument1, argument2),
                Expression.Constant(1.0),
                Expression.Constant(0.0));
        }

        if (operation.GetType() == typeof(GreaterThan))
        {
            GreaterThan greaterThan = (GreaterThan)operation;
            Expression argument1 = GenerateMethodBody(greaterThan.Argument1, contextParameter, functionRegistry);
            Expression argument2 = GenerateMethodBody(greaterThan.Argument2, contextParameter, functionRegistry);

            return Expression.Condition(Expression.GreaterThan(argument1, argument2),
                Expression.Constant(1.0),
                Expression.Constant(0.0));
        }

        if (operation.GetType() == typeof(GreaterOrEqualThan))
        {
            GreaterOrEqualThan greaterOrEqualThan = (GreaterOrEqualThan)operation;
            Expression argument1 = GenerateMethodBody(greaterOrEqualThan.Argument1, contextParameter, functionRegistry);
            Expression argument2 = GenerateMethodBody(greaterOrEqualThan.Argument2, contextParameter, functionRegistry);

            return Expression.Condition(Expression.GreaterThanOrEqual(argument1, argument2),
                Expression.Constant(1.0),
                Expression.Constant(0.0));
        }

        if (operation.GetType() == typeof(Equal))
        {
            Equal equal = (Equal)operation;
            Expression argument1 = GenerateMethodBody(equal.Argument1, contextParameter, functionRegistry);
            Expression argument2 = GenerateMethodBody(equal.Argument2, contextParameter, functionRegistry);

            return Expression.Condition(Expression.Equal(argument1, argument2),
                Expression.Constant(1.0),
                Expression.Constant(0.0));
        }

        if (operation.GetType() == typeof(NotEqual))
        {
            NotEqual notEqual = (NotEqual)operation;
            Expression argument1 = GenerateMethodBody(notEqual.Argument1, contextParameter, functionRegistry);
            Expression argument2 = GenerateMethodBody(notEqual.Argument2, contextParameter, functionRegistry);

            return Expression.Condition(Expression.NotEqual(argument1, argument2),
                Expression.Constant(1.0),
                Expression.Constant(0.0));
        }

        if (operation.GetType() == typeof(Function))
        {
            Function function = (Function)operation;

            FunctionInfo functionInfo = functionRegistry.GetFunctionInfo(function.FunctionName);
            Type funcType;
            Type[] parameterTypes;
            Expression[] arguments;

            if (functionInfo.IsDynamicFunc)
            {
                funcType = typeof(DynamicFunc<decimal, decimal>);
                parameterTypes = new Type[] { typeof(decimal[]) };


                Expression[] arrayArguments = new Expression[function.Arguments.Count];
                for (int i = 0; i < function.Arguments.Count; i++)
                    arrayArguments[i] = GenerateMethodBody(function.Arguments[i], contextParameter, functionRegistry);

                arguments = new Expression[1];
                arguments[0] = NewArrayExpression.NewArrayInit(typeof(decimal), arrayArguments);
            }
            else
            {
                funcType = GetFuncType(functionInfo.NumberOfParameters);
                parameterTypes = (from i in Enumerable.Range(0, functionInfo.NumberOfParameters)
                    select typeof(decimal)).ToArray();

                arguments = new Expression[functionInfo.NumberOfParameters];
                for (int i = 0; i < functionInfo.NumberOfParameters; i++)
                    arguments[i] = GenerateMethodBody(function.Arguments[i], contextParameter, functionRegistry);
            }

            Expression getFunctionRegistry = Expression.Property(contextParameter, "FunctionRegistry");

            Expression funcInstance;
            if (!functionInfo.IsOverWritable)
            {
                funcInstance = Expression.Convert(
                    Expression.Property(
                        Expression.Call(
                            getFunctionRegistry,
                            typeof(IFunctionRegistry).GetRuntimeMethod("GetFunctionInfo",
                                new Type[] { typeof(string) }),
                            Expression.Constant(function.FunctionName)),
                        "Function"),
                    funcType);
            }
            else
                funcInstance = Expression.Constant(functionInfo.Function, funcType);

            return Expression.Call(
                funcInstance,
                funcType.GetRuntimeMethod("Invoke", parameterTypes),
                arguments);
        }

        throw new ArgumentException(string.Format("Unsupported operation \"{0}\".", operation.GetType().FullName),
            "operation");
    }

    private Type GetFuncType(int numberOfParameters)
    {
        string funcTypeName;
        if (numberOfParameters < 9)
            funcTypeName = string.Format("System.Func`{0}", numberOfParameters + 1);
        else
            funcTypeName = string.Format("System.Func`{0}, {1}", numberOfParameters + 1, _funcAssemblyQualifiedName);
        Type funcType = Type.GetType(funcTypeName);

        Type[] typeArguments = new Type[numberOfParameters + 1];
        for (int i = 0; i < typeArguments.Length; i++)
            typeArguments[i] = typeof(decimal);

        return funcType.MakeGenericType(typeArguments);
    }

    private static class PrecompiledMethods
    {
        public static decimal GetVariableValueOrThrow(string variableName, FormulaContext context)
        {
            if (context.Variables.TryGetValue(variableName, out decimal result))
                return result;
            else if (context.ConstantRegistry.IsConstantName(variableName))
                return context.ConstantRegistry.GetConstantInfo(variableName).Value;
            else
                throw new VariableNotDefinedException($"The variable \"{variableName}\" used is not defined.");
        }
    }
}