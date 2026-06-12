public class QualityRange
{
    public QualityType MinValue;
    public QualityType MaxValue;

    public bool Contains(QualityType value)
        => value >= MinValue && value <= MaxValue;

    public void Normalize()
    {
        if (MinValue > MaxValue)
            (MinValue, MaxValue) = (MaxValue, MinValue);
    }
}