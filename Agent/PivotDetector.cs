namespace Wayfare.Agent;

public class PivotDetector : IPivotDetector
{
    private static readonly string[] _pivotPhrases =
    [
        "actually",
        "instead",
        "never mind",
        "nevermind",
        "forget about",
        "forget that",
        "scratch that",
        "drop that",
        "ignore that",
        "ignore previous",
        "disregard",
        "change of plan",
        "change plans",
        "change direction",
        "switch to",
        "switch gears",
        "let's switch",
        "let's pivot",
        "pivot to",
        "pivot away",
        "stop working on",
        "don't bother with",
        "no longer need",
        "rather than",
        "rather do",
        "instead of",
        "skip this",
        "skip that",
        "move on to",
        "let's move on",
        "on second thought",
        "on second thoughts",
        "something else",
        "start over",
        "start from scratch",
        "different task",
        "new direction"
    ];

    private static readonly string[] _leadingPivotPrefixes =
    [
        "wait,",
        "wait ",
        "wait!",
        "no,",
        "no ",
        "nah,",
        "nah ",
        "nope,",
        "nope ",
        "stop,",
        "stop ",
        "hold on,",
        "hold on "
    ];

    public bool IsPivot(string userInput)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput);

        string lower = userInput.ToLowerInvariant().Trim();

        foreach (string phrase in _pivotPhrases)
        {
            if (lower.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (string prefix in _leadingPivotPrefixes)
        {
            if (lower.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string remainder = lower[prefix.Length..].TrimStart();
                if (remainder.StartsWith("let's", StringComparison.OrdinalIgnoreCase) ||
                    remainder.StartsWith("lets", StringComparison.OrdinalIgnoreCase) ||
                    remainder.StartsWith("do", StringComparison.OrdinalIgnoreCase) ||
                    remainder.StartsWith("make", StringComparison.OrdinalIgnoreCase) ||
                    remainder.StartsWith("change", StringComparison.OrdinalIgnoreCase) ||
                    remainder.StartsWith("create", StringComparison.OrdinalIgnoreCase) ||
                    remainder.StartsWith("switch", StringComparison.OrdinalIgnoreCase) ||
                    remainder.StartsWith("just", StringComparison.OrdinalIgnoreCase) ||
                    remainder.StartsWith("focus", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
