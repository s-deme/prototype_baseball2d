using System;
using MiracleDiamond;

internal static class Program
{
    private static void Main()
    {
        ThreeStrikesMakeAnOut();
        LoadedWalkScoresExactlyOne();
        HomeRunClearsAndScoresAllRunners();
        ThreeOutsChangeSides();
        BottomThirdLeadIsWalkOff();
        WalkOffSingleStopsAtWinningRun();
        ResolverHonorsTimingAndSpecial();
        PitchControlBendsLateAndRewardsTheCorners();
        SpecialsImproveExpectedOutcome();
        MixingPitchesImprovesExpectedOutcome();
        RandomGamesTerminateWithValidState();
        RivalLadderAdvancesOnlyOnWin();
        Console.WriteLine("Core smoke checks passed (12/12).");
    }

    private static void ThreeStrikesMakeAnOut()
    {
        var game = new GameRules();
        game.Apply(PlateResult.Strike);
        game.Apply(PlateResult.Strike);
        var result = game.Apply(PlateResult.Strike);
        Check(game.Outs == 1 && game.Strikes == 0 && result.PlateEnded, "strikeout");
    }

    private static void LoadedWalkScoresExactlyOne()
    {
        var game = new GameRules();
        game.Apply(PlateResult.Single);
        game.Apply(PlateResult.Single);
        game.Apply(PlateResult.Single);
        game.Apply(PlateResult.Ball);
        game.Apply(PlateResult.Ball);
        game.Apply(PlateResult.Ball);
        var result = game.Apply(PlateResult.Ball);
        Check(game.VisitorRuns == 1 && result.RunsScored == 1, "loaded walk");
    }

    private static void HomeRunClearsAndScoresAllRunners()
    {
        var game = new GameRules();
        game.Apply(PlateResult.Single);
        game.Apply(PlateResult.Double);
        var result = game.Apply(PlateResult.HomeRun);
        Check(game.VisitorRuns == 3 && result.RunsScored == 3, "home run scoring");
        Check(!game.RunnerOnFirst && !game.RunnerOnSecond && !game.RunnerOnThird, "home run clears bases");
    }

    private static void ThreeOutsChangeSides()
    {
        var game = new GameRules();
        game.Apply(PlateResult.Out);
        game.Apply(PlateResult.Out);
        var result = game.Apply(PlateResult.Out);
        Check(!game.IsTop && game.Inning == 1 && game.Outs == 0 && result.HalfEnded, "half inning");
    }

    private static void BottomThirdLeadIsWalkOff()
    {
        var game = new GameRules();
        for (int half = 0; half < 5; half++)
        {
            game.Apply(PlateResult.Out);
            game.Apply(PlateResult.Out);
            game.Apply(PlateResult.Out);
        }
        Check(game.Inning == 3 && !game.IsTop, "bottom third setup");
        var result = game.Apply(PlateResult.HomeRun);
        Check(game.GameOver && game.HomeRuns == 1 && result.GameEnded, "walk-off");
    }

    private static void ResolverHonorsTimingAndSpecial()
    {
        Check(PlayResolver.ResolveBatting(0.9f, 0, false, false, 0f) == PlateResult.Strike, "late swing");
        Check(PlayResolver.ResolveBatting(0.2f, 1, true, false, 0f) == PlateResult.HomeRun, "special swing");
        Check(IsHit(PlayResolver.ResolveBatting(0.02f, 0.1f, false, false, 0.5f)) &&
            PlayResolver.ResolveBatting(0.02f, 0.9f, false, false, 0.5f) == PlateResult.Strike,
            "continuous bat contact");
        Check(PlayResolver.ResolvePitching(0.5f, PitchType.Fastball, 0, false, false, 0.2f) == PlateResult.Ball, "wild pitch");
    }

    private static void PitchControlBendsLateAndRewardsTheCorners()
    {
        Check(Math.Abs(PlayResolver.PitchPosition(0f, 0.8f, 0.5f) - 0.2f) < 0.001f, "late pitch break");
        Check(PlayResolver.IsStrikeLocation(1f) && !PlayResolver.IsStrikeLocation(1.01f), "strike-zone edge");
        Check(PlayResolver.PitchControlError(0.72f) < PlayResolver.PitchControlError(0f), "corner pitching");
    }

