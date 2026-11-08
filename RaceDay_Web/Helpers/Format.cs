using System.Globalization;

namespace RaceDay_Web.Helpers;

/// <summary>Small display helpers used by the views.</summary>
public static class Format
{
    public static string Currency(decimal amount) =>
        amount == 0 ? "Free" : "R" + amount.ToString("N2", CultureInfo.InvariantCulture);

    public static string Time(TimeSpan time) => time.ToString(@"hh\:mm\:ss");

    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        if (lastTwo >= 11 && lastTwo <= 13)
        {
            return number + "th";
        }

        return (number % 10) switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th"
        };
    }

    // For example "47th out of 312 finishers"
    public static string Position(int position, int totalFinishers)
    {
        if (totalFinishers <= 0)
        {
            return Ordinal(position);
        }

        return $"{Ordinal(position)} out of {totalFinishers} finisher{(totalFinishers == 1 ? "" : "s")}";
    }

    // The API stores "Active"; participants see it as "Confirmed".
    public static string StatusLabel(string status) => status == "Active" ? "Confirmed" : status;

    public static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = text.Trim();
        return text.Length <= maxLength ? text : text.Substring(0, maxLength).TrimEnd() + "...";
    }
}
