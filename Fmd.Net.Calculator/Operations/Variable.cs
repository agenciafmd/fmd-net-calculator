namespace Fmd.Net.Calculator.Operations;

/// <summary>
/// Represents a variable in a mathematical formula.
/// </summary>
public class Variable(string name) : Operation(DataType.FloatingPoint, true, false)
{
    public string Name { get; private set; } = name;

    public override bool Equals(object obj)
    {
        Variable other = obj as Variable;
        if (other != null)
        {
            return Name.Equals(other.Name);
        }
        else
            return false;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}