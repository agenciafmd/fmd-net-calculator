using Fmd.Net.Calculator.Execution;
using Fmd.Net.Calculator.Operations;
using Fmd.Net.Calculator.Tokenizer;
using Fmd.Net.Calculator.Util;

namespace Fmd.Net.Calculator;

public class AstBuilder
{
    private readonly IFunctionRegistry _functionRegistry;
    private readonly IConstantRegistry _localConstantRegistry;
    private readonly bool _caseSensitive;
    private readonly Dictionary<char, int> _operationPrecedence = new();
    private readonly Stack<Operation> _resultStack = new();
    private readonly Stack<Token> _operatorStack = new();
    private readonly Stack<int> _parameterCount = new();

    public AstBuilder(IFunctionRegistry functionRegistry, bool caseSensitive,
        IConstantRegistry compiledConstants = null)
    {
        if (functionRegistry == null)
            throw new ArgumentNullException("functionRegistry");

        _functionRegistry = functionRegistry;
        _localConstantRegistry = compiledConstants ?? new ConstantRegistry(caseSensitive);
        _caseSensitive = caseSensitive;

        _operationPrecedence.Add('(', 0);
        _operationPrecedence.Add('&', 1);
        _operationPrecedence.Add('|', 1);
        _operationPrecedence.Add('<', 2);
        _operationPrecedence.Add('>', 2);
        _operationPrecedence.Add('≤', 2);
        _operationPrecedence.Add('≥', 2);
        _operationPrecedence.Add('≠', 2);
        _operationPrecedence.Add('=', 2);
        _operationPrecedence.Add('+', 3);
        _operationPrecedence.Add('-', 3);
        _operationPrecedence.Add('*', 4);
        _operationPrecedence.Add('/', 4);
        _operationPrecedence.Add('%', 4);
        _operationPrecedence.Add('_', 6);
        _operationPrecedence.Add('^', 5);
    }

