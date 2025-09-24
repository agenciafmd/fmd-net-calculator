using System.Globalization;
using Fmd.Net.Calculator.Execution;
using Fmd.Net.Calculator.Operations;
using Fmd.Net.Calculator.Tokenizer;
using Fmd.Net.Calculator.Util;

namespace Fmd.Net.Calculator;

public delegate TResult DynamicFunc<T, TResult>(params T[] values);

public class CalculationEngine
{
    private readonly IExecutor _executor;
    private readonly Optimizer _optimizer;
    private readonly CultureInfo _cultureInfo;
    private readonly MemoryCache<string, Func<IDictionary<string, decimal>, decimal>> _executionFormulaCache;
    private readonly bool _cacheEnabled;
    private readonly bool _optimizerEnabled;
    private readonly bool _caseSensitive;
    private readonly Random _random;

    public CalculationEngine()
        : this(new EngineOptions())
    {
    }

    public CalculationEngine(EngineOptions options)
    {
        _executionFormulaCache =
            new MemoryCache<string, Func<IDictionary<string, decimal>, decimal>>(options.CacheMaximumSize,
                options.CacheReductionSize);
        FunctionRegistry = new FunctionRegistry(false);
        ConstantRegistry = new ConstantRegistry(false);
        _cultureInfo = options.CultureInfo;
        _cacheEnabled = options.CacheEnabled;
        _optimizerEnabled = options.OptimizerEnabled;
        _caseSensitive = options.CaseSensitive;

        _random = new Random();

        if (options.ExecutionMode == ExecutionMode.Interpreted)
            _executor = new Interpreter(_caseSensitive);
        else if (options.ExecutionMode == ExecutionMode.Compiled)
            _executor = new DynamicCompiler(_caseSensitive);
        else
            throw new ArgumentException(string.Format("Unsupported execution mode \"{0}\".", options.ExecutionMode),
                "executionMode");

        _optimizer = new Optimizer(new Interpreter()); // We run the optimizer with the interpreter 

        if (options.DefaultConstants)
        {
            RegisterDefaultConstants();
        }

        if (options.DefaultFunctions)
        {
            RegisterDefaultFunctions();
        }
    }

    internal IFunctionRegistry FunctionRegistry { get; private set; }
    internal IConstantRegistry ConstantRegistry { get; private set; }

    public decimal Calculate(string formulaText, IDictionary<string, decimal> variables)
    {
        if (string.IsNullOrEmpty(formulaText))
            throw new ArgumentNullException("formulaText");

        if (variables == null)
            throw new ArgumentNullException("variables");

        if (!_caseSensitive)
        {
            variables = EngineUtil.ConvertVariableNamesToLowerCase(variables);
        }

        VerifyVariableNames(variables);

        foreach (ConstantInfo constant in ConstantRegistry)
            variables.Add(constant.ConstantName, constant.Value);

        if (IsInFormulaCache(formulaText, null, out var function))
        {
            return function(variables);
        }

        Operation operation = BuildAbstractSyntaxTree(formulaText, new ConstantRegistry(_caseSensitive));
        function = BuildFormula(formulaText, null, operation);
        return function(variables);
    }

    public Func<IDictionary<string, decimal>, decimal> Build(string formulaText, IDictionary<string, decimal> constants)
    {
        if (string.IsNullOrEmpty(formulaText))
        {
            throw new ArgumentNullException("formulaText");
        }


        ConstantRegistry compiledConstants = new ConstantRegistry(_caseSensitive);
        if (constants != null)
        {
            foreach (var constant in constants)
            {
                compiledConstants.RegisterConstant(constant.Key, constant.Value);
            }
        }

        if (IsInFormulaCache(formulaText, compiledConstants, out var result))
        {
            return result;
        }

        Operation operation = BuildAbstractSyntaxTree(formulaText, compiledConstants);
        return BuildFormula(formulaText, compiledConstants, operation);
    }