    private static void WalkOffSingleStopsAtWinningRun()
    {
        var game = new GameRules();
        for (int half = 0; half < 5; half++)
        {
            game.Apply(PlateResult.Out);
            game.Apply(PlateResult.Out);
            game.Apply(PlateResult.Out);
        }
        game.Apply(PlateResult.Single);
        game.Apply(PlateResult.Single);
        game.Apply(PlateResult.Single);
        var result = game.Apply(PlateResult.Single);
        Check(game.HomeRuns == 1 && result.RunsScored == 1, "walk-off scoring stops");
    }

    private static void SpecialsImproveExpectedOutcome()
    {
        var random = new Random(42);
        int normalBatHits = 0;
        int specialBatHits = 0;
        int normalPitchHits = 0;
        int specialPitchHits = 0;
        for (int i = 0; i < 10000; i++)
        {
            float timing = (float)random.NextDouble() * 0.4f;
            int lane = random.Next(-1, 2);
            float batRoll = (float)random.NextDouble();
            if (IsHit(PlayResolver.ResolveBatting(timing, lane, false, false, batRoll))) normalBatHits++;
            if (IsHit(PlayResolver.ResolveBatting(timing, lane, true, false, batRoll))) specialBatHits++;

            float release = (float)random.NextDouble() * 0.5f;
            float pitchRoll = (float)random.NextDouble();
            if (IsHit(PlayResolver.ResolvePitching(release, PitchType.Curve, 1, false, false, pitchRoll))) normalPitchHits++;
            if (IsHit(PlayResolver.ResolvePitching(release, PitchType.Curve, 1, true, false, pitchRoll))) specialPitchHits++;
        }

        Check(specialBatHits > normalBatHits, "special swing improves hit rate");
        Check(specialPitchHits < normalPitchHits, "special pitch reduces hit rate");
    }

    private static void MixingPitchesImprovesExpectedOutcome()
    {
        var random = new Random(7);
        int repeatedHits = 0;
        int mixedHits = 0;
        for (int i = 0; i < 10000; i++)
        {
            float release = (float)random.NextDouble() * 0.42f;
            float roll = (float)random.NextDouble();
            if (IsHit(PlayResolver.ResolvePitching(release, PitchType.Fastball, 0, false, false, roll))) repeatedHits++;
            if (IsHit(PlayResolver.ResolvePitching(release, PitchType.Fastball, 2, false, false, roll))) mixedHits++;
        }
        Check(mixedHits < repeatedHits, "mixed pitch sequence reduces hit rate");
    }

    private static void RandomGamesTerminateWithValidState()
    {
        var random = new Random(99);
        PlateResult[] outcomes =
        {
            PlateResult.Strike, PlateResult.Strike, PlateResult.Strike,
            PlateResult.Ball, PlateResult.Foul, PlateResult.Out, PlateResult.Out,
            PlateResult.Single, PlateResult.Double, PlateResult.Triple, PlateResult.HomeRun
        };
        for (int gameIndex = 0; gameIndex < 500; gameIndex++)
        {
            var game = new GameRules();
            int playCount = 0;
            while (!game.GameOver && playCount++ < 2000)
            {
                game.Apply(outcomes[random.Next(outcomes.Length)]);
                Check(game.Inning >= 1 && game.Inning <= GameRules.MaximumInnings, "inning bounds");
                Check(game.Outs >= 0 && game.Outs < 3, "out bounds");
                Check(game.Balls >= 0 && game.Balls < 4, "ball bounds");
                Check(game.Strikes >= 0 && game.Strikes < 3, "strike bounds");
                Check(game.HomeRuns >= 0 && game.VisitorRuns >= 0, "score bounds");
            }
            Check(game.GameOver && playCount < 2000, "random game terminates");
        }
    }

    private static void RivalLadderAdvancesOnlyOnWin()
    {
        Check(GameRules.NextRivalStage(0, true) == 1, "rival advances on win");
        Check(GameRules.NextRivalStage(1, false) == 1, "rival holds on loss");
        Check(GameRules.NextRivalStage(2, true) == 0, "rival ladder wraps");
        Check(GameRules.NormalizeRivalStage(-1) == 2, "rival save normalization");
    }

    private static bool IsHit(PlateResult result)
    {
        return result == PlateResult.Single || result == PlateResult.Double ||
            result == PlateResult.Triple || result == PlateResult.HomeRun;
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Failed: " + name);
        }
    }
}
