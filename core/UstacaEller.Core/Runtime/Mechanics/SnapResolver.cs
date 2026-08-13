using System;
using System.Collections.Generic;
using UstacaEller.Core.Geometry;

namespace UstacaEller.Core.Mechanics
{
    public sealed class SnapZone
    {
        public SnapZone(string id, Rect bounds, IReadOnlyList<string> accepts, float snapRadius)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Bounds = bounds;
            Accepts = accepts ?? Array.Empty<string>();
            SnapRadius = snapRadius;
        }

        public string Id { get; }

        public Rect Bounds { get; }

        public IReadOnlyList<string> Accepts { get; }

        public float SnapRadius { get; }

        public bool AcceptsObject(string objectId)
        {
            foreach (string pattern in Accepts)
            {
                if (IdPattern.Matches(pattern, objectId)) return true;
            }

            return false;
        }
    }

    public enum SnapOutcome
    {
        /// <summary>Landed in an accepting zone and locked into place.</summary>
        Snapped,

        /// <summary>Missed, and the object travelled back to where it started.</summary>
        ReturnedToOrigin,

        /// <summary>Missed, and the object stays wherever it was dropped.</summary>
        LeftWhereDropped,
    }

    public readonly struct SnapDecision
    {
        public readonly SnapOutcome Outcome;
        public readonly string ZoneId;
        public readonly Vec2 Position;

        public SnapDecision(SnapOutcome outcome, string zoneId, Vec2 position)
        {
            Outcome = outcome;
            ZoneId = zoneId;
            Position = position;
        }
    }

    /// <summary>
    /// Decides where a dragged object ends up when the child lets go.
    ///
    /// The default on a miss is to fly back to where it started. That is not a
    /// nicety: an object dropped somewhere invalid and left there reads as lost,
    /// and a lost toy is where a four-year-old stops playing.
    /// </summary>
    public static class SnapResolver
    {
        public static SnapDecision Resolve(
            string objectId,
            Vec2 dropPosition,
            Vec2 originPosition,
            IReadOnlyList<SnapZone> zones,
            bool returnOnMiss = true)
        {
            if (objectId == null) throw new ArgumentNullException(nameof(objectId));

            SnapZone best = null;
            float bestDistance = float.MaxValue;

            foreach (SnapZone zone in zones ?? Array.Empty<SnapZone>())
            {
                if (!zone.AcceptsObject(objectId)) continue;

                float distance = zone.Bounds.DistanceTo(dropPosition);
                if (distance > zone.SnapRadius) continue;

                // Ties are broken by proximity only; overlapping zones that accept the
                // same object are a scene authoring mistake, not a runtime concern.
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = zone;
            }

            if (best != null)
            {
                return new SnapDecision(SnapOutcome.Snapped, best.Id, best.Bounds.ClosestPoint(dropPosition));
            }

            return returnOnMiss
                ? new SnapDecision(SnapOutcome.ReturnedToOrigin, null, originPosition)
                : new SnapDecision(SnapOutcome.LeftWhereDropped, null, dropPosition);
        }
    }
}
