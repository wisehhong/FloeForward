using System;
using System.Collections.Generic;
using UnityEngine;

namespace FloeForward
{
    public enum ResourceKind { Solid, Thin, Fish }

    [Serializable]
    public struct ResourceSpawn
    {
        public ResourceKind Kind;
        public float X;
        public float Width;
        public float Height;
        public float Arrival;

        public ResourceSpawn(ResourceKind kind, float x, float width, float height, float arrival)
        {
            Kind = kind;
            X = x;
            Width = width;
            Height = height;
            Arrival = arrival;
        }
    }

    public sealed class Choice
    {
        public readonly int FloeChange;
        public readonly bool Support;

        public Choice(int floeChange, bool support = false)
        {
            FloeChange = floeChange;
            Support = support;
        }
    }

    public static class JourneyData
    {
        public static readonly int[] Years = { 2016, 2026, 2036 };
        public const float GameplaySpeed = 1.5f;
        public const float PreparationDuration = 3f;
        public const float StageDuration = 30f;
        public const float SwimDrain = 4f;
        public const float SupportDiscount = 0.5f;
        public const float MaxStamina = 100f;
        public const float PlayfieldHalfWidth = 7f;
        public const float BearHalfWidth = 0.38f;
        public const float BearHalfLength = BearHalfWidth;
        public const float ScrollSpeed = 6.4f;
        public const float BearSpeed = 2f;
        public const float ArrivalFloeInset = .9f;
        public const float ArrivalPause = 3f;
        public const float ArrivalWalkSpeed = 3f;
        public const float ArrivalTurnSpeed = 360f;
        public const float SinkingDuration = 3f;
        public const float ThinIceBreakRealSeconds = 1f;
        public const float ThinIceBreakTime = ThinIceBreakRealSeconds * GameplaySpeed;
        public static readonly Vector3 ArrivalSpot = new Vector3(0f, .55f, 2.2f);

        // Arrival times are when the resource center passes the bear, not spawn times.
        private static readonly ResourceSpawn[][] BaseLayouts =
        {
            new[] {
                S(.47f, 1, 2.4f, 2.2f), S(.32f, 5, 1.7f, 2.7f),
                S(.62f, 9, 2.6f, 2.05f), S(.40f, 13, 1.9f, 2.5f),
                S(.70f, 17, 2.2f, 2.85f), S(.36f, 21, 2.7f, 2.1f),
                S(.58f, 25, 1.8f, 2.6f), S(.45f, 29, 2.35f, 2.35f),
                F(.53f, 3), F(.38f, 7), F(.51f, 15), F(.57f, 23), F(.43f, 27),
                T(.85f, 9.3f, 1.35f, 4.6f), T(.15f, 11.5f, 1.6f, 5.4f),
                T(.88f, 19.5f, 1.5f, 4.8f), T(.2f, 26f, 1.7f, 5.8f)
            },
            new[] {
                S(.43f, 1, 2.2f, 2.6f), S(.65f, 5, 1.65f, 2.2f),
                S(.34f, 11, 2.7f, 2.35f), S(.59f, 17, 1.9f, 2.75f),
                S(.39f, 23, 2.4f, 2.1f), S(.56f, 29, 1.8f, 2.5f),
                F(.52f, 3), F(.44f, 9), F(.63f, 19), F(.42f, 27),
                T(.84f, 11.35f, 1.5f, 4.9f), T(.25f, 25.4f, 1.9f, 5.6f),
                T(.12f, 7.5f, 1.6f, 5.2f), T(.88f, 14.5f, 1.4f, 6f),
                T(.78f, 20.6f, 1.75f, 4.4f)
            },
            new[] {
                S(.40f, 3, 2.5f, 2.15f), S(.65f, 11, 1.9f, 2.65f),
                S(.35f, 19, 1.6f, 2.4f), S(.60f, 27, 2.3f, 1.95f),
                F(.48f, 7), F(.56f, 17), F(.42f, 25),
                T(.85f, 11.25f, 1.45f, 5f), T(.23f, 15.4f, 2f, 5.8f),
                T(.85f, 26.65f, 1.2f, 4.6f), T(.2f, 5.5f, 1.7f, 5.2f),
                T(.8f, 21.5f, 1.6f, 6.2f), T(.18f, 8.8f, 1.5f, 4.4f)
            }
        };

        private static readonly ResourceSpawn[][] ExtraFloes =
        {
            Array.Empty<ResourceSpawn>(),
            new[] { S(.48f, 7, 2.3f, 2.25f), S(.57f, 15, 1.8f, 2.7f) },
            new[] { S(.53f, 1, 2.1f, 2.5f), S(.44f, 5, 2.6f, 2f) }
        };

        public static readonly Choice[][] Choices =
        {
            new[] {
                new Choice(1),
                new Choice(0),
                new Choice(-1, true)
            },
            new[] {
                new Choice(1),
                new Choice(0),
                new Choice(-1, true)
            }
        };

        public static List<ResourceSpawn> Layout(int stage, int adjustment)
        {
            var result = new List<ResourceSpawn>();
            int solidCount = 0;
            foreach (ResourceSpawn item in BaseLayouts[stage])
                if (item.Kind == ResourceKind.Solid) solidCount++;
            int keep = Mathf.Max(0, solidCount + Mathf.Min(0, adjustment));
            int seen = 0;
            foreach (ResourceSpawn item in BaseLayouts[stage])
            {
                if (item.Kind != ResourceKind.Solid || seen++ < keep)
                    result.Add(item);
            }
            for (int i = 0; i < Mathf.Min(adjustment, ExtraFloes[stage].Length); i++)
                result.Add(ExtraFloes[stage][i]);
            return result;
        }

        public static float WorldX(ResourceSpawn item)
        {
            return Mathf.Lerp(-PlayfieldHalfWidth + item.Width / 2f,
                PlayfieldHalfWidth - item.Width / 2f, item.X);
        }

        private static ResourceSpawn S(float x, float arrival, float width, float height) =>
            new ResourceSpawn(ResourceKind.Solid, x, width, height, arrival);
        private static ResourceSpawn T(float x, float arrival, float width, float height) =>
            new ResourceSpawn(ResourceKind.Thin, x, width, height, arrival);
        private static ResourceSpawn F(float x, float arrival) =>
            new ResourceSpawn(ResourceKind.Fish, x, .7f, .7f, arrival);
    }
}
