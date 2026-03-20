namespace zako_issuetracker.GitHub;

public static class GitHubMapper
{
    public static IssueTag MapLabelsToTag(string[]? labels)
    {
        if (labels == null || labels.Length == 0)
            return IssueTag.Enhancement;

        foreach (var label in labels)
        {
            var lower = label.ToLowerInvariant();
            switch (lower)
            {
                case "bug":
                    return IssueTag.Bug;
                case "feature":
                case "feature-request":
                    return IssueTag.Feature;
                case "enhancement":
                    return IssueTag.Enhancement;
            }
        }

        return IssueTag.Enhancement;
    }

    public static IssueStatus MapState(string state)
    {
        return IssueStatus.Proposed;
    }
}
