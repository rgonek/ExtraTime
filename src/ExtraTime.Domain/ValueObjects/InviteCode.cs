namespace ExtraTime.Domain.ValueObjects;

public sealed record InviteCode
{
    public string Value { get; init; }

    public InviteCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Invite code is required", nameof(value));

        if (value.Length < 6)
            throw new ArgumentException("Invite code must be at least 6 characters", nameof(value));

        Value = value.ToUpperInvariant();
    }

    public static implicit operator string(InviteCode code) => code.Value;
    public static explicit operator InviteCode(string value) => new(value);

    public override string ToString() => Value;
}
