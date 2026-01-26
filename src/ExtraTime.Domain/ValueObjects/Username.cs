namespace ExtraTime.Domain.ValueObjects;

public sealed record Username
{
    public string Value { get; init; }

    public Username(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Username is required", nameof(value));

        if (value.Length < 3 || value.Length > 50)
            throw new ArgumentException("Username must be between 3 and 50 characters", nameof(value));

        Value = value;
    }

    public static implicit operator string(Username username) => username.Value;
    public static explicit operator Username(string value) => new(value);

    public override string ToString() => Value;
}
