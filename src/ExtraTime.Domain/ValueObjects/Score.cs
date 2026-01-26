namespace ExtraTime.Domain.ValueObjects;

public sealed record Score
{
    public int Value { get; init; }

    public Score(int value)
    {
        if (value < 0)
            throw new ArgumentException("Score cannot be negative", nameof(value));
        
        Value = value;
    }

    public static implicit operator int(Score score) => score.Value;
    public static explicit operator Score(int value) => new(value);

    public override string ToString() => Value.ToString();
}
