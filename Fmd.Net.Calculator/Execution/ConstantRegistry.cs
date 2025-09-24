using System.Collections;
using Fmd.Net.Calculator.Util;

namespace Fmd.Net.Calculator.Execution;

public class ConstantRegistry : IConstantRegistry
{
    private readonly bool _caseSensitive;
    private readonly Dictionary<string, ConstantInfo> _constants;

    public ConstantRegistry(bool caseSensitive)
    {
        _caseSensitive = caseSensitive;
        _constants = new Dictionary<string, ConstantInfo>();
    }

    public IEnumerator<ConstantInfo> GetEnumerator()
    {
        return _constants.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public ConstantInfo GetConstantInfo(string constantName)
    {
        if (string.IsNullOrEmpty(constantName))
            throw new ArgumentNullException(nameof(constantName));

        ConstantInfo constantInfo = null;
        return _constants.TryGetValue(ConvertConstantName(constantName), out constantInfo) ? constantInfo : null;
    }

    public bool IsConstantName(string constantName)
    {
        if (string.IsNullOrEmpty(constantName))
            throw new ArgumentNullException(nameof(constantName));

        return _constants.ContainsKey(ConvertConstantName(constantName));
    }

    public void RegisterConstant(string constantName, decimal value)
    {
        RegisterConstant(constantName, value, true);
    }

    public void RegisterConstant(string constantName, decimal value, bool isOverWritable)
    {
        if (string.IsNullOrEmpty(constantName))
            throw new ArgumentNullException(nameof(constantName));

        constantName = ConvertConstantName(constantName);

        if (_constants.ContainsKey(constantName) && !_constants[constantName].IsOverWritable)
        {
            string message = string.Format("The constant \"{0}\" cannot be overwriten.", constantName);
            throw new Exception(message);
        }

        ConstantInfo constantInfo = new ConstantInfo(constantName, value, isOverWritable);

        if (_constants.ContainsKey(constantName))
            _constants[constantName] = constantInfo;
        else
            _constants.Add(constantName, constantInfo);
    }

    private string ConvertConstantName(string constantName)
    {
        return _caseSensitive ? constantName : constantName.ToLowerFast();
    }
}