public class QualityRange
{
    public QualityType MinValue { get; protected set; }
    public QualityType MaxValue { get; protected set; }

    public bool Contains(QualityType value)
        => value >= MinValue && value <= MaxValue;

    public void Normalize()
    {
        if (MinValue > MaxValue)
            (MinValue, MaxValue) = (MaxValue, MinValue);
    }

    public static void Validate(ref QualityType minValue, ref QualityType maxValue)
    {
        if(minValue > maxValue)
            maxValue = minValue;
    }

    public QualityRange(QualityType minValue = default, QualityType maxValue = default)
    {
        MinValue = minValue;
        MaxValue = maxValue;
    }
}