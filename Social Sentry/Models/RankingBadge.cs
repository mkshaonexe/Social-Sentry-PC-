using System;
using System.Collections.Generic;
using System.Linq;

namespace Social_Sentry.Models
{
    public enum RankingBadgeType
    {
        CLOWN,
        NOOB,
        NOVICE,
        AVERAGE,
        ADVANCED,
        DISCIPLINED,
        SIGMA,
        CHAD,
        ABSOLUTE_CHAD,
        GIGA_CHAD,
        ABSOLUTE_GIGA_CHAD
    }

    public class RankingBadge
    {
        public RankingBadgeType Type { get; }
        public string Title { get; }
        public int DayThreshold { get; }
        public string ImagePath { get; }

        private RankingBadge(RankingBadgeType type, string title, int dayThreshold, string imageName)
        {
            Type = type;
            Title = title;
            DayThreshold = dayThreshold;
            ImagePath = $"/Images/Badges/{imageName}"; // Assuming images will be stored here
        }

        public static readonly List<RankingBadge> AllBadges = new List<RankingBadge>
        {
            new RankingBadge(RankingBadgeType.CLOWN, "Clown", 0, "badge_clown.png"),
            new RankingBadge(RankingBadgeType.NOOB, "Noob", 1, "badge_noob.png"),
            new RankingBadge(RankingBadgeType.NOVICE, "Novice", 3, "badge_novice.png"),
            new RankingBadge(RankingBadgeType.AVERAGE, "Average", 7, "badge_average.png"),
            new RankingBadge(RankingBadgeType.ADVANCED, "Advanced", 10, "badge_advanced.png"),
            new RankingBadge(RankingBadgeType.DISCIPLINED, "Disciplined", 14, "badge_disciplined.png"),
            new RankingBadge(RankingBadgeType.SIGMA, "Sigma", 30, "badge_sigma.png"),
            new RankingBadge(RankingBadgeType.CHAD, "Chad", 45, "badge_chad.png"),
            new RankingBadge(RankingBadgeType.ABSOLUTE_CHAD, "Absolute Chad", 60, "badge_absolute_chad.png"),
            new RankingBadge(RankingBadgeType.GIGA_CHAD, "Giga Chad", 120, "badge_giga_chad.png"),
            new RankingBadge(RankingBadgeType.ABSOLUTE_GIGA_CHAD, "Absolute Giga Chad", 365, "badge_absolute_giga_chad.png")
        };

        public static RankingBadge GetBadgeForDays(long days)
        {
            return AllBadges
                .Where(b => days >= b.DayThreshold)
                .OrderByDescending(b => b.DayThreshold)
                .FirstOrDefault() ?? AllBadges[0];
        }
    }
}
