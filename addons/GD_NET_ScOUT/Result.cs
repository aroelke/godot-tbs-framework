namespace GD_NET_ScOUT;

public readonly struct Result
{
    public static readonly Result Success = new(Type.Success);
    public static readonly Result Failed = new(Type.Failure);
    public static readonly Result Error = new(Type.Error);
    public static readonly Result Skipped = new(Type.Skipped);

    public Result(Type type, string? message = null)
    {
        Value = type;
        Message = message ?? string.Empty;
    }

    public Type Value { get; }
    public string Message { get; }

    public override string ToString()
    {
        return $"{Value}. {Message}";
    }

    public enum Type
    {
        Success = 0,
        Skipped = 1,
        Failure = 2,
        Error = 3
    }
}