    private void RegisterDefaultFunctions()
    {
        FunctionRegistry.RegisterFunction("sin", (Func<decimal>)MathUtil.Sin, true, false);
        FunctionRegistry.RegisterFunction("cos", (Func<decimal>)MathUtil.Cos, true, false);
        FunctionRegistry.RegisterFunction("csc", (Func<decimal, decimal>)MathUtil.Csc, true, false);
        FunctionRegistry.RegisterFunction("sec", (Func<decimal, decimal>)MathUtil.Sec, true, false);
        FunctionRegistry.RegisterFunction("asin", (Func<decimal>)MathUtil.Asin, true, false);
        FunctionRegistry.RegisterFunction("acos", (Func<decimal>)MathUtil.Acos, true, false);
        FunctionRegistry.RegisterFunction("tan", (Func<decimal>)MathUtil.Tan, true, false);
        FunctionRegistry.RegisterFunction("cot", (Func<decimal, decimal>)MathUtil.Cot, true, false);
        FunctionRegistry.RegisterFunction("atan", (Func<decimal>)MathUtil.Atan, true, false);
        FunctionRegistry.RegisterFunction("acot", (Func<decimal, decimal>)MathUtil.Acot, true, false);
        FunctionRegistry.RegisterFunction("log10", (Func<decimal>)MathUtil.Log10, true, false);
        FunctionRegistry.RegisterFunction("sqrt", (Func<decimal>)MathUtil.Sqrt, true, false);
        FunctionRegistry.RegisterFunction("abs", (Func<decimal, decimal>)Math.Abs, true, false);
        FunctionRegistry.RegisterFunction("if",
            (Func<decimal, decimal, decimal, decimal>)((a, b, c) => (a != 0.0M ? b : c)),
            true, false);
        FunctionRegistry.RegisterFunction("ifless",
            (Func<decimal, decimal, decimal, decimal, decimal>)((a, b, c, d) => (a < b ? c : d)), true, false);
        FunctionRegistry.RegisterFunction("ifmore",
            (Func<decimal, decimal, decimal, decimal, decimal>)((a, b, c, d) => (a > b ? c : d)), true, false);
        FunctionRegistry.RegisterFunction("ifequal",
            (Func<decimal, decimal, decimal, decimal, decimal>)((a, b, c, d) => (a == b ? c : d)), true, false);
        FunctionRegistry.RegisterFunction("ceiling", (Func<decimal, decimal>)Math.Ceiling, true, false);
        FunctionRegistry.RegisterFunction("floor", (Func<decimal, decimal>)Math.Floor, true, false);
        FunctionRegistry.RegisterFunction("truncate", (Func<decimal, decimal>)Math.Truncate, true, false);
        FunctionRegistry.RegisterFunction("round", (Func<decimal, decimal>)Math.Round, true, false);

        // Dynamic based arguments Functions
        FunctionRegistry.RegisterFunction("max", (DynamicFunc<decimal, decimal>)((a) => a.Max()), true, false);
        FunctionRegistry.RegisterFunction("min", (DynamicFunc<decimal, decimal>)((a) => a.Min()), true, false);
        FunctionRegistry.RegisterFunction("avg", (DynamicFunc<decimal, decimal>)((a) => a.Average()), true, false);
        FunctionRegistry.RegisterFunction("median", (DynamicFunc<decimal, decimal>)((a) => MathExtended.Median(a)),
            true,
            false);

        // Non Idempotent Functions
        FunctionRegistry.RegisterFunction("random", (Func<decimal>)RandomUtil.NextDecimal, false, false);
    }

    private void RegisterDefaultConstants()
    {
        ConstantRegistry.RegisterConstant("e", decimal.Parse(Math.E.ToString()), false);
        ConstantRegistry.RegisterConstant("pi", decimal.Parse(Math.PI.ToString()), false);
    }

    private Operation BuildAbstractSyntaxTree(string formulaText, ConstantRegistry compiledConstants)
    {
        TokenReader tokenReader = new TokenReader(_cultureInfo);
        List<Token> tokens = tokenReader.Read(formulaText);

        AstBuilder astBuilder = new AstBuilder(FunctionRegistry, _caseSensitive, compiledConstants);
        Operation operation = astBuilder.Build(tokens);

        if (_optimizerEnabled)
        {
            return _optimizer.Optimize(operation, FunctionRegistry, ConstantRegistry);
        }

        return operation;
    }

    private Func<IDictionary<string, decimal>, decimal> BuildFormula(string formulaText,
        ConstantRegistry compiledConstants, Operation operation)
    {
        return _executionFormulaCache.GetOrAdd(GenerateFormulaCacheKey(formulaText, compiledConstants),
            v => _executor.BuildFormula(operation, FunctionRegistry, ConstantRegistry));
    }

    private bool IsInFormulaCache(string formulaText, ConstantRegistry compiledConstants,
        out Func<IDictionary<string, decimal>, decimal> function)
    {
        function = null;
        return _cacheEnabled &&
               _executionFormulaCache.TryGetValue(GenerateFormulaCacheKey(formulaText, compiledConstants), out function);
    }

    private string GenerateFormulaCacheKey(string formulaText, ConstantRegistry compiledConstants)
    {
        return compiledConstants != null && compiledConstants.Any()
            ? $"{formulaText}@{String.Join(",", compiledConstants?.Select(x => $"{x.ConstantName}:{x.Value}"))}"
            : formulaText;
    }


    internal void VerifyVariableNames(IDictionary<string, decimal> variables)
    {
        foreach (string variableName in variables.Keys)
        {
            if (ConstantRegistry.IsConstantName(variableName) &&
                !ConstantRegistry.GetConstantInfo(variableName).IsOverWritable)
                throw new ArgumentException(
                    string.Format("The name \"{0}\" is a reservered variable name that cannot be overwritten.",
                        variableName), "variables");

            if (FunctionRegistry.IsFunctionName(variableName))
                throw new ArgumentException(
                    string.Format("The name \"{0}\" is a function name. Parameters cannot have this name.",
                        variableName), "variables");
        }
    }
}