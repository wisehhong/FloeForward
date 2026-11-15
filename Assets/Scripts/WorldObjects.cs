using UnityEngine;

namespace FloeForward
{
    public static class ShapeArt
    {
        public const float SunHalfExtent = .71f;

        private static Material iceMaterial;
        private static Material thinIceMaterial;
        private static Material bearMaterial;
        private static Material bearBodyMaterial;
        private static Material bearDetailMaterial;
        private static Material darkMaterial;
        private static Material waterMaterial;
        private static Material waveMaterial;
        private static Material fishMaterial;
        private static Material fishFinMaterial;
        private static Material sunMaterial;
        private static Material sunCenterMaterial;

        private static Material Material(ref Material material, Color color, bool transparent = false)
        {
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new MissingReferenceException("URP Lit shader not found. Is URP installed and active?");
            material = new Material(shader) { color = color };
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetShaderPassEnabled("DepthOnly", false);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            return material;
        }

        private static Material Ice => Material(ref iceMaterial, new Color(.86f, .96f, 1f));
        private static Material ThinIce => Material(ref thinIceMaterial, new Color(.53f, .82f, .91f, .72f), true);
        private static Material BearWhite => Material(ref bearMaterial, new Color(.86f, .84f, .80f));
        private static Material BearBody => Material(ref bearBodyMaterial, new Color(.77f, .76f, .72f));
        private static Material BearDetail => Material(ref bearDetailMaterial, new Color(.65f, .65f, .62f));
        private static Material Dark => Material(ref darkMaterial, new Color(.10f, .18f, .20f));
        private static Material Water => Material(ref waterMaterial, new Color(.025f, .23f, .31f));
        private static Material Wave => Material(ref waveMaterial, new Color(.16f, .44f, .5f));
        private static Material FishMaterial => Material(ref fishMaterial, new Color(1f, .67f, .2f));
        private static Material FishFin => Material(ref fishFinMaterial, new Color(.83f, .4f, .12f));
        private static Material cheekMaterial;
        private static Material Cheek => Material(ref cheekMaterial, new Color(.95f, .55f, .6f));

        public static GameObject Primitive(string name, Transform parent, PrimitiveType type,
            Vector3 position, Vector3 scale, Material material)
        {
            var result = GameObject.CreatePrimitive(type);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            var collider = result.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            result.GetComponent<MeshRenderer>().sharedMaterial = material;
            return result;
        }

        public static Transform[] WaterWorld(Transform parent)
        {
            Primitive("Ocean", parent, PrimitiveType.Cube, new Vector3(0, -.35f, 0),
                new Vector3(2000, .55f, 2000), Water);
            var markers = new Transform[24];
            for (int i = 0; i < markers.Length; i++)
            {
                int variation = i % 3;
                float side = i % 2 == 0 ? -1f : 1f;
                markers[i] = Primitive("Ocean ripple " + i, parent, PrimitiveType.Cube,
                    new Vector3(side * JourneyData.PlayfieldHalfWidth * (.5f + variation * .09f), -.045f, 0),
                    new Vector3(1f + variation * .25f, .035f, .06f), Wave).transform;
            }
            return markers;
        }

        public static Transform Sun(Transform parent)
        {
            Material material = Material(ref sunMaterial, new Color(1f, .72f, .18f));
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(1f, .62f, .08f));
            GameObject sun = Primitive("Sun", parent, PrimitiveType.Cube, Vector3.zero,
                Vector3.one, material);
            Material center = Material(ref sunCenterMaterial, new Color(1f, .32f, .03f));
            center.EnableKeyword("_EMISSION");
            center.SetColor("_EmissionColor", new Color(.8f, .15f, .01f));
            // Separate the inset from the distant face to avoid depth-buffer flicker.
            Primitive("Sun center", sun.transform, PrimitiveType.Cube, new Vector3(0, 0, -1.5f),
                new Vector3(.55f, .55f, .1f), center);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("Sun ray", sun.transform, PrimitiveType.Cube, new Vector3(side * .63f, 0, 0),
                    new Vector3(.16f, .12f, .8f), material);
                Primitive("Sun ray", sun.transform, PrimitiveType.Cube, new Vector3(0, side * .63f, 0),
                    new Vector3(.12f, .16f, .8f), material);
                for (int vertical = -1; vertical <= 1; vertical += 2)
                    Primitive("Sun ray", sun.transform, PrimitiveType.Cube,
                        new Vector3(side * .59f, vertical * .59f, 0),
                        new Vector3(.12f, .12f, .8f), material);
            }
            foreach (MeshRenderer renderer in sun.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return sun.transform;
        }

