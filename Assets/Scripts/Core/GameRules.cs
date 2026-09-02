using System;

namespace MiracleDiamond
{
    public enum PlateResult
    {
        Strike,
        Ball,
        Foul,
        Out,
        Single,
        Double,
        Triple,
        HomeRun
    }

    public enum PitchType
    {
        Fastball,
        Curve,
        Changeup
    }

    public struct PlayResolution
    {
        public PlateResult Result;
        public int RunsScored;
        public bool PlateEnded;
        public bool HalfEnded;
        public bool GameEnded;
    }

    public sealed class GameRules
    {
        public const int RegulationInnings = 3;
        public const int MaximumInnings = 5;
        public const int MaximumSpirit = 100;
        public const int RivalCount = 3;
        public const string ProductVersion = "0.1.0";

        private readonly bool[] bases = new bool[3];

        public int Inning { get; private set; }
        public bool IsTop { get; private set; }
        public int VisitorRuns { get; private set; }
        public int HomeRuns { get; private set; }
        public int Outs { get; private set; }
        public int Balls { get; private set; }
        public int Strikes { get; private set; }
        public int PlayerSpirit { get; private set; }
        public bool GameOver { get; private set; }
        public bool IsDraw { get; private set; }

        public bool RunnerOnFirst { get { return bases[0]; } }
        public bool RunnerOnSecond { get { return bases[1]; } }
        public bool RunnerOnThird { get { return bases[2]; } }
        public bool PlayerIsBatting { get { return !IsTop; } }

        public GameRules()
        {
            Reset();
        }

        public void Reset()
        {
            Inning = 1;
            IsTop = true;
            VisitorRuns = 0;
            HomeRuns = 0;
            Outs = 0;
            Balls = 0;
            Strikes = 0;
            PlayerSpirit = 0;
            GameOver = false;
            IsDraw = false;
            ClearBases();
        }

        public void GainSpirit(int amount)
        {
            PlayerSpirit = Math.Max(0, Math.Min(MaximumSpirit, PlayerSpirit + amount));
        }

        public bool TrySpendSpirit()
        {
            if (PlayerSpirit < MaximumSpirit)
            {
                return false;
            }

            PlayerSpirit = 0;
            return true;
        }

        public static int NormalizeRivalStage(int stage)
        {
            return ((stage % RivalCount) + RivalCount) % RivalCount;
        }

        public static int NextRivalStage(int stage, bool won)
        {
            stage = NormalizeRivalStage(stage);
            return won ? (stage + 1) % RivalCount : stage;
        }

        public PlayResolution Apply(PlateResult result)
        {
            var resolution = new PlayResolution { Result = result };
            if (GameOver)
            {
                resolution.GameEnded = true;
                return resolution;
            }

            int inningBefore = Inning;
            bool topBefore = IsTop;
            int runsBefore = VisitorRuns + HomeRuns;

            switch (result)
            {
                case PlateResult.Strike:
                    Strikes++;
                    if (Strikes >= 3)
                    {
                        resolution.PlateEnded = true;
                        RecordOut();
                    }
                    break;
                case PlateResult.Ball:
                    Balls++;
                    if (Balls >= 4)
                    {
                        resolution.PlateEnded = true;
                        WalkBatter();
                        ResetCount();
                    }
                    break;
                case PlateResult.Foul:
                    if (Strikes < 2)
                    {
                        Strikes++;
                    }
                    break;
                case PlateResult.Out:
                    resolution.PlateEnded = true;
                    RecordOut();
                    break;
                case PlateResult.Single:
                    resolution.PlateEnded = true;
                    AdvanceBatter(1);
                    ResetCount();
                    break;
                case PlateResult.Double:
                    resolution.PlateEnded = true;
                    AdvanceBatter(2);
                    ResetCount();
                    break;
                case PlateResult.Triple:
                    resolution.PlateEnded = true;
                    AdvanceBatter(3);
                    ResetCount();
                    break;
                case PlateResult.HomeRun:
                    resolution.PlateEnded = true;
                    AdvanceBatter(4);
                    ResetCount();
                    break;
            }

            resolution.RunsScored = VisitorRuns + HomeRuns - runsBefore;
            resolution.HalfEnded = inningBefore != Inning || topBefore != IsTop || GameOver;
            resolution.GameEnded = GameOver;
            return resolution;
        }

        private void RecordOut()
        {
            Outs++;
            ResetCount();
            if (Outs >= 3)
            {
                EndHalfInning();
            }
        }

        private void WalkBatter()
        {
            if (bases[0])
            {
                if (bases[1])
                {
                    if (bases[2])
                    {
                        ScoreRun();
                    }
                    bases[2] = true;
                }
                bases[1] = true;
            }
            bases[0] = true;
        }

