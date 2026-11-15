using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FloeForward
{
    // Invoked only by the explicit -floe-test command-line argument.
    public sealed class JourneySelfTest : MonoBehaviour
    {
        private GameController game;
        private int checks;
        private Pickup[] fishTargets;
        private int fishTargetStage = -1;
        private float lastSteeringTime = float.PositiveInfinity;

        private IEnumerator Start()
        {
            game = GetComponent<GameController>();
            Application.runInBackground = true;
            Time.timeScale = 20f;
            Time.maximumDeltaTime = .333333f;
            Application.targetFrameRate = -1;
            Random.InitState(20261114);
            game.AutomatedInput = true;

            CheckLayouts();
            game.StartRun();
            yield return FinishPreparation();
            if (!enabled) yield break;
            CheckFishAndStamina();
            yield return CheckThinIce();
            if (!enabled) yield break;

            DisableResources();
            int limit = 10000;
            while (game.IsPlaying && limit-- > 0) yield return new WaitForFixedUpdate();
            Check(game.Phase == GamePhase.Sinking, "swimming without resources causes exhaustion");
            limit = 1000;
            while (game.Phase == GamePhase.Sinking && limit-- > 0) yield return new WaitForFixedUpdate();
            Check(game.Phase == GamePhase.Failure && game.CompletedStages == 0,
                "exhaustion ends the run without clearing a stage");

            game.StartRun();
            Check(game.Phase == GamePhase.Preparing && game.Stage == 0 &&
                game.CompletedStages == 0 && game.SelectedChoices.Count == 0 &&
                game.FloeAdjustment == 0 && !game.SupportActive && game.Stamina == JourneyData.MaxStamina,
                "restarting resets run state");
            yield return CompleteStage();
            if (!enabled) yield break;
            game.SelectChoice(0);
            Check(game.Stage == 1 && game.FloeAdjustment == 1 && game.SolidFloeCount == 7 &&
                !game.SupportActive, "the first choice adds solid ice to the next stage");

            yield return CompleteStage();
            if (!enabled) yield break;
            game.SelectChoice(2);
            Check(game.Stage == 2 && game.FloeAdjustment == 0 && game.SolidFloeCount == 4 &&
                game.SupportActive && Mathf.Approximately(game.SwimRate, 3.5f),
                "the second choice applies its floe change and one-stage swim discount");

            yield return CompleteStage();
            Check(game.Phase == GamePhase.Victory && game.CompletedStages == 3 &&
                game.SelectedChoices.Count == 2, "the three-stage journey reaches victory");
            if (!enabled) yield break;

            Debug.Log("FLOE TEST PASS: " + checks + " checks");
            Quit(0);
        }

        private void CheckLayouts()
        {
            int[] expectedSolids = { 8, 6, 4 };
            for (int stage = 0; stage < expectedSolids.Length; stage++)
            {
                List<ResourceSpawn> layout = JourneyData.Layout(stage, 0);
                int solids = 0;
                foreach (ResourceSpawn item in layout)
                {
                    if (item.Kind == ResourceKind.Solid) solids++;
                    Check(Mathf.Abs(JourneyData.WorldX(item)) + item.Width / 2f <=
                        JourneyData.PlayfieldHalfWidth + .001f, "resources fit inside the playfield");
                }
                Check(solids == expectedSolids[stage], "baseline solid-floe count for stage " + stage);
            }

            Check(SolidCount(1, 1) == 7 && SolidCount(1, -1) == 5 &&
                SolidCount(2, 2) == 6 && SolidCount(2, -2) == 2,
                "choice adjustments add or remove solid floes in later stages");
        }

        private static int SolidCount(int stage, int adjustment)
        {
            int count = 0;
            foreach (ResourceSpawn item in JourneyData.Layout(stage, adjustment))
                if (item.Kind == ResourceKind.Solid) count++;
            return count;
        }

        private void CheckFishAndStamina()
        {
            game.LoseStamina(30f);
            var fishObject = new GameObject("Test fish");
            Pickup fish = fishObject.AddComponent<Pickup>();
            fish.Initialize();
            fish.Collect(game);
            Check(game.FishCollected == 1 && Mathf.Approximately(game.Stamina, 90f),
                "collecting a fish restores stamina once");
            fish.Collect(game);
            Check(game.FishCollected == 1, "a collected fish cannot be collected again");
            game.RestoreStamina(20f);
            Check(game.Stamina == JourneyData.MaxStamina, "stamina restoration is capped");
            Destroy(fishObject);
        }

        private IEnumerator CheckThinIce()
        {
            Floe thinIce = MakeFloe(true);
            thinIce.Touch();
            for (int i = 0; i < 2; i++) yield return new WaitForFixedUpdate();
            Check(game.Player.Overlaps(thinIce), "the bear can stand on thin ice");
            float stamina = game.Stamina;
            thinIce.Advance(JourneyData.ThinIceBreakTime - .01f, game.Player, game);
            Check(!thinIce.IsBroken, "thin ice remains intact until its break time");
            thinIce.Advance(.02f, game.Player, game);
            Check(thinIce.IsBroken && !game.Player.Overlaps(thinIce) &&
                Mathf.Approximately(game.Stamina, stamina - 5f),
                "breaking occupied thin ice removes it and costs five stamina");
            Destroy(thinIce.gameObject);
        }

        private IEnumerator FinishPreparation()
        {
            Check(game.Phase == GamePhase.Preparing && game.Elapsed == 0f,
                "a stage starts in preparation with its timer reset");
            int limit = 1000;
            while (game.Phase == GamePhase.Preparing && limit-- > 0)
                yield return new WaitForFixedUpdate();
            Check(game.IsPlaying && game.Elapsed == 0f,
                "preparation ends and starts the journey");
        }

        private IEnumerator CompleteStage()
        {
            yield return FinishPreparation();
            if (!enabled) yield break;

            int limit = 10000;
            while (game.IsPlaying && limit-- > 0)
            {
                SteerToFish();
                yield return new WaitForFixedUpdate();
            }
            Check(game.Phase == GamePhase.Arrival, "the journey reaches the destination");
            limit = 1000;
            while (game.Phase == GamePhase.Arrival && limit-- > 0)
                yield return new WaitForFixedUpdate();
            Check(game.Phase == (game.Stage == 2 ? GamePhase.Victory : GamePhase.Choice),
                "arrival advances to the correct next screen");
        }

        private void DisableResources()
        {
            foreach (Floe floe in FindObjectsByType<Floe>(FindObjectsSortMode.None))
                floe.gameObject.SetActive(false);
            foreach (Pickup pickup in FindObjectsByType<Pickup>(FindObjectsSortMode.None))
                pickup.gameObject.SetActive(false);
        }

        private void SteerToFish()
        {
            if (game.Stage != fishTargetStage || game.Elapsed < lastSteeringTime)
            {
                fishTargets = FindObjectsByType<Pickup>(FindObjectsSortMode.None);
                fishTargetStage = game.Stage;
            }
            lastSteeringTime = game.Elapsed;
            float nearestZ = float.MaxValue;
            float target = game.Player.X;
            foreach (Pickup pickup in fishTargets)
            {
                if (pickup == null || !pickup.gameObject.activeSelf || pickup.transform.position.z < -.25f ||
                    pickup.transform.position.z >= nearestZ)
                    continue;
                nearestZ = pickup.transform.position.z;
                target = pickup.HorizontalPosition(game.Elapsed + Time.fixedDeltaTime * JourneyData.GameplaySpeed);
            }
            game.AutomatedDirection = Mathf.Clamp((target - game.Player.X) /
                (JourneyData.BearSpeed * Time.fixedDeltaTime * JourneyData.GameplaySpeed), -1f, 1f);
        }

        private void Check(bool condition, string description)
        {
            checks++;
            if (condition || !enabled) return;
            Debug.LogError("FLOE TEST FAILED: " + description + " (stage " + game.Stage +
                ", elapsed " + game.Elapsed + ", stamina " + game.Stamina + ")");
            enabled = false;
            Quit(1);
        }

        private static Floe MakeFloe(bool thin)
        {
            var obj = new GameObject("Test floe");
            Floe floe = obj.AddComponent<Floe>();
            floe.Initialize(thin, 2f);
            return floe;
        }

        private static void Quit(int code)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
    }
}