        public static void Bear(Transform parent)
        {
            Primitive("Bear body", parent, PrimitiveType.Cube, new Vector3(0, .60f, -.06f),
                new Vector3(.96f, .72f, 1.30f), BearBody);
            Primitive("Bear shoulders", parent, PrimitiveType.Cube, new Vector3(0, .78f, .38f),
                new Vector3(.88f, .64f, .55f), BearWhite);
            Primitive("Bear head", parent, PrimitiveType.Cube, new Vector3(0, 1.08f, .76f),
                new Vector3(.72f, .60f, .64f), BearWhite);
            Primitive("Bear muzzle", parent, PrimitiveType.Cube, new Vector3(0, .96f, 1.14f),
                new Vector3(.42f, .26f, .27f), BearDetail);
            Primitive("Bear nose", parent, PrimitiveType.Cube, new Vector3(0, 1.02f, 1.29f),
                new Vector3(.16f, .11f, .07f), Dark);
            Primitive("Bear tail", parent, PrimitiveType.Cube, new Vector3(0, .74f, -.76f),
                new Vector3(.16f, .16f, .14f), BearDetail);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("Bear ear", parent, PrimitiveType.Cube, new Vector3(side * .24f, 1.45f, .70f),
                    new Vector3(.18f, .18f, .16f), BearWhite);
                Primitive("Bear eye", parent, PrimitiveType.Cube, new Vector3(side * .22f, 1.14f, 1.09f),
                    new Vector3(.07f, .07f, .04f), Dark);
                for (int front = -1; front <= 1; front += 2)
                {
                    Primitive("Bear leg", parent, PrimitiveType.Cube,
                        new Vector3(side * .31f, 0, front * .40f),
                        new Vector3(.30f, .74f, .34f), BearWhite);
                    Primitive("Bear paw", parent, PrimitiveType.Cube,
                        new Vector3(side * .31f, -.30f, front * .40f + .04f),
                        new Vector3(.34f, .14f, .39f), BearDetail);
                }
            }
            var happy = new GameObject("Bear happy face").transform;
            happy.SetParent(parent, false);
            for (int side = -1; side <= 1; side += 2)
            {
                // Closed "^" eyes, rosy cheeks and smile corners.
                Primitive("Happy eye top", happy, PrimitiveType.Cube, new Vector3(side * .22f, 1.18f, 1.09f),
                    new Vector3(.06f, .04f, .04f), Dark);
                Primitive("Happy eye side", happy, PrimitiveType.Cube, new Vector3(side * .22f - .05f, 1.14f, 1.09f),
                    new Vector3(.04f, .05f, .04f), Dark);
                Primitive("Happy eye side", happy, PrimitiveType.Cube, new Vector3(side * .22f + .05f, 1.14f, 1.09f),
                    new Vector3(.04f, .05f, .04f), Dark);
                Primitive("Happy cheek", happy, PrimitiveType.Cube, new Vector3(side * .28f, 1.0f, 1.085f),
                    new Vector3(.11f, .06f, .03f), Cheek);
                Primitive("Smile corner", happy, PrimitiveType.Cube, new Vector3(side * .1f, .92f, 1.285f),
                    new Vector3(.04f, .06f, .03f), Dark);
            }
            Primitive("Smile", happy, PrimitiveType.Cube, new Vector3(0, .88f, 1.285f),
                new Vector3(.18f, .035f, .03f), Dark);
            happy.gameObject.SetActive(false);
        }

        public static void Fish(Transform parent)
        {
            Primitive("Fish body", parent, PrimitiveType.Cube, new Vector3(0, .10f, 0),
                new Vector3(.54f, .30f, .55f), FishMaterial);
            Primitive("Fish tail stem", parent, PrimitiveType.Cube, new Vector3(0, .10f, -.30f),
                new Vector3(.20f, .16f, .13f), FishMaterial);
            Primitive("Fish tail", parent, PrimitiveType.Cube, new Vector3(0, .10f, -.39f),
                new Vector3(.42f, .24f, .12f), FishFin);
            Primitive("Fish dorsal fin", parent, PrimitiveType.Cube, new Vector3(0, .28f, -.04f),
                new Vector3(.12f, .12f, .22f), FishFin);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("Fish fin", parent, PrimitiveType.Cube, new Vector3(side * .31f, .08f, -.05f),
                    new Vector3(.12f, .10f, .18f), FishFin);
                Primitive("Fish eye", parent, PrimitiveType.Cube, new Vector3(side * .28f, .17f, .17f),
                    new Vector3(.04f, .06f, .06f), Dark);
            }
        }

        public static GameObject[] CrackVisuals(Transform parent, float width, float length)
        {
            var cracks = new GameObject[3];
            for (int i = 0; i < cracks.Length; i++)
            {
                Vector3 position = i == 0 ? new Vector3(0, .21f, -length * .12f) :
                    i == 1 ? new Vector3(width * .08f, .21f, 0) :
                    new Vector3(width * .16f, .21f, length * .12f);
                Vector3 size = i == 1 ? new Vector3(width * .16f, .035f, .06f) :
                    new Vector3(.06f, .035f, length * .24f);
                cracks[i] = Primitive("Crack", parent, PrimitiveType.Cube,
                    position, size, Dark);
                cracks[i].SetActive(false);
            }
            return cracks;
        }

        public static GameObject FloeVisual(Transform parent, bool steppedShape, float width,
            float length, bool thinMaterial = false)
        {
            Vector2[] outline = steppedShape ? new[] {
                new Vector2(.5f, -.35f), new Vector2(.5f, .30f),
                new Vector2(.34f, .30f), new Vector2(.34f, .5f),
                new Vector2(-.30f, .5f), new Vector2(-.30f, .38f),
                new Vector2(-.5f, .38f), new Vector2(-.5f, -.28f),
                new Vector2(-.36f, -.28f), new Vector2(-.36f, -.5f),
                new Vector2(.28f, -.5f), new Vector2(.28f, -.35f)
            } : new[] {
                new Vector2(.5f, -.5f), new Vector2(.5f, .5f),
                new Vector2(-.5f, .5f), new Vector2(-.5f, -.5f)
            };
            int segments = outline.Length;
            const float top = .18f;
            const float bottom = -.13f;
            var mesh = new Mesh { name = steppedShape ? "Stepped ice mesh" : "Sea ice mesh" };
            var vertices = new Vector3[2 + 6 * segments];
            vertices[0] = new Vector3(0, top, 0);
            vertices[1] = new Vector3(0, bottom, 0);
            for (int i = 0; i < segments; i++)
            {
                float x = outline[i].x * width;
                float z = outline[i].y * length;
                vertices[2 + i] = new Vector3(x, top, z);
                vertices[2 + segments + i] = new Vector3(x, bottom, z);
            }
            int[] triangles = new int[segments * 12];
            int cursor = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[cursor++] = 0;
                triangles[cursor++] = 2 + next;
                triangles[cursor++] = 2 + i;
                triangles[cursor++] = 1;
                triangles[cursor++] = 2 + segments + i;
                triangles[cursor++] = 2 + segments + next;
                int side = 2 + 2 * segments + 4 * i;
                vertices[side] = vertices[2 + i];
                vertices[side + 1] = vertices[2 + next];
                vertices[side + 2] = vertices[2 + segments + next];
                vertices[side + 3] = vertices[2 + segments + i];
                triangles[cursor++] = side;
                triangles[cursor++] = side + 1;
                triangles[cursor++] = side + 2;
                triangles[cursor++] = side;
                triangles[cursor++] = side + 2;
                triangles[cursor++] = side + 3;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            var visual = new GameObject("Ice floe");
            visual.transform.SetParent(parent, false);
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            visual.AddComponent<MeshRenderer>().sharedMaterial = thinMaterial ? ThinIce : Ice;
            return visual;
        }

        public static Transform[] FloeFragments(Transform parent, GameObject visual)
        {
            Mesh source = visual.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] sourceVertices = source.vertices;
            int[] sourceTriangles = source.triangles;
            const int segments = 12;
            const int perFragment = 4;
            var fragments = new Transform[3];
            for (int i = 0; i < fragments.Length; i++)
            {
                int start = i * perFragment;
                var vertices = new Vector3[sourceVertices.Length + 8];
                System.Array.Copy(sourceVertices, vertices, sourceVertices.Length);
                var triangles = new int[perFragment * 12 + 12];
                System.Array.Copy(sourceTriangles, start * 12, triangles, 0, perFragment * 12);
                for (int side = 0; side < 2; side++)
                {
                    int outline = (start + (side == 0 ? 0 : perFragment)) % segments;
                    int vertex = sourceVertices.Length + side * 4;
                    vertices[vertex] = sourceVertices[0];
                    vertices[vertex + 1] = sourceVertices[2 + outline];
                    vertices[vertex + 2] = sourceVertices[1];
                    vertices[vertex + 3] = sourceVertices[2 + segments + outline];
                    int triangle = perFragment * 12 + side * 6;
                    triangles[triangle] = vertex;
                    triangles[triangle + 1] = vertex + (side == 0 ? 2 : 1);
                    triangles[triangle + 2] = vertex + (side == 0 ? 1 : 2);
                    triangles[triangle + 3] = vertex + 1;
                    triangles[triangle + 4] = vertex + (side == 0 ? 2 : 3);
                    triangles[triangle + 5] = vertex + (side == 0 ? 3 : 2);
                }
                var mesh = new Mesh { name = "Thin ice fragment mesh", vertices = vertices, triangles = triangles };
                mesh.RecalculateNormals();
                var fragment = new GameObject("Ice fragment");
                fragment.transform.SetParent(parent, false);
                fragment.AddComponent<MeshFilter>().sharedMesh = mesh;
                fragment.AddComponent<MeshRenderer>().sharedMaterial = ThinIce;
                fragments[i] = fragment.transform;
            }
            visual.SetActive(false);
            return fragments;
        }
    }

    public sealed class Bear : MonoBehaviour
    {
        private Rigidbody body;
        private GameController game;
        private Transform model;
        private Transform head;
        private GameObject happyFace;
        private GameObject[] eyes;
        private float sinkingStartY;
        private readonly System.Collections.Generic.HashSet<Floe> floes =
            new System.Collections.Generic.HashSet<Floe>();

        public bool OnIce => floes.Count > 0 || game.OnDestination;
        public int IceCount => floes.Count;
        public float X => body.position.x;
        public Vector3 HeadPosition => head.position;

        public void Initialize(GameController controller)
        {
            game = controller;
            body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var collider = gameObject.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = JourneyData.BearHalfWidth;
            collider.height = 1.2f;
            model = new GameObject("Bear model").transform;
            model.SetParent(transform, false);
            ShapeArt.Bear(model);
            head = model.Find("Bear head");
            happyFace = model.Find("Bear happy face").gameObject;
            var eyeList = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in model)
                if (child.name == "Bear eye") eyeList.Add(child.gameObject);
            eyes = eyeList.ToArray();
        }

        public void ResetPosition()
        {
            floes.Clear();
            body.position = new Vector3(0, .55f, 0);
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            SetHappy(false);
        }

        public bool FacingCamera => Quaternion.Angle(model.localRotation, Quaternion.Euler(0, 180f, 0)) < .5f;
        public bool Happy => happyFace.activeSelf;
        public Vector3 Position => body.position;

        private void SetHappy(bool happy)
        {
            happyFace.SetActive(happy);
            foreach (GameObject eye in eyes) eye.SetActive(!happy);
        }

        // Walks to the center of the destination, turns toward the camera and smiles.
        // Returns true once the bear is standing in place showing its happy face.
        public bool AdvanceCelebration(float delta)
        {
            Vector3 target = JourneyData.ArrivalSpot;
            Vector3 offset = target - body.position;
            Quaternion facing = Quaternion.Euler(0, 180f, 0);
            if (offset.sqrMagnitude > .0001f)
            {
                facing = Quaternion.LookRotation(new Vector3(offset.x, 0, offset.z));
                body.position = Vector3.MoveTowards(body.position, target, JourneyData.ArrivalWalkSpeed * delta);
                transform.position = body.position;
            }
            model.localRotation = Quaternion.RotateTowards(model.localRotation, facing,
                JourneyData.ArrivalTurnSpeed * delta);
            bool done = offset.sqrMagnitude <= .0001f && FacingCamera;
            if (done && !Happy) SetHappy(true);
            return done;
        }

        private void LateUpdate()
        {
            if (game.Phase == GamePhase.Sinking || game.Phase == GamePhase.Failure) return;
            bool swimming = !OnIce && game.Phase == GamePhase.Playing;
            Vector3 position = model.localPosition;
            position.y = game.OnDestination ? 0f :
                Mathf.MoveTowards(position.y, swimming ? -1.05f : 0f, 4f * Time.deltaTime);
            model.localPosition = position;
        }

        public void BeginSinking()
        {
            sinkingStartY = model.localPosition.y;
        }

        public void Sink(float progress)
        {
            Vector3 position = model.localPosition;
            position.y = Mathf.Lerp(sinkingStartY, -2.4f, Mathf.SmoothStep(0f, 1f, progress));
            model.localPosition = position;
        }

        public void Move(float direction, float delta)
        {
            float x = Mathf.Clamp(body.position.x + Mathf.Clamp(direction, -1, 1) * JourneyData.BearSpeed * delta,
                -JourneyData.PlayfieldHalfWidth + JourneyData.BearHalfWidth,
                JourneyData.PlayfieldHalfWidth - JourneyData.BearHalfWidth);
            body.position = new Vector3(x, .55f, 0);
        }

        public bool Overlaps(Floe floe) => floes.Contains(floe);
        public void RemoveFloe(Floe floe) => floes.Remove(floe);

        private void OnTriggerEnter(Collider other)
        {
            if (!game.IsPlaying && game.Phase != GamePhase.Preparing) return;
            Floe floe = other.GetComponent<Floe>();
            if (floe != null && !floe.IsBroken)
            {
                floes.Add(floe);
                floe.Touch();
            }
            Pickup pickup = other.GetComponent<Pickup>();
            if (pickup != null) pickup.Collect(game);
        }

        private void OnTriggerExit(Collider other)
        {
            Floe floe = other.GetComponent<Floe>();
            if (floe != null) floes.Remove(floe);
        }
    }

    public sealed class Floe : MonoBehaviour
    {
        private static readonly Vector3[] FragmentDirections = {
            new Vector3(.65f, 0f, .35f),
            new Vector3(-.65f, 0f, .3f),
            new Vector3(0f, 0f, -.65f)
        };
        private bool thin;
        private bool touched;
        private float crackTime;
        private float crackDuration;
        private float fragmentTime;
        private GameObject visual;
        private GameObject[] cracks;
        private Transform[] fragments;
        private BoxCollider iceCollider;

        public bool IsBroken { get; private set; }
        public float CrackProgress => Mathf.Clamp01(crackTime / crackDuration);

        public void Initialize(bool isThin, float width, float length = 2.45f,
            float crackDuration = JourneyData.ThinIceBreakTime, bool steppedShape = false)
        {
            thin = isThin;
            this.crackDuration = crackDuration;
            iceCollider = gameObject.AddComponent<BoxCollider>();
            iceCollider.isTrigger = true;
            iceCollider.size = new Vector3(width, .38f, length);
            iceCollider.center = new Vector3(0, .06f, 0);
            visual = ShapeArt.FloeVisual(transform, thin || steppedShape, width, length, thin);
            if (thin) cracks = ShapeArt.CrackVisuals(transform, width, length);
        }

        public void Touch()
        {
            if (thin) touched = true;
        }

        public void Advance(float delta, Bear bear, GameController game)
        {
            if (!thin || !touched) return;
            if (IsBroken)
            {
                fragmentTime += delta;
                float sink = Mathf.Clamp01(fragmentTime / (.6f * JourneyData.GameplaySpeed));
                PositionFragments(1f, sink);
                if (sink >= 1f) gameObject.SetActive(false);
                return;
            }
            crackTime += delta;
            if (fragments == null)
                for (int i = 0; i < cracks.Length; i++)
                    cracks[i].SetActive(CrackProgress >= i / 3f);
            if (CrackProgress >= .5f)
            {
                if (fragments == null)
                {
                    fragments = ShapeArt.FloeFragments(transform, visual);
                    foreach (GameObject crack in cracks) crack.SetActive(false);
                }
                PositionFragments((CrackProgress - .5f) * 2f, 0f);
            }
            if (crackTime < crackDuration) return;
            IsBroken = true;
            bool wasOnFloe = bear.Overlaps(this);
            bear.RemoveFloe(this);
            iceCollider.enabled = false;
            if (wasOnFloe) game.LoseStamina(5f);
        }

        private void PositionFragments(float spread, float sink)
        {
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i].localPosition = FragmentDirections[i] * (spread + sink * .4f) +
                    Vector3.down * (sink * .8f);
                fragments[i].localRotation = Quaternion.Euler(
                    FragmentDirections[i].z * sink * 18f, 0f, -FragmentDirections[i].x * sink * 18f);
            }
        }

        private void OnDestroy()
        {
            if (visual != null) Destroy(visual.GetComponent<MeshFilter>().sharedMesh);
            if (fragments != null)
                foreach (Transform fragment in fragments)
                    if (fragment != null) Destroy(fragment.GetComponent<MeshFilter>().sharedMesh);
        }
    }

    public sealed class Pickup : MonoBehaviour
    {
        private bool collected;
        private float centerX;
        private float strafeAmplitude;
        private float strafeFrequency;
        private float strafePhase;

        public void Initialize()
        {
            centerX = transform.position.x;
            strafeAmplitude = Mathf.Min(Random.Range(.45f, .9f),
                JourneyData.PlayfieldHalfWidth - .42f - Mathf.Abs(centerX));
            strafeFrequency = Random.Range(1.4f, 2.1f);
            strafePhase = Random.Range(0f, Mathf.PI * 2f);
            var collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = .42f;
            ShapeArt.Fish(transform);
        }

        public float HorizontalPosition(float elapsed)
        {
            return centerX + Mathf.Sin(elapsed * strafeFrequency + strafePhase) * strafeAmplitude;
        }

        public void Collect(GameController game)
        {
            if (collected) return;
            collected = true;
            game.CollectFish();
            gameObject.SetActive(false);
        }
    }
}
