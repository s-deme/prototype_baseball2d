using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MiracleDiamond
{
    public sealed class MiracleDiamondGame : MonoBehaviour
    {
        private const float VirtualWidth = 1280f;
        private const float VirtualHeight = 720f;
        private const string PrefPrefix = "miracle_diamond.";

        private enum ScreenMode
        {
            Title,
            HowTo,
            Options,
            Records,
            Playing,
            Paused,
            Results
        }

        private enum PlayMoment
        {
            Ready,
            BallInFlight,
            Result,
            HalfChange
        }

        private sealed class Spark
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float Size;
            public Color Color;
        }

        private static MiracleDiamondGame instance;

        private readonly List<Spark> sparks = new List<Spark>(96);
        private readonly System.Random random = new System.Random();
        private readonly KeyCode[] secretSequence =
        {
            KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.DownArrow,
            KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.B, KeyCode.A
        };
        private static readonly string[] RivalNames = { "HARBOR COMETS", "IRON FANGS", "PHANTOM OWLS" };
        private static readonly string[] RivalPitchSpecials = { "シャドーボール", "アイアンライザー", "ナイトメアカーブ" };
        private static readonly string[] RivalBatSpecials = { "流星打法", "鋼鉄打法", "冥界打法" };

        private ScreenMode screen = ScreenMode.Title;
        private ScreenMode optionsReturnScreen = ScreenMode.Title;
        private PlayMoment moment;
        private GameRules game;
        private GameAudio gameAudio;

        private Texture2D pixel;
        private Texture2D circle;
        private Texture2D softCircle;
        private Texture2D buttonNormal;
        private Texture2D buttonHover;
        private Texture2D buttonActive;
        private Texture2D buttonSelected;
        private Texture2D titleArt;
        private Font gameFont;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle centeredBodyStyle;
        private GUIStyle captionStyle;
        private GUIStyle tinyStyle;
        private GUIStyle scoreStyle;
        private GUIStyle buttonStyle;

        private int titleIndex;
        private int optionsIndex;
        private int pauseIndex;
        private int pendingPauseAction = -1;
        private int resultIndex;
        private PitchType pitchType;
        private PlateResult pendingResult;
        private PitchType lastPlayerPitchType;
        private float momentTime;
        private float ballProgress;
        private float playerPosition;
        private float pitchStartPosition;
        private float pitchBend;
        private float lastPlayerPitchPosition;
        private float swingTime;
        private float flashAlpha;
        private float shakeTime;
        private float shakeStrength;
        private float horizontalAxisLatch;
        private float verticalAxisLatch;
        private int secretProgress;
        private int aiSpirit;
        private int homeRunsThisGame;
        private int longestHit;
        private int pendingDeception;
        private int rivalStage;
        private bool playerSpecial;
        private bool aiSpecial;
        private bool hasPitchHistory;
        private bool moonMode;
        private bool fullScreenEnabled;
        private bool screenShakeEnabled;
        private bool reducedFlash;
        private int masterVolume;
        private string resultText = string.Empty;
        private string resultDetail = string.Empty;
        private string pendingDetail = string.Empty;
        private string toastText = string.Empty;
        private float toastTime;
        private float pendingPauseTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            var root = new GameObject("Miracle Diamond");
            instance = root.AddComponent<MiracleDiamondGame>();
            DontDestroyOnLoad(root);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            LoadSettings();
            CreateVisualResources();
            titleArt = Resources.Load<Texture2D>("TitleArt");
            gameAudio = gameObject.AddComponent<GameAudio>();
            gameAudio.SetVolume(masterVolume / 10f);
            string capturePath = CommandLineValue("-captureStoreScreens");
            if (!string.IsNullOrEmpty(capturePath))
            {
                StartCoroutine(CaptureStoreScreens(capturePath));
            }
        }

        private void OnDestroy()
        {
            Destroy(pixel);
            Destroy(circle);
            Destroy(softCircle);
            Destroy(buttonNormal);
            Destroy(buttonHover);
            Destroy(buttonActive);
            Destroy(buttonSelected);
        }

        private IEnumerator CaptureStoreScreens(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            Screen.SetResolution(1920, 1080, false);
            moonMode = false;
            screen = ScreenMode.Title;
            yield return CaptureFrame(Path.Combine(outputDirectory, "qa_01_title.png"));

            screen = ScreenMode.HowTo;
            yield return CaptureFrame(Path.Combine(outputDirectory, "qa_02_how_to.png"));

            rivalStage = 0;
            StartNewGame();
            moment = PlayMoment.Ready;
            playerPosition = 0.55f;
            yield return CaptureFrame(Path.Combine(outputDirectory, "store_01_pitching.png"));

            game.GainSpirit(GameRules.MaximumSpirit);
            game.TrySpendSpirit();
            playerSpecial = true;
            moment = PlayMoment.BallInFlight;
            ballProgress = 0.55f;
            toastText = "必殺・まぼろしフォーク！";
            toastTime = 10f;
            yield return CaptureFrame(Path.Combine(outputDirectory, "store_02_special_pitch.png"));

            playerSpecial = false;
            game.Apply(PlateResult.Out);
            game.Apply(PlateResult.Out);
            game.Apply(PlateResult.Out);
            BeginNextPlay();
            moment = PlayMoment.BallInFlight;
            ballProgress = 0.72f;
            toastTime = 0f;
            yield return CaptureFrame(Path.Combine(outputDirectory, "store_03_batting.png"));

            game.GainSpirit(GameRules.MaximumSpirit);
            game.TrySpendSpirit();
            playerSpecial = true;
            ballProgress = 0.82f;
            toastText = "必殺・彗星スイング！";
            toastTime = 10f;
            yield return CaptureFrame(Path.Combine(outputDirectory, "store_04_special_bat.png"));

            moonMode = true;
            playerSpecial = false;
            ballProgress = 0.66f;
            toastTime = 0f;
            yield return CaptureFrame(Path.Combine(outputDirectory, "store_05_moon_stadium.png"));

            game = new GameRules();
            for (int half = 0; half < 5; half++)
            {
                game.Apply(PlateResult.Out);
                game.Apply(PlateResult.Out);
                game.Apply(PlateResult.Out);
            }
            game.Apply(PlateResult.HomeRun);
            homeRunsThisGame = 1;
            longestHit = 132;
            screen = ScreenMode.Results;
            resultIndex = 0;
            yield return CaptureFrame(Path.Combine(outputDirectory, "qa_03_results.png"));
            Application.Quit();
        }

        private static IEnumerator CaptureFrame(string path)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(0.6f);
        }

        private static string CommandLineValue(string key)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], key, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }
            return string.Empty;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            UpdateEffects(delta);

            if (screen == ScreenMode.Title)
            {
                TrackSecretCode();
                UpdateMenu(ref titleIndex, 5, ActivateTitleItem);
                return;
            }

            if (screen == ScreenMode.HowTo || screen == ScreenMode.Records)
            {
                if (PressedConfirm() || PressedBack())
                {
                    gameAudio.Play(GameAudio.Cue.Back);
                    screen = ScreenMode.Title;
                }
                return;
            }

            if (screen == ScreenMode.Options)
            {
                UpdateOptions();
                return;
            }

            if (screen == ScreenMode.Paused)
            {
                UpdateMenu(ref pauseIndex, 4, ActivatePauseItem);
                if (PressedBack())
                {
                    ResumeGame();
                }
                return;
            }

            if (screen == ScreenMode.Results)
            {
                UpdateMenu(ref resultIndex, 2, ActivateResultItem);
                if (PressedBack())
                {
                    ReturnToTitle();
                }
                return;
            }

            if (screen != ScreenMode.Playing)
            {
                return;
            }

            if (PressedBack())
            {
                pauseIndex = 0;
                pendingPauseAction = -1;
                screen = ScreenMode.Paused;
                gameAudio.Play(GameAudio.Cue.Pause);
                return;
            }

            UpdateGameplay(delta);
        }

        private void UpdateGameplay(float delta)
        {
            momentTime += delta;

            bool canUseSpecial = moment == PlayMoment.Ready ||
                (game.PlayerIsBatting && moment == PlayMoment.BallInFlight);
            if (canUseSpecial && PressedSpecial() && !playerSpecial)
            {
                if (game.TrySpendSpirit())
                {
                    playerSpecial = true;
                    toastText = game.PlayerIsBatting ? "必殺・彗星スイング！" : "必殺・まぼろしフォーク！";
                    toastTime = 1.4f;
                    flashAlpha = reducedFlash ? 0.10f : 0.28f;
                    SpawnBurst(new Vector2(640f, 390f), Theme.Secondary, 36, 260f);
                    gameAudio.Play(GameAudio.Cue.Special);
                }
                else
                {
                    toastText = "スピリットが足りない！";
                    toastTime = 0.8f;
                    gameAudio.Play(GameAudio.Cue.Deny);
                }
            }

            if (moment == PlayMoment.Result)
            {
                if (momentTime >= 1.15f)
                {
                    BeginNextPlay();
                }
                return;
            }

            if (moment == PlayMoment.HalfChange)
            {
                if (momentTime >= 1.8f)
                {
                    BeginNextPlay();
                }
                return;
            }

            if (game.PlayerIsBatting)
            {
                UpdateBatting(delta);
            }
            else
            {
                UpdatePitching(delta);
            }
        }

        private void UpdateBatting(float delta)
        {
            playerPosition = Mathf.Clamp(playerPosition + ReadHorizontalAxis() * delta * 1.9f, -1.05f, 1.05f);

            if (moment == PlayMoment.Ready)
            {
                if (momentTime >= 0.72f)
                {
                    moment = PlayMoment.BallInFlight;
                    momentTime = 0f;
                    ballProgress = 0f;
                    gameAudio.Play(GameAudio.Cue.Pitch);
                }
                return;
            }

            float duration = (aiSpecial ? 0.68f : PitchDuration(pitchType)) * (1f - rivalStage * 0.06f);
            ballProgress += delta / duration;
            if (PressedConfirm())
            {
                float timingError = Mathf.Abs(ballProgress - 0.86f);
                float contactError = playerPosition - PlayResolver.PitchPosition(
                    pitchStartPosition, pitchBend, ballProgress);
                swingTime = 0.22f;
                PlateResult result = PlayResolver.ResolveBatting(
                    timingError,
                    contactError,
                    playerSpecial,
                    aiSpecial,
                    NextRoll());
                ResolvePlay(result, timingError <= 0.06f && Mathf.Abs(contactError) <= 0.18f
                    ? "ジャストミート！" : string.Empty);
            }
            else if (ballProgress > 1.08f)
            {
                float finalPosition = PlayResolver.PitchPosition(pitchStartPosition, pitchBend, 1f);
                ResolvePlay(PlayResolver.IsStrikeLocation(finalPosition) ? PlateResult.Strike : PlateResult.Ball,
                    PlayResolver.IsStrikeLocation(finalPosition) ? "見逃し" : "よく見た！");
            }
        }

        private void UpdatePitching(float delta)
        {
            if (moment == PlayMoment.Ready)
            {
                playerPosition = Mathf.Clamp(playerPosition + ReadHorizontalAxis() * delta * 1.55f, -0.82f, 0.82f);

                int vertical = ReadVerticalPress();
                if (vertical != 0)
                {
                    int next = ((int)pitchType + vertical + 3) % 3;
                    pitchType = (PitchType)next;
                    gameAudio.Play(GameAudio.Cue.Move);
                }

                if (PressedConfirm())
                {
                    pendingDeception = hasPitchHistory
                        ? (Mathf.Abs(playerPosition - lastPlayerPitchPosition) >= 0.35f ? 1 : 0) +
                            (pitchType != lastPlayerPitchType ? 1 : 0)
                        : 2;
                    pendingDetail = playerSpecial ? "消える落差！" :
                        pendingDeception == 0 ? "配球を読まれた！" :
                        pendingDeception == 2 ? "裏をかいた！" : string.Empty;
                    pitchStartPosition = playerPosition;
                    pitchBend = 0f;
                    lastPlayerPitchPosition = playerPosition;
                    lastPlayerPitchType = pitchType;
                    hasPitchHistory = true;
                    moment = PlayMoment.BallInFlight;
                    momentTime = 0f;
                    ballProgress = 0f;
                    gameAudio.Play(GameAudio.Cue.Pitch);
                }
                return;
            }

            float curveSpeed = pitchType == PitchType.Curve ? 1.35f :
                pitchType == PitchType.Changeup ? 1f : 0.72f;
            pitchBend += ReadHorizontalAxis() * delta * curveSpeed * (playerSpecial ? 1.7f : 1f);
            pitchBend = Mathf.Clamp(pitchBend, -1.8f, 1.8f);
            ballProgress += delta / (playerSpecial ? 0.48f : PitchDuration(pitchType) * 0.78f);
            if (ballProgress >= 1f)
            {
                float finalPosition = PlayResolver.PitchPosition(pitchStartPosition, pitchBend, 1f);
                pendingResult = PlayResolver.ResolvePitching(
                    PlayResolver.PitchControlError(finalPosition),
                    pitchType,
                    pendingDeception,
                    playerSpecial,
                    aiSpecial,
                    NextRoll());
                if (!PlayResolver.IsStrikeLocation(finalPosition))
                {
                    pendingDetail = pendingResult == PlateResult.Ball ? "見送られた" : "ボール球を振らせた！";
                }
                ResolvePlay(pendingResult, pendingDetail);
            }
        }

        private void ResolvePlay(PlateResult plateResult, string detail)
        {
            bool playerWasBatting = game.PlayerIsBatting;
            PlayResolution resolution = game.Apply(plateResult);
            resultText = ResultLabel(plateResult);
            resultDetail = detail;

            if (resolution.RunsScored > 0)
            {
                resultDetail = resolution.RunsScored == 1 ? "1点！" : resolution.RunsScored + "点追加！";
            }

            if (playerWasBatting)
            {
                GainPlayerBattingSpirit(plateResult);
                GainAiPitchingSpirit(plateResult);
                if (plateResult == PlateResult.HomeRun)
                {
                    homeRunsThisGame++;
                    longestHit = Math.Max(longestHit, 108 + random.Next(38));
                }
            }
            else
            {
                GainPlayerPitchingSpirit(plateResult);
                GainAiBattingSpirit(plateResult);
            }

            PlayResultEffects(plateResult, playerWasBatting);
            playerSpecial = false;
            aiSpecial = false;
            momentTime = 0f;

            if (resolution.GameEnded)
            {
                FinishGame();
                return;
            }

            moment = resolution.HalfEnded ? PlayMoment.HalfChange : PlayMoment.Result;
        }

        private void BeginNextPlay()
        {
            if (game.GameOver)
            {
                FinishGame();
                return;
            }

            moment = PlayMoment.Ready;
            momentTime = 0f;
            ballProgress = 0f;
            playerPosition = 0f;
            pitchType = (PitchType)random.Next(0, 3);
            pendingDetail = string.Empty;
            playerSpecial = false;
            aiSpecial = aiSpirit >= GameRules.MaximumSpirit;
            if (aiSpecial)
            {
                aiSpirit = 0;
                toastText = "ライバル必殺・" +
                    (game.PlayerIsBatting ? RivalPitchSpecials[rivalStage] : RivalBatSpecials[rivalStage]) + "！";
                toastTime = 1.25f;
                gameAudio.Play(GameAudio.Cue.Warning);
            }

            pitchStartPosition = NextRoll() * 1.5f - 0.75f;
            float breakDirection = NextRoll() < 0.5f ? -1f : 1f;
            pitchBend = pitchType == PitchType.Curve
                ? breakDirection * (0.38f + NextRoll() * 0.18f)
                : pitchType == PitchType.Changeup
                    ? breakDirection * (0.12f + NextRoll() * 0.12f)
                    : (NextRoll() - 0.5f) * 0.12f;
            if (aiSpecial)
            {
                pitchBend *= 1.45f;
            }
        }

        private void StartNewGame()
        {
            game = new GameRules();
            aiSpirit = 0;
            homeRunsThisGame = 0;
            longestHit = 0;
            hasPitchHistory = false;
            rivalStage = GameRules.NormalizeRivalStage(PlayerPrefs.GetInt(PrefPrefix + "rival_stage", 0));
            screen = ScreenMode.Playing;
            resultText = string.Empty;
            resultDetail = string.Empty;
            gameAudio.StartMusic();
            BeginNextPlay();
        }

        private void FinishGame()
        {
            SaveGameRecord();
            moment = PlayMoment.Result;
            screen = ScreenMode.Results;
            resultIndex = 0;
            gameAudio.StopMusic();
            gameAudio.Play(game.IsDraw ? GameAudio.Cue.Draw :
                game.HomeRuns > game.VisitorRuns ? GameAudio.Cue.Win : GameAudio.Cue.Lose);
            if (game.HomeRuns > game.VisitorRuns)
            {
                SpawnBurst(new Vector2(640f, 330f), Theme.Primary, 72, 420f);
            }
        }

        private void SaveGameRecord()
        {
            int games = PlayerPrefs.GetInt(PrefPrefix + "games", 0) + 1;
            int wins = PlayerPrefs.GetInt(PrefPrefix + "wins", 0);
            bool won = game.HomeRuns > game.VisitorRuns;
            int streak = won ? PlayerPrefs.GetInt(PrefPrefix + "win_streak", 0) + 1 : 0;
            if (won)
            {
                wins++;
            }

            PlayerPrefs.SetInt(PrefPrefix + "games", games);
            PlayerPrefs.SetInt(PrefPrefix + "wins", wins);
            PlayerPrefs.SetInt(PrefPrefix + "home_runs",
                PlayerPrefs.GetInt(PrefPrefix + "home_runs", 0) + homeRunsThisGame);
            PlayerPrefs.SetInt(PrefPrefix + "best_score",
                Math.Max(PlayerPrefs.GetInt(PrefPrefix + "best_score", 0), game.HomeRuns));
            PlayerPrefs.SetInt(PrefPrefix + "longest_hit",
                Math.Max(PlayerPrefs.GetInt(PrefPrefix + "longest_hit", 0), longestHit));
            PlayerPrefs.SetInt(PrefPrefix + "win_streak", streak);
            PlayerPrefs.SetInt(PrefPrefix + "best_streak",
                Math.Max(PlayerPrefs.GetInt(PrefPrefix + "best_streak", 0), streak));
            PlayerPrefs.SetInt(PrefPrefix + "rival_stage", GameRules.NextRivalStage(rivalStage, won));

            if (wins >= 3 && !moonMode)
            {
                UnlockMoonMode("3勝達成！ 月影スタジアム解放");
            }

            PlayerPrefs.Save();
        }

        private void GainPlayerBattingSpirit(PlateResult result)
        {
            int amount = result == PlateResult.HomeRun ? 32 :
                IsHit(result) ? 24 : result == PlateResult.Out ? 18 : 14;
            game.GainSpirit(amount);
        }

        private void GainPlayerPitchingSpirit(PlateResult result)
        {
            int amount = result == PlateResult.Out ? 24 :
                result == PlateResult.Strike ? 18 : IsHit(result) ? 16 : 12;
            game.GainSpirit(amount);
        }

        private void GainAiPitchingSpirit(PlateResult result)
        {
            int amount = IsHit(result) ? 24 : 15;
            aiSpirit = Mathf.Min(100, aiSpirit + Mathf.RoundToInt(amount * (1f + rivalStage * 0.15f)));
        }

        private void GainAiBattingSpirit(PlateResult result)
        {
            int amount = result == PlateResult.Out || result == PlateResult.Strike ? 22 : 14;
            aiSpirit = Mathf.Min(100, aiSpirit + Mathf.RoundToInt(amount * (1f + rivalStage * 0.15f)));
        }

        private void PlayResultEffects(PlateResult result, bool playerWasBatting)
        {
            Color burstColor = playerWasBatting ? Theme.Primary : Theme.Secondary;
            switch (result)
            {
                case PlateResult.HomeRun:
                    SpawnBurst(new Vector2(640f, 355f), burstColor, 64, 420f);
                    StartShake(0.34f, 13f);
                    flashAlpha = reducedFlash ? 0.12f : 0.34f;
                    gameAudio.Play(GameAudio.Cue.HomeRun);
                    break;
                case PlateResult.Triple:
                case PlateResult.Double:
                case PlateResult.Single:
                    SpawnBurst(new Vector2(640f, 490f), burstColor, 24, 220f);
                    StartShake(0.18f, 6f);
                    gameAudio.Play(GameAudio.Cue.Hit);
                    break;
                case PlateResult.Out:
                    gameAudio.Play(GameAudio.Cue.Out);
                    break;
                case PlateResult.Strike:
                    gameAudio.Play(GameAudio.Cue.Strike);
                    break;
                case PlateResult.Ball:
                    gameAudio.Play(GameAudio.Cue.Ball);
                    break;
                default:
                    gameAudio.Play(GameAudio.Cue.Foul);
                    break;
            }
        }

        private void UpdateEffects(float delta)
        {
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, delta * 1.8f);
            shakeTime = Mathf.Max(0f, shakeTime - delta);
            swingTime = Mathf.Max(0f, swingTime - delta);
            toastTime = Mathf.Max(0f, toastTime - delta);
            pendingPauseTime = Mathf.Max(0f, pendingPauseTime - delta);

            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                Spark spark = sparks[i];
                spark.Life -= delta;
                if (spark.Life <= 0f)
                {
                    sparks.RemoveAt(i);
                    continue;
                }

                spark.Position += spark.Velocity * delta;
                spark.Velocity += new Vector2(0f, 250f) * delta;
            }
        }

        private void SpawnBurst(Vector2 origin, Color color, int count, float speed)
        {
            while (sparks.Count > 120)
            {
                sparks.RemoveAt(0);
            }

            for (int i = 0; i < count; i++)
            {
                float angle = NextRoll() * Mathf.PI * 2f;
                float velocity = speed * (0.35f + NextRoll() * 0.65f);
                sparks.Add(new Spark
                {
                    Position = origin,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * velocity,
                    Life = 0.45f + NextRoll() * 0.65f,
                    Size = 4f + NextRoll() * 10f,
                    Color = Color.Lerp(color, Theme.OnSurface, NextRoll() * 0.45f)
                });
            }
        }

        private void StartShake(float duration, float strength)
        {
            if (!screenShakeEnabled)
            {
                return;
            }
            shakeTime = duration;
            shakeStrength = strength;
        }

        private void OnGUI()
        {
            if (pixel == null)
            {
                return;
            }

            Matrix4x4 original = GUI.matrix;
            Color originalColor = GUI.color;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), pixel);

            float scale = Mathf.Min(Screen.width / VirtualWidth, Screen.height / VirtualHeight);
            float xOffset = (Screen.width - VirtualWidth * scale) * 0.5f;
            float yOffset = (Screen.height - VirtualHeight * scale) * 0.5f;
            Vector2 shake = shakeTime > 0f
                ? UnityEngine.Random.insideUnitCircle * shakeStrength
                : Vector2.zero;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(xOffset + shake.x * scale, yOffset + shake.y * scale, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));
            GUI.color = Color.white;

            switch (screen)
            {
                case ScreenMode.Title: DrawTitle(); break;
                case ScreenMode.HowTo: DrawHowTo(); break;
                case ScreenMode.Options: DrawOptions(); break;
                case ScreenMode.Records: DrawRecords(false); break;
                case ScreenMode.Playing: DrawGame(); break;
                case ScreenMode.Paused: DrawGame(); DrawPause(); break;
                case ScreenMode.Results: DrawResults(); break;
            }

            DrawSparks();
            if (toastTime > 0f)
            {
                DrawToast();
            }
            if (flashAlpha > 0f)
            {
                DrawColorRect(new Rect(0f, 0f, VirtualWidth, VirtualHeight),
                    new Color(Theme.Primary.r, Theme.Primary.g, Theme.Primary.b, flashAlpha));
            }

            GUI.matrix = original;
            GUI.color = originalColor;
        }

        private void DrawTitle()
        {
            DrawBackdrop();
            if (titleArt != null)
            {
                Rect art = new Rect(492f, 205f, 740f, 416f);
                GUI.DrawTexture(art, titleArt, ScaleMode.ScaleAndCrop, true);
                DrawOutline(art, Theme.Outline, 3f);
            }
            else
            {
                DrawTitleDiamond();
            }

            GUI.Label(new Rect(88f, 70f, 800f, 88f), "ミラクル・ダイヤモンド", titleStyle);
            GUI.Label(new Rect(94f, 151f, 700f, 36f), "3回勝負！ 必殺アーケード野球", subtitleStyle);

            string[] labels = { "プレイ", "あそびかた", "設定", "戦績", "終了" };
            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(92f, 252f + i * 64f, 330f, 50f);
                if (DrawMenuButton(rect, labels[i], titleIndex == i))
                {
                    titleIndex = i;
                    ActivateTitleItem(i);
                }
            }

            string stadium = moonMode ? "MOONLIGHT STADIUM / 解放済み" : "SUNSET STADIUM";
            GUI.Label(new Rect(92f, 610f, 450f, 28f), stadium, captionStyle);
            int nextRival = GameRules.NormalizeRivalStage(PlayerPrefs.GetInt(PrefPrefix + "rival_stage", 0));
            GUI.Label(new Rect(492f, 625f, 740f, 28f), "NEXT RIVAL  /  " + RivalNames[nextRival], captionStyle);
            GUI.Label(new Rect(92f, 644f, 600f, 24f),
                "↑↓ 選択   Z / Enter / A 決定   Esc / B 戻る", tinyStyle);
            GUI.Label(new Rect(1050f, 675f, 190f, 22f), "VERSION " + GameRules.ProductVersion, tinyStyle);
        }

        private void DrawBackdrop()
        {
            DrawColorRect(new Rect(0f, 0f, VirtualWidth, VirtualHeight), Theme.Surface);
            DrawColorRect(new Rect(0f, 500f, VirtualWidth, 220f), Theme.SurfaceContainer);
            for (int i = 0; i < 24; i++)
            {
                float x = (i * 83f + 31f) % VirtualWidth;
                float y = 35f + (i * 47f) % 430f;
                DrawTexture(new Rect(x, y, 3f, 3f), circle, new Color(1f, 1f, 1f, 0.18f));
            }
        }

        private void DrawTitleDiamond()
        {
            Vector2 center = new Vector2(915f, 350f);
            DrawTexture(new Rect(center.x - 222f, center.y - 222f, 444f, 444f),
                circle, moonMode ? Theme.MoonGrass : Theme.GrassDark);
            DrawRotatedRect(new Rect(center.x - 157f, center.y - 157f, 314f, 314f),
                moonMode ? Theme.MoonDirt : Theme.Dirt, 45f);
            DrawRotatedRect(new Rect(center.x - 104f, center.y - 104f, 208f, 208f),
                moonMode ? Theme.MoonGrass : Theme.Grass, 45f);
            DrawBase(center + new Vector2(0f, 154f), true);
            DrawBase(center + new Vector2(154f, 0f), false);
            DrawBase(center + new Vector2(0f, -154f), false);
            DrawBase(center + new Vector2(-154f, 0f), false);
            DrawPlayer(center + new Vector2(-16f, 117f), Theme.Primary, false);
            DrawPlayer(center + new Vector2(0f, 10f), Theme.Secondary, true);
            GUI.Label(new Rect(720f, 566f, 400f, 48f), moonMode ? "SECRET TEAM ACTIVE" : "CLUTCH SPIRIT BASEBALL", headingStyle);
        }

        private void DrawHowTo()
        {
            DrawBackdrop();
            DrawPanel(new Rect(112f, 58f, 1056f, 604f), true);
            GUI.Label(new Rect(160f, 86f, 960f, 52f), "あそびかた", headingStyle);

            DrawInstructionCard(new Rect(160f, 160f, 456f, 205f), "攻撃 / バッティング",
                "← → で打者を自由に移動\n曲がる球筋を見て Z / A でスイング\n芯とタイミングが合うほど長打！",
                Theme.Primary);
            DrawInstructionCard(new Rect(664f, 160f, 456f, 205f), "守備 / ピッチング",
                "← → で投手を移動、↑ ↓ で球種を選択\nZ / A で投球後、← → でボールを曲げる\n同じ配球を続けると読まれやすい！",
                Theme.Secondary);
            DrawInstructionCard(new Rect(160f, 397f, 960f, 158f), "必殺技 / CLUTCH SPIRIT",
                "プレイでスピリットが増加。100になったら X / Y で発動。\n攻撃は『彗星スイング』、守備は『まぼろしフォーク』。\n3回終了時に同点なら最大5回まで延長。5回同点は引き分け。",
                Theme.Warning);

            if (DrawMenuButton(new Rect(490f, 583f, 300f, 48f), "戻る", true))
            {
                ReturnToTitle();
            }
        }

        private void DrawInstructionCard(Rect rect, string heading, string copy, Color accent)
        {
            DrawPanel(rect, false);
            DrawColorRect(new Rect(rect.x, rect.y, 8f, rect.height), accent);
            GUI.Label(new Rect(rect.x + 28f, rect.y + 20f, rect.width - 50f, 36f), heading, subtitleStyle);
            GUI.Label(new Rect(rect.x + 28f, rect.y + 70f, rect.width - 50f, rect.height - 84f), copy, bodyStyle);
        }

        private void DrawOptions()
        {
            DrawBackdrop();
            DrawPanel(new Rect(300f, 76f, 680f, 568f), true);
            GUI.Label(new Rect(348f, 105f, 584f, 54f), "設定", headingStyle);

            string[] labels =
            {
                "音量", "画面モード", "画面揺れ", "フラッシュ軽減", "戻る"
            };
            string[] values =
            {
                masterVolume + " / 10",
                fullScreenEnabled ? "フルスクリーン" : "ウィンドウ",
                screenShakeEnabled ? "ON" : "OFF",
                reducedFlash ? "ON" : "OFF",
                string.Empty
            };

            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(348f, 190f + i * 76f, 584f, 56f);
                if (DrawMenuButton(rect, labels[i] + (values[i].Length > 0 ? "    " + values[i] : string.Empty), optionsIndex == i))
                {
                    optionsIndex = i;
                    if (i == 4) CloseOptions(); else AdjustOption(i, 1);
                }
            }
            GUI.Label(new Rect(348f, 576f, 584f, 30f), "← → 変更   Esc / B 保存して戻る", centeredBodyStyle);
        }

        private void DrawRecords(bool compact)
        {
            if (!compact)
            {
                DrawBackdrop();
            }

            Rect panel = compact ? new Rect(168f, 215f, 944f, 250f) : new Rect(250f, 92f, 780f, 530f);
            DrawPanel(panel, true);
            int games = PlayerPrefs.GetInt(PrefPrefix + "games", 0);
            int wins = PlayerPrefs.GetInt(PrefPrefix + "wins", 0);
            int homeRuns = PlayerPrefs.GetInt(PrefPrefix + "home_runs", 0);
            int best = PlayerPrefs.GetInt(PrefPrefix + "best_score", 0);
            int longest = PlayerPrefs.GetInt(PrefPrefix + "longest_hit", 0);
            int bestStreak = PlayerPrefs.GetInt(PrefPrefix + "best_streak", 0);

            GUI.Label(new Rect(panel.x + 44f, panel.y + 28f, panel.width - 88f, 50f), "戦績", headingStyle);
            string record = "試合  " + games + "     勝利  " + wins + "     勝率  " +
                (games == 0 ? "---" : (wins * 100 / games) + "%") +
                "\n本塁打  " + homeRuns + "     最高得点  " + best + "     最長飛距離  " +
                (longest == 0 ? "---" : longest + "m") + "\n最高連勝  " + bestStreak;
            GUI.Label(new Rect(panel.x + 44f, panel.y + 102f, panel.width - 88f, 92f), record, centeredBodyStyle);

            if (!compact)
            {
                string secret = moonMode ? "★ 月影スタジアム / 月影ナイン 解放済み" : "？？？ 隠し要素 未解放";
                GUI.Label(new Rect(panel.x + 44f, panel.y + 238f, panel.width - 88f, 42f), secret, subtitleStyle);
                GUI.Label(new Rect(panel.x + 44f, panel.y + 340f, panel.width - 88f, 40f),
                    "3勝すると、夜のスタジアムと隠しチームが現れる。", centeredBodyStyle);
                if (DrawMenuButton(new Rect(panel.x + 240f, panel.y + 430f, 300f, 48f), "戻る", true))
                {
                    ReturnToTitle();
                }
            }
        }

        private void DrawGame()
        {
            DrawField();
            DrawScoreboard();
            DrawPlayersAndBall();
            DrawGameHud();

            if (moment == PlayMoment.Result)
            {
                DrawResultBanner();
            }
            else if (moment == PlayMoment.HalfChange)
            {
                DrawHalfBanner();
            }
        }

        private void DrawField()
        {
            DrawColorRect(new Rect(0f, 0f, VirtualWidth, VirtualHeight),
                moonMode ? Theme.MoonSky : Theme.Sky);
            DrawColorRect(new Rect(0f, 94f, VirtualWidth, 44f), Theme.CrowdDark);
            for (int i = 0; i < 34; i++)
            {
                Color crowd = i % 3 == 0 ? Theme.Primary : i % 3 == 1 ? Theme.Secondary : Theme.OnSurfaceVariant;
                DrawTexture(new Rect(22f + i * 38f, 103f + (i % 2) * 12f, 8f, 8f), circle, crowd);
            }
            DrawColorRect(new Rect(0f, 136f, VirtualWidth, 584f), moonMode ? Theme.MoonGrassDark : Theme.GrassDark);
            DrawTexture(new Rect(118f, 102f, 1044f, 780f), circle, moonMode ? Theme.MoonGrass : Theme.Grass);

            Vector2 home = new Vector2(640f, 635f);
            Vector2 first = new Vector2(855f, 430f);
            Vector2 second = new Vector2(640f, 246f);
            Vector2 third = new Vector2(425f, 430f);
            DrawLine(home, first, 10f, Theme.Chalk);
            DrawLine(first, second, 10f, Theme.Chalk);
            DrawLine(second, third, 10f, Theme.Chalk);
            DrawLine(third, home, 10f, Theme.Chalk);
            DrawRotatedRect(new Rect(520f, 310f, 240f, 240f), moonMode ? Theme.MoonDirt : Theme.Dirt, 45f);
            DrawTexture(new Rect(601f, 375f, 78f, 78f), circle, moonMode ? Theme.MoonDirtLight : Theme.DirtLight);
            DrawBase(first, game != null && game.RunnerOnFirst);
            DrawBase(second, game != null && game.RunnerOnSecond);
            DrawBase(third, game != null && game.RunnerOnThird);
            DrawBase(home, true);
        }

        private void DrawPlayersAndBall()
        {
            Color playerColor = moonMode ? Theme.MoonPlayer : Theme.Primary;
            Color rivalColor = Theme.Secondary;
            Color battingColor = game.PlayerIsBatting ? playerColor : rivalColor;
            Color fieldingColor = game.PlayerIsBatting ? rivalColor : playerColor;
            float pitcherPosition = game.PlayerIsBatting ? pitchStartPosition : playerPosition;
            float batterPosition = game.PlayerIsBatting ? playerPosition : 0f;
            Vector2 pitcher = new Vector2(640f + pitcherPosition * 88f, 382f);
            Vector2 batter = new Vector2(640f + batterPosition * 88f, 585f);
            DrawPlayer(pitcher, fieldingColor, true);
            float swing = swingTime > 0f ? 1f - swingTime / 0.22f : 0f;
            DrawPlayer(batter, battingColor, false, Mathf.Lerp(-18f, 74f, swing));
            DrawPlayer(new Vector2(855f, 400f), game.RunnerOnFirst ? battingColor : fieldingColor, true);
            DrawPlayer(new Vector2(640f, 215f), game.RunnerOnSecond ? battingColor : fieldingColor, true);
            DrawPlayer(new Vector2(425f, 400f), game.RunnerOnThird ? battingColor : fieldingColor, true);

            Color targetColor = playerSpecial ? Theme.Warning : Theme.Focus;
            DrawOutline(new Rect(552f, 516f, 176f, 82f), targetColor, 3f);

            if (moment == PlayMoment.BallInFlight)
            {
                float progress = Mathf.Clamp01(ballProgress);
                Vector2 ball = new Vector2(
                    640f + PlayResolver.PitchPosition(pitchStartPosition, pitchBend, progress) * 88f,
                    Mathf.Lerp(394f, 568f, progress));
                if (playerSpecial || aiSpecial)
                {
                    Color trail = playerSpecial ? Theme.Primary : Theme.Error;
                    for (int i = 1; i <= 5; i++)
                    {
                        float ghostProgress = Mathf.Clamp01(ballProgress - i * 0.045f);
                        Vector2 ghost = new Vector2(
                            640f + PlayResolver.PitchPosition(pitchStartPosition, pitchBend, ghostProgress) * 88f,
                            Mathf.Lerp(394f, 568f, ghostProgress));
                        DrawTexture(new Rect(ghost.x - 9f, ghost.y - 9f, 18f, 18f), softCircle,
                            new Color(trail.r, trail.g, trail.b, 0.42f - i * 0.05f));
                    }
                }
                DrawTexture(new Rect(ball.x - 8f, ball.y - 8f, 16f, 16f), circle, Theme.Ball);
            }
        }

        private void DrawScoreboard()
        {
            DrawPanel(new Rect(24f, 18f, 1232f, 74f), false);
            string away = RivalNames[rivalStage];
            string home = moonMode ? "MOON NINE" : "MIRACLES";
            GUI.Label(new Rect(48f, 27f, 250f, 28f), away + "  " + game.VisitorRuns, subtitleStyle);
            GUI.Label(new Rect(48f, 57f, 250f, 28f), home + "  " + game.HomeRuns, subtitleStyle);
            GUI.Label(new Rect(485f, 30f, 310f, 48f),
                (game.IsTop ? "▲" : "▼") + " " + game.Inning + "回", scoreStyle);

            GUI.Label(new Rect(860f, 25f, 360f, 30f),
                "B " + Dots(game.Balls, 3) + "   S " + Dots(game.Strikes, 2) + "   O " + Dots(game.Outs, 2), bodyStyle);
            GUI.Label(new Rect(860f, 57f, 360f, 24f),
                game.PlayerIsBatting ? "ATTACK / 攻撃" : "DEFENSE / 守備", tinyStyle);
        }

        private void DrawGameHud()
        {
            DrawPanel(new Rect(24f, 548f, 252f, 148f), false);
            GUI.Label(new Rect(44f, 565f, 212f, 26f), "CLUTCH SPIRIT", captionStyle);
            DrawMeter(new Rect(44f, 602f, 212f, 24f), game.PlayerSpirit / 100f,
                game.PlayerSpirit >= 100 ? Theme.Warning : Theme.Primary);
            GUI.Label(new Rect(44f, 635f, 212f, 44f),
                game.PlayerSpirit >= 100 ? "X / Y  必殺技発動！" : game.PlayerSpirit + " / 100", centeredBodyStyle);

            DrawPanel(new Rect(1004f, 548f, 252f, 148f), false);
            GUI.Label(new Rect(1024f, 565f, 212f, 26f), "RIVAL HEAT", captionStyle);
            DrawMeter(new Rect(1024f, 602f, 212f, 24f), aiSpirit / 100f,
                aiSpirit >= 100 ? Theme.Error : Theme.Secondary);
            GUI.Label(new Rect(1024f, 635f, 212f, 44f),
                aiSpirit >= 100 ? "必殺技に警戒！" : aiSpirit + " / 100", centeredBodyStyle);

            Rect actionPanel = new Rect(300f, 105f, 680f, 92f);
            DrawPanel(actionPanel, false);
            if (game.PlayerIsBatting)
            {
                string prompt = moment == PlayMoment.Ready ? "左右に動いて球を待て" : "球筋を見て打て！";
                GUI.Label(new Rect(320f, 111f, 640f, 35f), prompt, subtitleStyle);
                GUI.Label(new Rect(320f, 151f, 640f, 35f),
                    "← → 打者移動   Z / A スイング   X / Y 必殺", centeredBodyStyle);
            }
            else
            {
                GUI.Label(new Rect(320f, 111f, 640f, 32f),
                    moment == PlayMoment.Ready
                        ? PitchLabel(pitchType) + "   投手を動かしてコースを作れ"
                        : "投球後も ← → でボールを曲げろ！", subtitleStyle);
                GUI.Label(new Rect(320f, 151f, 640f, 30f),
                    moment == PlayMoment.Ready
                        ? "← → 投手移動   ↑ ↓ 球種   Z / A 投球"
                        : "← → 変化   球筋を端へ導け", centeredBodyStyle);
            }
        }

        private void DrawResultBanner()
        {
            DrawColorRect(new Rect(0f, 270f, VirtualWidth, 152f), new Color(0.02f, 0.07f, 0.11f, 0.90f));
            GUI.Label(new Rect(120f, 285f, 1040f, 72f), resultText, titleStyle);
            if (resultDetail.Length > 0)
            {
                GUI.Label(new Rect(120f, 365f, 1040f, 38f), resultDetail, subtitleStyle);
            }
        }

        private void DrawHalfBanner()
        {
            DrawColorRect(new Rect(0f, 260f, VirtualWidth, 180f), new Color(0.02f, 0.07f, 0.11f, 0.94f));
            string side = game.GameOver ? "試合終了" : game.IsTop ? "表 / 守備" : "裏 / 攻撃";
            GUI.Label(new Rect(120f, 282f, 1040f, 64f), game.Inning + "回 " + side, titleStyle);
            GUI.Label(new Rect(120f, 360f, 1040f, 40f),
                game.VisitorRuns + " － " + game.HomeRuns, scoreStyle);
        }

        private void DrawPause()
        {
            DrawColorRect(new Rect(0f, 0f, VirtualWidth, VirtualHeight), new Color(0f, 0f, 0f, 0.72f));
            DrawPanel(new Rect(390f, 150f, 500f, 420f), true);
            GUI.Label(new Rect(430f, 182f, 420f, 60f), "ポーズ", headingStyle);
            string[] labels = { "ゲームに戻る", "設定", "最初からやり直す", "タイトルへ" };
            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(450f, 250f + i * 68f, 380f, 54f);
                if (DrawMenuButton(rect, labels[i], pauseIndex == i))
                {
                    pauseIndex = i;
                    ActivatePauseItem(i);
                }
            }
        }

        private void DrawResults()
        {
            DrawBackdrop();
            bool win = game.HomeRuns > game.VisitorRuns;
            string verdict = game.IsDraw ? "DRAW GAME" : win ? "VICTORY!" : "GAME SET";
            Color verdictColor = game.IsDraw ? Theme.Warning : win ? Theme.Primary : Theme.OnSurface;
            DrawColorRect(new Rect(0f, 46f, VirtualWidth, 135f), Theme.SurfaceContainer);
            Color previous = titleStyle.normal.textColor;
            titleStyle.normal.textColor = verdictColor;
            GUI.Label(new Rect(100f, 55f, 1080f, 72f), verdict, titleStyle);
            titleStyle.normal.textColor = previous;
            GUI.Label(new Rect(100f, 126f, 1080f, 44f),
                RivalNames[rivalStage] + "  " + game.VisitorRuns + " － " + game.HomeRuns + "  " +
                (moonMode ? "MOON NINE" : "MIRACLES"), scoreStyle);

            DrawRecords(true);
            string[] labels = { "もう一度", "タイトルへ" };
            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(370f + i * 290f, 520f, 250f, 56f);
                if (DrawMenuButton(rect, labels[i], resultIndex == i))
                {
                    resultIndex = i;
                    ActivateResultItem(i);
                }
            }
            GUI.Label(new Rect(260f, 608f, 760f, 30f),
                "本塁打 " + homeRunsThisGame + "    最長飛距離 " +
                (longestHit == 0 ? "---" : longestHit + "m"), centeredBodyStyle);
        }

        private void DrawToast()
        {
            float width = Mathf.Clamp(headingStyle.CalcSize(new GUIContent(toastText)).x + 80f, 360f, 900f);
            Rect rect = new Rect((VirtualWidth - width) * 0.5f, 112f, width, 58f);
            DrawColorRect(rect, new Color(0.03f, 0.10f, 0.16f, 0.95f));
            DrawOutline(rect, Theme.Warning, 3f);
            GUI.Label(rect, toastText, subtitleStyle);
        }

        private void DrawSparks()
        {
            for (int i = 0; i < sparks.Count; i++)
            {
                Spark spark = sparks[i];
                Color color = spark.Color;
                color.a = Mathf.Clamp01(spark.Life * 2f);
                DrawTexture(new Rect(spark.Position.x - spark.Size * 0.5f,
                    spark.Position.y - spark.Size * 0.5f, spark.Size, spark.Size), circle, color);
            }
        }

        private void DrawPlayer(Vector2 position, Color uniform, bool fielder, float batAngle = -18f)
        {
            DrawTexture(new Rect(position.x - 19f, position.y - 19f, 38f, 38f), circle, Theme.Shadow);
            DrawTexture(new Rect(position.x - 15f, position.y - 15f, 30f, 30f), circle, uniform);
            DrawColorRect(new Rect(position.x - 13f, position.y - 4f, 26f, 10f), Theme.OnSurface);
            if (!fielder)
            {
                DrawRotatedRect(new Rect(position.x + 14f, position.y - 34f, 6f, 52f), Theme.Bat, batAngle);
            }
        }

        private void DrawBase(Vector2 center, bool occupied)
        {
            DrawRotatedRect(new Rect(center.x - 12f, center.y - 12f, 24f, 24f),
                occupied ? Theme.Warning : Theme.Base, 45f);
            DrawOutline(new Rect(center.x - 13f, center.y - 13f, 26f, 26f), Theme.Outline, 2f);
        }

        private void DrawMeter(Rect rect, float fill, Color color)
        {
            DrawColorRect(rect, Theme.Surface);
            DrawColorRect(new Rect(rect.x + 3f, rect.y + 3f, (rect.width - 6f) * Mathf.Clamp01(fill), rect.height - 6f), color);
            DrawOutline(rect, Theme.Outline, 2f);
        }

        private bool DrawMenuButton(Rect rect, string text, bool selected)
        {
            buttonStyle.normal.background = selected ? buttonSelected : buttonNormal;
            bool clicked = GUI.Button(rect, text, buttonStyle);
            if (selected)
            {
                DrawOutline(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), Theme.Focus, 3f);
                DrawColorRect(new Rect(rect.x + 12f, rect.y + rect.height * 0.5f - 4f, 8f, 8f), Theme.Primary);
            }
            return clicked;
        }

        private void DrawPanel(Rect rect, bool elevated)
        {
            if (elevated)
            {
                DrawColorRect(new Rect(rect.x + 10f, rect.y + 12f, rect.width, rect.height), Theme.Shadow);
            }
            DrawColorRect(rect, Theme.SurfaceContainer);
            DrawOutline(rect, Theme.Outline, 2f);
        }

        private void DrawOutline(Rect rect, Color color, float width)
        {
            DrawColorRect(new Rect(rect.x, rect.y, rect.width, width), color);
            DrawColorRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            DrawColorRect(new Rect(rect.x, rect.y, width, rect.height), color);
            DrawColorRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }

        private void DrawLine(Vector2 start, Vector2 end, float width, Color color)
        {
            Vector2 delta = end - start;
            Vector2 midpoint = (start + end) * 0.5f;
            Rect rect = new Rect(midpoint.x - delta.magnitude * 0.5f,
                midpoint.y - width * 0.5f, delta.magnitude, width);
            DrawRotatedRect(rect, color, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void DrawRotatedRect(Rect rect, Color color, float angle)
        {
            Matrix4x4 matrix = GUI.matrix;
            GUI.matrix = matrix *
                Matrix4x4.TRS(rect.center, Quaternion.Euler(0f, 0f, angle), Vector3.one) *
                Matrix4x4.TRS(-rect.center, Quaternion.identity, Vector3.one);
            DrawColorRect(rect, color);
            GUI.matrix = matrix;
        }

        private void DrawColorRect(Rect rect, Color color)
        {
            DrawTexture(rect, pixel, color);
        }

        private static void DrawTexture(Rect rect, Texture texture, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture);
            GUI.color = old;
        }

        private void CreateVisualResources()
        {
            pixel = MakeSolidTexture(Color.white);
            circle = MakeCircleTexture(64, false);
            softCircle = MakeCircleTexture(64, true);
            buttonNormal = MakeSolidTexture(Theme.SurfaceContainerHigh);
            buttonHover = MakeSolidTexture(Theme.OutlineVariant);
            buttonActive = MakeSolidTexture(Theme.PrimaryContainer);
            buttonSelected = MakeSolidTexture(Theme.Selection);

            gameFont = Resources.Load<Font>("Fonts/NotoSansJP-Regular");
            if (gameFont == null)
            {
                gameFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Yu Gothic UI", "Meiryo", "Noto Sans CJK JP", "Arial" }, 24);
            }
            if (gameFont == null)
            {
                gameFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            titleStyle = MakeStyle(48, FontStyle.Bold, TextAnchor.MiddleCenter, Theme.OnSurface);
            titleStyle.wordWrap = false;
            subtitleStyle = MakeStyle(24, FontStyle.Bold, TextAnchor.MiddleCenter, Theme.OnSurface);
            headingStyle = MakeStyle(34, FontStyle.Bold, TextAnchor.MiddleCenter, Theme.OnSurface);
            bodyStyle = MakeStyle(19, FontStyle.Normal, TextAnchor.UpperLeft, Theme.OnSurface);
            bodyStyle.wordWrap = true;
            centeredBodyStyle = MakeStyle(19, FontStyle.Normal, TextAnchor.MiddleCenter, Theme.OnSurface);
            centeredBodyStyle.wordWrap = true;
            captionStyle = MakeStyle(17, FontStyle.Bold, TextAnchor.MiddleLeft, Theme.OnSurfaceVariant);
            tinyStyle = MakeStyle(15, FontStyle.Normal, TextAnchor.MiddleCenter, Theme.OnSurfaceVariant);
            scoreStyle = MakeStyle(29, FontStyle.Bold, TextAnchor.MiddleCenter, Theme.OnSurface);
            buttonStyle = MakeStyle(21, FontStyle.Bold, TextAnchor.MiddleCenter, Theme.OnSurface);
            buttonStyle.hover.background = buttonHover;
            buttonStyle.active.background = buttonActive;
            buttonStyle.padding = new RectOffset(28, 28, 8, 8);
        }

        private GUIStyle MakeStyle(int size, FontStyle style, TextAnchor anchor, Color color)
        {
            return new GUIStyle
            {
                font = gameFont,
                fontSize = size,
                fontStyle = style,
                alignment = anchor,
                normal = { textColor = color },
                hover = { textColor = color },
                active = { textColor = Theme.OnPrimary },
                focused = { textColor = color }
            };
        }

        private static Texture2D MakeSolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "UI color";
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private static Texture2D MakeCircleTexture(int size, bool soft)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = soft ? "Soft circle" : "Circle";
            var pixels = new Color[size * size];
            float radius = size * 0.5f - 1f;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(center, new Vector2(x, y));
                    float alpha = soft
                        ? Mathf.Clamp01(1f - distance / radius)
                        : Mathf.Clamp01(radius - distance + 1f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private void UpdateMenu(ref int index, int count, Action<int> activate)
        {
            int vertical = ReadVerticalPress();
            if (vertical != 0)
            {
                index = (index + vertical + count) % count;
                gameAudio.Play(GameAudio.Cue.Move);
            }
            if (PressedConfirm())
            {
                gameAudio.Play(GameAudio.Cue.Confirm);
                activate(index);
            }
        }

        private void ActivateTitleItem(int index)
        {
            switch (index)
            {
                case 0: StartNewGame(); break;
                case 1: screen = ScreenMode.HowTo; break;
                case 2:
                    optionsReturnScreen = ScreenMode.Title;
                    screen = ScreenMode.Options;
                    break;
                case 3: screen = ScreenMode.Records; break;
                case 4:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        private void ActivatePauseItem(int index)
        {
            if (index == 0)
            {
                ResumeGame();
                return;
            }

            if (index == 1)
            {
                optionsReturnScreen = ScreenMode.Paused;
                optionsIndex = 0;
                pendingPauseAction = -1;
                screen = ScreenMode.Options;
                return;
            }

            if (pendingPauseAction == index && pendingPauseTime > 0f)
            {
                if (index == 2) StartNewGame(); else ReturnToTitle();
                return;
            }

            pendingPauseAction = index;
            pendingPauseTime = 2.5f;
            toastText = index == 2 ? "もう一度決定で最初からやり直す" : "もう一度決定でタイトルへ戻る";
            toastTime = 2.5f;
            gameAudio.Play(GameAudio.Cue.Warning);
        }

        private void ActivateResultItem(int index)
        {
            if (index == 0) StartNewGame(); else ReturnToTitle();
        }

        private void ResumeGame()
        {
            pendingPauseAction = -1;
            screen = ScreenMode.Playing;
            gameAudio.Play(GameAudio.Cue.Back);
        }

        private void ReturnToTitle()
        {
            SaveSettings();
            gameAudio.StopMusic();
            screen = ScreenMode.Title;
            gameAudio.Play(GameAudio.Cue.Back);
        }

        private void CloseOptions()
        {
            SaveSettings();
            screen = optionsReturnScreen;
            gameAudio.Play(GameAudio.Cue.Back);
        }

        private void UpdateOptions()
        {
            int vertical = ReadVerticalPress();
            if (vertical != 0)
            {
                optionsIndex = (optionsIndex + vertical + 5) % 5;
                gameAudio.Play(GameAudio.Cue.Move);
            }

            int horizontal = ReadHorizontalPress();
            if (horizontal != 0 && optionsIndex < 4)
            {
                AdjustOption(optionsIndex, horizontal);
            }

            if (PressedConfirm())
            {
                if (optionsIndex == 4)
                {
                    CloseOptions();
                }
                else
                {
                    AdjustOption(optionsIndex, 1);
                }
            }

            if (PressedBack())
            {
                CloseOptions();
            }
        }

        private void AdjustOption(int index, int direction)
        {
            if (index == 0)
            {
                masterVolume = Mathf.Clamp(masterVolume + direction, 0, 10);
                gameAudio.SetVolume(masterVolume / 10f);
            }
            else if (index == 1)
            {
                fullScreenEnabled = !fullScreenEnabled;
                Screen.fullScreenMode = fullScreenEnabled
                    ? FullScreenMode.FullScreenWindow
                    : FullScreenMode.Windowed;
            }
            else if (index == 2)
            {
                screenShakeEnabled = !screenShakeEnabled;
            }
            else if (index == 3)
            {
                reducedFlash = !reducedFlash;
            }
            gameAudio.Play(GameAudio.Cue.Confirm);
            SaveSettings();
        }

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetInt(PrefPrefix + "volume", 8);
            fullScreenEnabled = PlayerPrefs.GetInt(PrefPrefix + "fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            Screen.fullScreenMode = fullScreenEnabled ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            screenShakeEnabled = PlayerPrefs.GetInt(PrefPrefix + "shake", 1) == 1;
            reducedFlash = PlayerPrefs.GetInt(PrefPrefix + "reduced_flash", 0) == 1;
            moonMode = PlayerPrefs.GetInt(PrefPrefix + "moon_mode", 0) == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt(PrefPrefix + "volume", masterVolume);
            PlayerPrefs.SetInt(PrefPrefix + "fullscreen", fullScreenEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PrefPrefix + "shake", screenShakeEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PrefPrefix + "reduced_flash", reducedFlash ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void TrackSecretCode()
        {
            for (int i = 0; i < secretSequence.Length; i++)
            {
                if (!Input.GetKeyDown(secretSequence[i]))
                {
                    continue;
                }

                if (i == secretProgress)
                {
                    secretProgress++;
                    if (secretProgress >= secretSequence.Length)
                    {
                        UnlockMoonMode("SECRET! 月影スタジアム解放");
                        secretProgress = 0;
                    }
                }
                else
                {
                    secretProgress = i == 0 ? 1 : 0;
                }
                break;
            }
        }

        private void UnlockMoonMode(string message)
        {
            moonMode = true;
            PlayerPrefs.SetInt(PrefPrefix + "moon_mode", 1);
            PlayerPrefs.Save();
            toastText = message;
            toastTime = 3f;
            SpawnBurst(new Vector2(900f, 350f), Theme.Warning, 72, 360f);
            gameAudio.Play(GameAudio.Cue.Win);
        }

        private int ReadHorizontalPress()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return -1;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return 1;
            float axis = SafeAxis("Horizontal");
            if (Mathf.Abs(axis) < 0.35f) horizontalAxisLatch = 0f;
            if (axis > 0.65f && horizontalAxisLatch <= 0f) { horizontalAxisLatch = 1f; return 1; }
            if (axis < -0.65f && horizontalAxisLatch >= 0f) { horizontalAxisLatch = -1f; return -1; }
            return 0;
        }

        private static float ReadHorizontalAxis()
        {
            float keyboard = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) keyboard -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) keyboard += 1f;
            return keyboard != 0f ? keyboard : Mathf.Clamp(SafeAxis("Horizontal"), -1f, 1f);
        }

        private int ReadVerticalPress()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return -1;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return 1;
            float axis = SafeAxis("Vertical");
            if (Mathf.Abs(axis) < 0.35f) verticalAxisLatch = 0f;
            if (axis > 0.65f && verticalAxisLatch <= 0f) { verticalAxisLatch = 1f; return -1; }
            if (axis < -0.65f && verticalAxisLatch >= 0f) { verticalAxisLatch = -1f; return 1; }
            return 0;
        }

        private static float SafeAxis(string axis)
        {
            try { return Input.GetAxisRaw(axis); }
            catch (ArgumentException) { return 0f; }
        }

        private static bool PressedConfirm()
        {
            return Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.JoystickButton0);
        }

        private static bool PressedSpecial()
        {
            return Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) ||
                Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.JoystickButton3);
        }

        private static bool PressedBack()
        {
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
        }

        private float NextRoll()
        {
            return (float)random.NextDouble();
        }

        private static float PitchDuration(PitchType type)
        {
            if (type == PitchType.Fastball) return 0.86f;
            if (type == PitchType.Changeup) return 1.12f;
            return 1f;
        }

        private static bool IsHit(PlateResult result)
        {
            return result == PlateResult.Single || result == PlateResult.Double ||
                result == PlateResult.Triple || result == PlateResult.HomeRun;
        }

        private static string ResultLabel(PlateResult result)
        {
            switch (result)
            {
                case PlateResult.Strike: return "STRIKE!";
                case PlateResult.Ball: return "BALL";
                case PlateResult.Foul: return "FOUL";
                case PlateResult.Out: return "OUT!";
                case PlateResult.Single: return "SINGLE!";
                case PlateResult.Double: return "DOUBLE!";
                case PlateResult.Triple: return "TRIPLE!";
                case PlateResult.HomeRun: return "HOME RUN!!";
                default: return string.Empty;
            }
        }

        private static string PitchLabel(PitchType type)
        {
            if (type == PitchType.Fastball) return "ストレート";
            if (type == PitchType.Curve) return "カーブ";
            return "チェンジアップ";
        }

        private static string Dots(int filled, int maximum)
        {
            string value = string.Empty;
            for (int i = 0; i < maximum; i++)
            {
                value += i < filled ? "●" : "○";
            }
            return value;
        }
    }

    internal static class Theme
    {
        public static readonly Color Primary = Hex("F7C84B");
        public static readonly Color OnPrimary = Hex("17212B");
        public static readonly Color PrimaryContainer = Hex("6B5014");
        public static readonly Color Secondary = Hex("4CE0C2");
        public static readonly Color Surface = Hex("071521");
        public static readonly Color SurfaceContainer = Hex("10283B");
        public static readonly Color SurfaceContainerHigh = Hex("17364D");
        public static readonly Color OnSurface = Hex("F7FBFF");
        public static readonly Color OnSurfaceVariant = Hex("BDD0DF");
        public static readonly Color Outline = Hex("7893A8");
        public static readonly Color OutlineVariant = Hex("294D67");
        public static readonly Color Selection = Hex("265C78");
        public static readonly Color Focus = Hex("FFFFFF");
        public static readonly Color Error = Hex("FF737D");
        public static readonly Color Warning = Hex("FFB85C");
        public static readonly Color Success = Hex("68E28F");
        public static readonly Color Sky = Hex("183F60");
        public static readonly Color Grass = Hex("2B805A");
        public static readonly Color GrassDark = Hex("19543D");
        public static readonly Color Dirt = Hex("B86F45");
        public static readonly Color DirtLight = Hex("D99B67");
        public static readonly Color CrowdDark = Hex("0B1D2B");
        public static readonly Color Chalk = Hex("EAF6EE");
        public static readonly Color Base = Hex("FFFFFF");
        public static readonly Color Ball = Hex("FFFFFF");
        public static readonly Color Bat = Hex("E6B875");
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.45f);
        public static readonly Color MoonSky = Hex("17173B");
        public static readonly Color MoonGrass = Hex("31526D");
        public static readonly Color MoonGrassDark = Hex("1A3048");
        public static readonly Color MoonDirt = Hex("765A80");
        public static readonly Color MoonDirtLight = Hex("A37BA3");
        public static readonly Color MoonPlayer = Hex("D7A5FF");

        private static Color Hex(string value)
        {
            Color color;
            return ColorUtility.TryParseHtmlString("#" + value, out color) ? color : Color.magenta;
        }
    }

    internal sealed class GameAudio : MonoBehaviour
    {
        internal enum Cue
        {
            Move,
            Confirm,
            Back,
            Deny,
            Pitch,
            Hit,
            HomeRun,
            Strike,
            Ball,
            Foul,
            Out,
            Special,
            Warning,
            Pause,
            Win,
            Lose,
            Draw
        }

        private readonly Dictionary<Cue, AudioClip> clips = new Dictionary<Cue, AudioClip>();
        private AudioSource effects;
        private AudioSource music;

        private void Awake()
        {
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;

            clips[Cue.Move] = Tone("Move", 410f, 0.05f, 80f, 0.16f);
            clips[Cue.Confirm] = Tone("Confirm", 620f, 0.09f, 220f, 0.18f);
            clips[Cue.Back] = Tone("Back", 360f, 0.08f, -130f, 0.16f);
            clips[Cue.Deny] = Tone("Deny", 155f, 0.12f, -30f, 0.18f);
            clips[Cue.Pitch] = NoiseTone("Pitch", 0.14f, 0.11f);
            clips[Cue.Hit] = NoiseTone("Hit", 0.18f, 0.28f);
            clips[Cue.HomeRun] = Chord("Home Run", new[] { 392f, 523.25f, 659.25f }, 0.7f, 0.17f);
            clips[Cue.Strike] = Tone("Strike", 240f, 0.18f, 260f, 0.21f);
            clips[Cue.Ball] = Tone("Ball", 180f, 0.14f, -45f, 0.15f);
            clips[Cue.Foul] = NoiseTone("Foul", 0.10f, 0.14f);
            clips[Cue.Out] = Tone("Out", 210f, 0.25f, -100f, 0.18f);
            clips[Cue.Special] = Chord("Special", new[] { 293.66f, 440f, 698.46f }, 0.58f, 0.15f);
            clips[Cue.Warning] = Tone("Warning", 125f, 0.44f, 60f, 0.19f);
            clips[Cue.Pause] = Tone("Pause", 330f, 0.12f, -80f, 0.14f);
            clips[Cue.Win] = Chord("Win", new[] { 523.25f, 659.25f, 783.99f }, 1.05f, 0.16f);
            clips[Cue.Lose] = Chord("Lose", new[] { 293.66f, 261.63f, 220f }, 0.85f, 0.13f);
            clips[Cue.Draw] = Chord("Draw", new[] { 329.63f, 392f }, 0.65f, 0.13f);
            music.clip = CreateMusic();
            music.volume = 0.27f;
        }

        internal void SetVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
        }

        internal void Play(Cue cue)
        {
            AudioClip clip;
            if (effects != null && clips.TryGetValue(cue, out clip))
            {
                effects.PlayOneShot(clip);
            }
        }

        internal void StartMusic()
        {
            if (music != null && !music.isPlaying) music.Play();
        }

        internal void StopMusic()
        {
            if (music != null) music.Stop();
        }

        private void OnDestroy()
        {
            foreach (AudioClip clip in clips.Values) Destroy(clip);
            if (music != null && music.clip != null) Destroy(music.clip);
        }

        private static AudioClip Tone(string name, float frequency, float duration, float slide, float volume)
        {
            const int sampleRate = 44100;
            int count = Mathf.CeilToInt(duration * sampleRate);
            var data = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * i / count);
                phase += (frequency + slide * t) * Mathf.PI * 2f / sampleRate;
                data[i] = Mathf.Sign(Mathf.Sin(phase)) * envelope * volume;
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip NoiseTone(string name, float duration, float volume)
        {
            const int sampleRate = 44100;
            int count = Mathf.CeilToInt(duration * sampleRate);
            var data = new float[count];
            var noise = new System.Random(name.GetHashCode());
            float filtered = 0f;
            for (int i = 0; i < count; i++)
            {
                float envelope = 1f - i / (float)count;
                filtered = Mathf.Lerp(filtered, (float)noise.NextDouble() * 2f - 1f, 0.3f);
                data[i] = filtered * envelope * volume;
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Chord(string name, float[] frequencies, float duration, float volume)
        {
            const int sampleRate = 44100;
            int count = Mathf.CeilToInt(duration * sampleRate);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Min(1f, t * 18f) * Mathf.Clamp01((duration - t) * 3f);
                float sample = 0f;
                for (int note = 0; note < frequencies.Length; note++)
                {
                    sample += Mathf.Sin(t * frequencies[note] * Mathf.PI * 2f);
                }
                data[i] = sample / frequencies.Length * envelope * volume;
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateMusic()
        {
            const int sampleRate = 44100;
            const float duration = 8f;
            int count = (int)(sampleRate * duration);
            var data = new float[count];
            float[] melody = { 261.63f, 329.63f, 392f, 523.25f, 392f, 329.63f, 293.66f, 392f };
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                int beat = Mathf.FloorToInt(t * 2f) % melody.Length;
                float beatPhase = (t * 2f) % 1f;
                float lead = Mathf.Sign(Mathf.Sin(t * melody[beat] * Mathf.PI * 2f)) * (1f - beatPhase) * 0.035f;
                float bassFrequency = beat < 4 ? 65.41f : 73.42f;
                float bass = Mathf.Sin(t * bassFrequency * Mathf.PI * 2f) * 0.035f;
                data[i] = lead + bass;
            }
            var clip = AudioClip.Create("Ballpark Loop", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