    public Operation Build(IList<Token> tokens)
    {
        _resultStack.Clear();
        _operatorStack.Clear();

        _parameterCount.Clear();

        foreach (Token token in tokens)
        {
            object value = token.Value;

            switch (token.TokenType)
            {
                case TokenType.Integer:
                    _resultStack.Push(new IntegerConstant((int)token.Value));
                    break;
                case TokenType.FloatingPoint:
                    _resultStack.Push(new FloatingPointConstant((decimal)token.Value));
                    break;
                case TokenType.Text:
                    if (_functionRegistry.IsFunctionName((string)token.Value))
                    {
                        _operatorStack.Push(token);
                        _parameterCount.Push(1);
                    }
                    else
                    {
                        string tokenValue = (string)token.Value;
                        if (_localConstantRegistry.IsConstantName(tokenValue))
                        {
                            _resultStack.Push(
                                new FloatingPointConstant(_localConstantRegistry.GetConstantInfo(tokenValue).Value));
                        }
                        else
                        {
                            if (!_caseSensitive)
                            {
                                tokenValue = tokenValue.ToLowerFast();
                            }

                            _resultStack.Push(new Variable(tokenValue));
                        }
                    }

                    break;
                case TokenType.LeftBracket:
                    _operatorStack.Push(token);
                    break;
                case TokenType.RightBracket:
                    PopOperations(true, token);
                    //parameterCount.Pop();
                    break;
                case TokenType.ArgumentSeparator:
                    PopOperations(false, token);
                    _parameterCount.Push(_parameterCount.Pop() + 1);
                    break;
                case TokenType.Operation:
                    Token operation1Token = token;
                    char operation1 = (char)operation1Token.Value;

                    while (_operatorStack.Count > 0 && (_operatorStack.Peek().TokenType == TokenType.Operation ||
                                                       _operatorStack.Peek().TokenType == TokenType.Text))
                    {
                        Token operation2Token = _operatorStack.Peek();
                        bool isFunctionOnTopOfStack = operation2Token.TokenType == TokenType.Text;

                        if (!isFunctionOnTopOfStack)
                        {
                            char operation2 = (char)operation2Token.Value;

                            if ((IsLeftAssociativeOperation(operation1) &&
                                 _operationPrecedence[operation1] <= _operationPrecedence[operation2]) ||
                                (_operationPrecedence[operation1] < _operationPrecedence[operation2]))
                            {
                                _operatorStack.Pop();
                                _resultStack.Push(ConvertOperation(operation2Token));
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            _operatorStack.Pop();
                            _resultStack.Push(ConvertFunction(operation2Token));
                        }
                    }

                    _operatorStack.Push(operation1Token);
                    break;
            }
        }

        PopOperations(false, null);

        VerifyResultStack();

        return _resultStack.First();
    }

    private void PopOperations(bool untillLeftBracket, Token? currentToken)
    {
        if (untillLeftBracket && !currentToken.HasValue)
            throw new ArgumentNullException("currentToken", "If the parameter \"untillLeftBracket\" is set to true, " +
                                                            "the parameter \"currentToken\" cannot be null.");

        while (_operatorStack.Count > 0 && _operatorStack.Peek().TokenType != TokenType.LeftBracket)
        {
            Token token = _operatorStack.Pop();

            switch (token.TokenType)
            {
                case TokenType.Operation:
                    _resultStack.Push(ConvertOperation(token));
                    break;
                case TokenType.Text:
                    _resultStack.Push(ConvertFunction(token));
                    break;
            }
        }

        if (untillLeftBracket)
        {
            if (_operatorStack.Count > 0 && _operatorStack.Peek().TokenType == TokenType.LeftBracket)
                _operatorStack.Pop();
            else
                throw new ParseException(string.Format("No matching left bracket found for the right " +
                                                       "bracket at position {0}.", currentToken.Value.StartPosition));
        }
        else
        {
            if (_operatorStack.Count > 0 && _operatorStack.Peek().TokenType == TokenType.LeftBracket
                                        && !(currentToken.HasValue &&
                                             currentToken.Value.TokenType == TokenType.ArgumentSeparator))
                throw new ParseException(string.Format("No matching right bracket found for the left " +
                                                       "bracket at position {0}.", _operatorStack.Peek().StartPosition));
        }
    }

    private Operation ConvertOperation(Token operationToken)
    {
        try
        {
            DataType dataType;
            Operation argument1;
            Operation argument2;
            Operation divisor;
            Operation divident;

            switch ((char)operationToken.Value)
            {
                case '+':
                    argument2 = _resultStack.Pop();
                    argument1 = _resultStack.Pop();
                    dataType = RequiredDataType(argument1, argument2);

                    return new Addition(dataType, argument1, argument2);
                case '-':
                    argument2 = _resultStack.Pop();
                    argument1 = _resultStack.Pop();
                    dataType = RequiredDataType(argument1, argument2);

                    return new Subtraction(dataType, argument1, argument2);
                case '*':
                    argument2 = _resultStack.Pop();
                    argument1 = _resultStack.Pop();
                    dataType = RequiredDataType(argument1, argument2);

                    return new Multiplication(dataType, argument1, argument2);
                case '/':
                    divisor = _resultStack.Pop();
                    divident = _resultStack.Pop();

                    return new Division(DataType.FloatingPoint, divident, divisor);
                case '%':
                    divisor = _resultStack.Pop();
                    divident = _resultStack.Pop();

                    return new Modulo(DataType.FloatingPoint, divident, divisor);
                case '_':
                    argument1 = _resultStack.Pop();

                    return new UnaryMinus(argument1.DataType, argument1);
                case '^':
                    Operation exponent = _resultStack.Pop();
                    Operation @base = _resultStack.Pop();

                    return new Exponentiation(DataType.FloatingPoint, @base, exponent);
                case '&':
                    argument2 = _resultStack.Pop();
                    argument1 = _resultStack.Pop();
                    dataType = RequiredDataType(argument1, argument2);

                    return new And(dataType, argument1, argument2);
                case '|':
                    argument2 = _resultStack.Pop();
                    argument1 = _resultStack.Pop();
                    dataType = RequiredDataType(argument1, argument2);

                    return new Or(dataType, argument1, argument2);
                case '<':
                    argument2 = _resultStack.Pop();
                    argument1 = _resultStack.Pop();
                    dataType = RequiredDataType(argument1, argument2);

                    return new LessThan(dataType, argument1, argument2);
                case '≤':
                    argument2 = _resultStack.Pop();
                    argument1 = _resultStack.Pop();
                    dataType = RequiredDataType(argument1, argument2);

                    return new LessOrEqualThan(dataType, argument1, argument2);
                case '>':
                    argument2 = _resultStack.Pop();
                    argument1 = _resultStack.Pop();
                    dataType = RequiredDataType(argument1, argument2);

                    return new GreaterThan(dataType, argument1, argument2);
                case '≥':
                    argument2 = _resultStack.Pop();
                    argument1 = _resultStack.Pop();
                    dataType = RequiredDataType(argument1, argument2);

                    return new GreaterOrEqualThan(dataType, argument1, argument2);
                case '=':
                    argument2 = _resultStack.Pop();
                    argument1 = _resultStack.Pop();
                    dataType = RequiredDataType(argument1, argument2);

                    return new Equal(dataType, argument1, argument2);
                case '≠':
                    argument2 = _resultStack.Pop();
                    argument1 = _resultStack.Pop();
                    dataType = RequiredDataType(argument1, argument2);

                    return new NotEqual(dataType, argument1, argument2);
                default:
                    throw new ArgumentException(string.Format("Unknown operation \"{0}\".", operationToken),
                        "operation");
            }
        }
        catch (InvalidOperationException)
        {
            // If we encounter a Stack empty issue this means there is a syntax issue in 
            // the mathematical formula
            throw new ParseException(string.Format(
                "There is a syntax issue for the operation \"{0}\" at position {1}. " +
                "The number of arguments does not match with what is expected.", operationToken.Value,
                operationToken.StartPosition));
        }
    }

    private Operation ConvertFunction(Token functionToken)
    {
        try
        {
            string functionName = ((string)functionToken.Value).ToLowerInvariant();

            if (_functionRegistry.IsFunctionName(functionName))
            {
                FunctionInfo functionInfo = _functionRegistry.GetFunctionInfo(functionName);

                int numberOfParameters;

                if (functionInfo.IsDynamicFunc)
                {
                    numberOfParameters = _parameterCount.Pop();
                }
                else
                {
                    _parameterCount.Pop();
                    numberOfParameters = functionInfo.NumberOfParameters;
                }

                List<Operation> operations = new List<Operation>();
                for (int i = 0; i < numberOfParameters; i++)
                    operations.Add(_resultStack.Pop());
                operations.Reverse();

                return new Function(DataType.FloatingPoint, functionName, operations, functionInfo.IsIdempotent);
            }
            else
            {
                throw new ArgumentException(string.Format("Unknown function \"{0}\".", functionToken.Value),
                    "function");
            }
        }
        catch (InvalidOperationException)
        {
            // If we encounter a Stack empty issue this means there is a syntax issue in 
            // the mathematical formula
            throw new ParseException(string.Format(
                "There is a syntax issue for the function \"{0}\" at position {1}. " +
                "The number of arguments does not match with what is expected.", functionToken.Value,
                functionToken.StartPosition));
        }
    }

    private void VerifyResultStack()
    {
        if (_resultStack.Count > 1)
        {
            Operation[] operations = _resultStack.ToArray();

            for (int i = 1; i < operations.Length; i++)
            {
                Operation operation = operations[i];

                if (operation.GetType() == typeof(IntegerConstant))
                {
                    IntegerConstant constant = (IntegerConstant)operation;
                    throw new ParseException(
                        string.Format("Unexpected integer constant \"{0}\" found.", constant.Value));
                }
                else if (operation.GetType() == typeof(FloatingPointConstant))
                {
                    FloatingPointConstant constant = (FloatingPointConstant)operation;
                    throw new ParseException(string.Format("Unexpected floating point constant \"{0}\" found.",
                        constant.Value));
                }
            }

            throw new ParseException("The syntax of the provided formula is not valid.");
        }
    }

    private bool IsLeftAssociativeOperation(char character)
    {
        return character == '*' || character == '+' || character == '-' || character == '/';
    }

    private DataType RequiredDataType(Operation argument1, Operation argument2)
    {
        return (argument1.DataType == DataType.FloatingPoint || argument2.DataType == DataType.FloatingPoint)
            ? DataType.FloatingPoint
            : DataType.Integer;
    }
}