namespace Fmd.Net.Calculator.Operations;

public class Function : Operation
{
    private IList<Operation> _arguments;

    public Function(DataType dataType, string functionName, IList<Operation> arguments, bool isIdempotent)
        : base(dataType, arguments.FirstOrDefault(o => o.DependsOnVariables) != null, isIdempotent && arguments.All(o => o.IsIdempotent))
    {
        FunctionName = functionName;
        _arguments = arguments;
    }

    public string FunctionName { get; private set; }

    public IList<Operation> Arguments {
        get
        {
            return _arguments;
        }
        internal set
        {
            _arguments = value;
            DependsOnVariables = _arguments.FirstOrDefault(o => o.DependsOnVariables) != null;
        }
    }
}