        private void AdvanceBatter(int baseCount)
        {
            for (int baseIndex = 2; baseIndex >= 0; baseIndex--)
            {
                if (!bases[baseIndex])
                {
                    continue;
                }

                bases[baseIndex] = false;
                int destination = baseIndex + baseCount;
                if (destination >= 3)
                {
                    ScoreRun();
                    if (GameOver && baseCount < 4)
                    {
                        return;
                    }
                }
                else
                {
                    bases[destination] = true;
                }
            }

            if (baseCount >= 4)
            {
                ScoreRun();
            }
            else
            {
                bases[baseCount - 1] = true;
            }
        }

        private void ScoreRun()
        {
            if (IsTop)
            {
                VisitorRuns++;
            }
            else
            {
                HomeRuns++;
                if (Inning >= RegulationInnings && HomeRuns > VisitorRuns)
                {
                    GameOver = true;
                }
            }
        }

        private void EndHalfInning()
        {
            ClearBases();
            Outs = 0;
            ResetCount();

            if (IsTop)
            {
                if (Inning >= RegulationInnings && HomeRuns > VisitorRuns)
                {
                    GameOver = true;
                    return;
                }

                IsTop = false;
                return;
            }

            if (Inning >= RegulationInnings && VisitorRuns != HomeRuns)
            {
                GameOver = true;
                return;
            }

            if (Inning >= MaximumInnings)
            {
                GameOver = true;
                IsDraw = true;
                return;
            }

            Inning++;
            IsTop = true;
        }

        private void ResetCount()
        {
            Balls = 0;
            Strikes = 0;
        }

        private void ClearBases()
        {
            bases[0] = false;
            bases[1] = false;
            bases[2] = false;
        }
    }

    public static class PlayResolver
    {
        public static PlateResult ResolveBatting(
            float timingError,
            int laneDifference,
            bool batterSpecial,
            bool pitcherSpecial,
            float roll)
        {
            timingError = Clamp01(Math.Abs(timingError));
            laneDifference = Math.Abs(laneDifference);
            roll = Clamp01(roll);

            float window = 0.24f + (batterSpecial ? 0.16f : 0f) - (pitcherSpecial ? 0.07f : 0f);
            if (timingError > window)
            {
                return PlateResult.Strike;
            }

            if (laneDifference > 0 && !batterSpecial)
            {
                return roll < 0.55f ? PlateResult.Strike : PlateResult.Foul;
            }

            float quality = 1f - timingError / Math.Max(0.08f, window);
            quality -= laneDifference * (batterSpecial ? 0.12f : 0.32f);
            quality += batterSpecial ? 0.24f : 0f;
            quality -= pitcherSpecial ? 0.16f : 0f;
            quality = Clamp01(quality);

            float homeRun = 0.02f + quality * 0.16f + (batterSpecial ? 0.17f : 0f);
            float triple = 0.01f + quality * 0.035f;
            float doubleHit = 0.05f + quality * 0.17f;
            float single = 0.12f + quality * 0.28f;
            float foul = 0.08f + (1f - quality) * 0.18f;

            if (roll < homeRun) return PlateResult.HomeRun;
            if (roll < homeRun + triple) return PlateResult.Triple;
            if (roll < homeRun + triple + doubleHit) return PlateResult.Double;
            if (roll < homeRun + triple + doubleHit + single) return PlateResult.Single;
            if (roll < homeRun + triple + doubleHit + single + foul) return PlateResult.Foul;
            return PlateResult.Out;
        }

        public static PlateResult ResolvePitching(
            float releaseError,
            PitchType pitchType,
            int deception,
            bool pitcherSpecial,
            bool batterSpecial,
            float roll)
        {
            releaseError = Clamp01(Math.Abs(releaseError));
            deception = Math.Max(0, Math.Min(2, deception));
            roll = Clamp01(roll);

            float pitchBonus = pitchType == PitchType.Curve ? 0.04f :
                pitchType == PitchType.Changeup ? 0.02f : 0f;
            float accuracy = 1f - releaseError * 2f + pitchBonus;
            accuracy += deception * 0.06f - (deception == 0 ? 0.08f : 0f);
            accuracy += pitcherSpecial ? 0.30f : 0f;
            accuracy -= batterSpecial ? 0.24f : 0f;
            accuracy = Clamp01(accuracy);

            if (releaseError > 0.42f && roll < 0.58f)
            {
                return PlateResult.Ball;
            }

            float strike = 0.16f + accuracy * 0.34f;
            float fieldOut = 0.22f + accuracy * 0.22f;
            float single = 0.22f - accuracy * 0.08f;
            float doubleHit = 0.11f - accuracy * 0.04f;
            float homeRun = 0.08f - accuracy * 0.05f + (batterSpecial ? 0.14f : 0f);

            if (roll < strike) return PlateResult.Strike;
            if (roll < strike + fieldOut) return PlateResult.Out;
            if (roll < strike + fieldOut + single) return PlateResult.Single;
            if (roll < strike + fieldOut + single + doubleHit) return PlateResult.Double;
            if (roll < strike + fieldOut + single + doubleHit + homeRun) return PlateResult.HomeRun;
            return accuracy < 0.45f ? PlateResult.Ball : PlateResult.Foul;
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }
}
