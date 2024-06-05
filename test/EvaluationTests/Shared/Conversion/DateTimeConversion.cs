namespace EvaluationTests.Shared.Conversion;

public static class DateTimeConversion
{
    public static int ToCurrentAge(this DateTime startingDate)
    {
        var yearDifference = DateTime.Now.Year - startingDate.Year;
        if (DateTime.Now < startingDate.AddYears(yearDifference))
        {
            yearDifference--;
        }

        return yearDifference;
    }
}
