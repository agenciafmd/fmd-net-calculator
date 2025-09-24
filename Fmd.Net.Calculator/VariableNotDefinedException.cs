namespace Fmd.Net.Calculator;

/// <summary>
/// An exception thrown when a formula must be executed with a variable that is not defined.
/// </summary>
public class VariableNotDefinedException : Exception
{
    public VariableNotDefinedException(string message)
        : base(message)
    {
    }
}