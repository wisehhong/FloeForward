using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FloeForward
{
    public enum GamePhase { Title, Preparing, Playing, Arrival, Choice, Victory, Sinking, Failure }

    public sealed class GameController : MonoBehaviour
    {
        private const float CameraFieldOfView = 28f;
        private const float ReferenceAspect = 16f / 9f;
        private const float WaterMarkerSpacing = 7f;
        private const float WaterMarkerStart = -20f;

        private struct ActiveResource
        {
            public GameObject Object;
            public ResourceSpawn Spawn;
            public Floe Floe;
            public Pickup Pickup;
        }

        private readonly List<ActiveResource> resources = new List<ActiveResource>();
        private readonly List<Choice> selectedChoices = new List<Choice>();
        private readonly List<int> selectedChoiceIndices = new List<int>();
        private Camera viewCamera;
        private float viewAspect = -1f;
        private Transform[] waterMarkers;
        private Transform sun;
        private Bear bear;
        private GameUI ui;
        private Floe preparationFloe;
        private GameObject destinationFloe;
        private float destinationLength;
        private float arrivalElapsed;
        private float sinkingElapsed;

        public GamePhase Phase { get; private set; } = GamePhase.Title;
        public bool IsPlaying => Phase == GamePhase.Playing;
        public bool OnDestination => destinationFloe != null && destinationFloe.activeInHierarchy &&
            DestinationFrontZ <= JourneyData.BearHalfLength;
        private float DestinationFrontZ =>
            JourneyData.ScrollSpeed * (JourneyData.StageDuration - Elapsed) - JourneyData.ArrivalFloeInset;
        public int Stage { get; private set; }
        public int CompletedStages { get; private set; }
        public int FloeAdjustment { get; private set; }
        public bool SupportActive { get; private set; }
        public float Stamina { get; private set; } = JourneyData.MaxStamina;
        public float Elapsed { get; private set; }
        public float PreparationRemaining => preparationFloe != null ?
            JourneyData.PreparationDuration * (1f - preparationFloe.CrackProgress) : 0f;
        public float SwimRate => JourneyData.SwimDrain - (SupportActive ? JourneyData.SupportDiscount : 0f);
        public Bear Player => bear;
        public IReadOnlyList<Choice> SelectedChoices => selectedChoices;
        public IReadOnlyList<int> SelectedChoiceIndices => selectedChoiceIndices;
        public int SolidFloeCount { get; private set; }
        public int FishCollected { get; private set; }
        public bool AutomatedInput { get; set; }
        public float AutomatedDirection { get; set; }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            viewCamera = Camera.main;
            viewCamera.orthographic = false;
            viewCamera.nearClipPlane = .1f;
            viewCamera.farClipPlane = 350f;
            viewCamera.backgroundColor = new Color(.36f, .67f, .78f);
            viewCamera.transform.position = new Vector3(0, 5.7f, -17f);
            viewCamera.transform.LookAt(new Vector3(0, 0, 21f));
            UpdateCameraFraming();
            CreateLighting();
            waterMarkers = ShapeArt.WaterWorld(transform);
            PositionWater();
            sun = ShapeArt.Sun(transform);
            PositionSun();
            var bearObject = new GameObject("Polar bear");
            bear = bearObject.AddComponent<Bear>();
            bear.Initialize(this);
            bear.ResetPosition();
            ui = GetComponent<GameUI>();
            if (ui == null) throw new MissingComponentException("Add a GameUI component with UI text to the Game object in the scene.");
            ui.Initialize(this);
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-floe-test") >= 0)
                gameObject.AddComponent<JourneySelfTest>();
        }

        private void CreateLighting()
        {
            if (FindFirstObjectByType<Light>() != null) return;
            var lightObject = new GameObject("Arctic daylight");
            lightObject.transform.rotation = Quaternion.Euler(43f, -28f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(.82f, .92f, 1f);
            RenderSettings.ambientLight = new Color(.57f, .68f, .78f);
        }

        private void Update()
        {
            UpdateCameraFraming();
            if (AutomatedInput) return;
            Keyboard keyboard = Keyboard.current;
            if (Phase == GamePhase.Preparing) AdvancePreparation(Time.deltaTime);
            else if (Phase == GamePhase.Playing)
            {
                float direction = keyboard == null ? 0f :
                    (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed ? 1f : 0f)
                    - (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed ? 1f : 0f);
                Tick(direction, Time.deltaTime);
            }
            else if (Phase == GamePhase.Arrival) AdvanceArrival(Time.deltaTime);
            else if (Phase == GamePhase.Sinking) AdvanceSinking(Time.deltaTime);
            else if (keyboard != null && (Phase == GamePhase.Title || Phase == GamePhase.Victory || Phase == GamePhase.Failure)
                && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                StartRun();
            else if (keyboard != null && Phase == GamePhase.Choice)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) SelectChoice(0);
                else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) SelectChoice(1);
                else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) SelectChoice(2);
            }
        }

        private void FixedUpdate()
        {
            if (!AutomatedInput) return;
            if (Phase == GamePhase.Preparing) AdvancePreparation(Time.fixedDeltaTime);
            else if (IsPlaying) Tick(AutomatedDirection, Time.fixedDeltaTime);
            else if (Phase == GamePhase.Arrival) AdvanceArrival(Time.fixedDeltaTime);
            else if (Phase == GamePhase.Sinking) AdvanceSinking(Time.fixedDeltaTime);
        }

        private void UpdateCameraFraming()
        {
            float aspect = viewCamera.aspect;
            if (Mathf.Approximately(aspect, viewAspect)) return;
            viewAspect = aspect;
            float horizontalFieldOfView = Camera.VerticalToHorizontalFieldOfView(CameraFieldOfView, ReferenceAspect);
            viewCamera.fieldOfView = Mathf.Max(CameraFieldOfView,
                Camera.HorizontalToVerticalFieldOfView(horizontalFieldOfView, aspect));
            PositionSun();
        }

        private void PositionSun()
        {
            if (sun == null) return;
            float progress = Mathf.Clamp01(Elapsed / JourneyData.StageDuration);
            float farZ = viewCamera.transform.position.z +
                viewCamera.transform.forward.z * viewCamera.farClipPlane;
            float waterline = viewCamera.WorldToViewportPoint(new Vector3(0, -.075f, farZ)).y;
            const float diameter = .06f;
            float depth = viewCamera.farClipPlane - 20f;
            sun.position = viewCamera.ViewportToWorldPoint(new Vector3(
                Mathf.Lerp(.75f, .25f, progress),
                waterline + diameter * ShapeArt.SunHalfExtent + Mathf.Sin(progress * Mathf.PI) * .12f, depth));
            float size = 2f * depth * Mathf.Tan(viewCamera.fieldOfView * .5f * Mathf.Deg2Rad) * diameter;
            sun.rotation = viewCamera.transform.rotation;
            sun.localScale = new Vector3(size, size, size * .02f);
        }

        private void PositionWater()
        {
            float range = waterMarkers.Length * WaterMarkerSpacing;
            for (int i = 0; i < waterMarkers.Length; i++)
            {
                Vector3 position = waterMarkers[i].position;
                position.z = WaterMarkerStart +
                    Mathf.Repeat(i * WaterMarkerSpacing - Elapsed * JourneyData.ScrollSpeed, range);
                waterMarkers[i].position = position;
            }
        }

        private void Tick(float direction, float delta)
        {
            if (!IsPlaying) return;
            delta = Mathf.Min(delta * JourneyData.GameplaySpeed, JourneyData.StageDuration - Elapsed);
            bear.Move(direction, delta);
            Elapsed = Mathf.Min(Elapsed + delta, JourneyData.StageDuration);
            PositionDestination();
            PositionWater();
            PositionSun();
            foreach (ActiveResource resource in resources)
            {
                if (resource.Object == null || !resource.Object.activeSelf) continue;
                float x = resource.Pickup != null ? resource.Pickup.HorizontalPosition(Elapsed) : JourneyData.WorldX(resource.Spawn);
                resource.Object.transform.position = new Vector3(x,
                    resource.Pickup != null ? .55f : 0f,
                    JourneyData.ScrollSpeed * (resource.Spawn.Arrival - Elapsed));
                if (resource.Floe != null) resource.Floe.Advance(delta, bear, this);
                if (!IsPlaying) return;
                if (resource.Object.activeSelf && resource.Object.transform.position.z < -8f)
                {
                    if (resource.Floe != null) bear.RemoveFloe(resource.Floe);
                    resource.Object.SetActive(false);
                    Destroy(resource.Object);
                }
            }
            if (!bear.OnIce) LoseStamina(SwimRate * delta, true);
            if (!IsPlaying) return;
            if (Elapsed >= JourneyData.StageDuration)
            {
                CompletedStages++;
                arrivalElapsed = 0f;
                Phase = GamePhase.Arrival;
            }
        }

        private void AdvancePreparation(float delta)
        {
            preparationFloe.Advance(delta, bear, this);
            if (!preparationFloe.IsBroken) return;
            resources.Add(new ActiveResource {
                Object = preparationFloe.gameObject,
                Floe = preparationFloe,
                Spawn = new ResourceSpawn(ResourceKind.Thin, .5f,
                    JourneyData.PlayfieldHalfWidth * 1.6f, 3.8f, 0f)
            });
            preparationFloe = null;
            Phase = GamePhase.Playing;
        }

        private void AdvanceArrival(float delta)
        {
            if (!bear.AdvanceCelebration(delta)) return;
            arrivalElapsed += delta;
            if (arrivalElapsed < JourneyData.ArrivalPause) return;
            ClearResources();
            if (Stage == 2) Phase = GamePhase.Victory;
            else
            {
                RestoreStamina(25f);
                Phase = GamePhase.Choice;
            }
        }

        private void AdvanceSinking(float delta)
        {
            sinkingElapsed = Mathf.Min(sinkingElapsed + delta, JourneyData.SinkingDuration);
            bear.Sink(sinkingElapsed / JourneyData.SinkingDuration);
            if (sinkingElapsed >= JourneyData.SinkingDuration) Phase = GamePhase.Failure;
        }

        private void CreateDestination()
        {
            destinationFloe = new GameObject("Destination floe");
            destinationLength = 3.8f;
            destinationFloe.AddComponent<Floe>().Initialize(false,
                JourneyData.PlayfieldHalfWidth * 1.6f, destinationLength,
                JourneyData.ThinIceBreakTime, true);
            PositionDestination();
        }

        private void PositionDestination()
        {
            destinationFloe.transform.position = new Vector3(0, 0,
                DestinationFrontZ + destinationLength * .5f);
        }

        public void StartRun()
        {
            ClearResources();
            Stage = 0;
            CompletedStages = 0;
            FloeAdjustment = 0;
            SupportActive = false;
            Stamina = JourneyData.MaxStamina;
            selectedChoices.Clear();
            selectedChoiceIndices.Clear();
            BeginStage();
        }

        public void SelectChoice(int index)
        {
            if (Phase != GamePhase.Choice || index < 0 || index >= 3) return;
            Choice choice = JourneyData.Choices[Stage][index];
            selectedChoices.Add(choice);
            selectedChoiceIndices.Add(index);
            FloeAdjustment += choice.FloeChange;
            SupportActive = choice.Support;
            Stage++;
            BeginStage();
        }

        private void BeginStage()
        {
            ClearResources();
            ui.ClearStaminaPopups();
            Elapsed = 0f;
            PositionWater();
            PositionSun();
            arrivalElapsed = 0f;
            bear.ResetPosition();
            SolidFloeCount = 0;
            FishCollected = 0;
            CreateDestination();
            foreach (ResourceSpawn spawn in JourneyData.Layout(Stage, FloeAdjustment))
            {
                var obj = new GameObject(spawn.Kind.ToString());
                obj.transform.position = new Vector3(JourneyData.WorldX(spawn),
                    spawn.Kind == ResourceKind.Fish ? .55f : 0f,
                    JourneyData.ScrollSpeed * spawn.Arrival);
                Floe floe = null;
                Pickup pickup = null;
                if (spawn.Kind == ResourceKind.Fish)
                {
                    pickup = obj.AddComponent<Pickup>();
                    pickup.Initialize();
                    Vector3 position = obj.transform.position;
                    position.x = pickup.HorizontalPosition(0f);
                    obj.transform.position = position;
                }
                else
                {
                    floe = obj.AddComponent<Floe>();
                    floe.Initialize(spawn.Kind == ResourceKind.Thin, spawn.Width, spawn.Height);
                    if (spawn.Kind == ResourceKind.Solid) SolidFloeCount++;
                }
                resources.Add(new ActiveResource { Object = obj, Spawn = spawn, Floe = floe, Pickup = pickup });
            }
            var startingIce = new GameObject("Preparation floe");
            startingIce.transform.position = Vector3.zero;
            preparationFloe = startingIce.AddComponent<Floe>();
            preparationFloe.Initialize(true, JourneyData.PlayfieldHalfWidth * 1.6f, 3.8f,
                JourneyData.PreparationDuration);
            preparationFloe.Touch();
            Phase = GamePhase.Preparing;
        }

        private void ClearResources()
        {
            foreach (ActiveResource resource in resources)
            {
                if (resource.Object == null) continue;
                resource.Object.SetActive(false);
                Destroy(resource.Object);
            }
            resources.Clear();
            if (preparationFloe != null)
            {
                preparationFloe.gameObject.SetActive(false);
                Destroy(preparationFloe.gameObject);
                preparationFloe = null;
            }
            if (destinationFloe != null)
            {
                destinationFloe.SetActive(false);
                Destroy(destinationFloe);
                destinationFloe = null;
            }
            if (bear != null) bear.ResetPosition();
        }

        public void RestoreStamina(float amount)
        {
            float previous = Stamina;
            Stamina = Mathf.Min(JourneyData.MaxStamina, Stamina + amount);
            ui.ShowStaminaChange(Stamina - previous);
        }

        public void CollectFish()
        {
            FishCollected++;
            RestoreStamina(20f);
        }

        public void LoseStamina(float amount, bool continuous = false)
        {
            if (!IsPlaying) return;
            float previous = Stamina;
            Stamina = Mathf.Max(0f, Stamina - amount);
            ui.ShowStaminaChange(Stamina - previous, continuous);
            if (Stamina <= 0f)
            {
                sinkingElapsed = 0f;
                bear.BeginSinking();
                ui.ClearStaminaPopups();
                Phase = GamePhase.Sinking;
            }
        }
    }
}
