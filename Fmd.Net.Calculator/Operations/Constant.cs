namespace Fmd.Net.Calculator.Operations;

public abstract class Constant<T> : Operation
{
    public Constant(DataType dataType, T value)
        : base(dataType, false, true)
    {
        Value = value;
    }

    public T Value { get; private set; }

    public override bool Equals(object obj)
    {
        Constant<T> other = obj as Constant<T>;
        if (other != null)
            return Value.Equals(other.Value);
        else
            return false;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}

public class IntegerConstant : Constant<int>
{
    public IntegerConstant(int value)
        : base(DataType.Integer, value)
    {
    }
}

public class FloatingPointConstant : Constant<decimal>
{
    public FloatingPointConstant(decimal value)
        : base(DataType.FloatingPoint, value)
    {
    }
}