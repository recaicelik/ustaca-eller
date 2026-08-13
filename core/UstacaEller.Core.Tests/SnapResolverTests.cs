using System.Collections.Generic;
using UstacaEller.Core.Geometry;
using UstacaEller.Core.Mechanics;
using Xunit;

namespace UstacaEller.Core.Tests
{
    public class SnapResolverTests
    {
        private static readonly Vec2 Origin = new Vec2(100, 100);

        private static SnapZone Tray() =>
            new SnapZone("tray_area", new Rect(200, 200, 100, 50), new[] { "cookie_*" }, snapRadius: 40f);

        private static SnapZone Counter() =>
            new SnapZone("counter_area", new Rect(0, 0, 60, 60), new[] { "dough_a" }, snapRadius: 40f);

        [Fact]
        public void DropInsideAnAcceptingZoneSnaps()
        {
            SnapDecision decision = SnapResolver.Resolve(
                "cookie_star", new Vec2(250, 220), Origin, new List<SnapZone> { Tray() });

            Assert.Equal(SnapOutcome.Snapped, decision.Outcome);
            Assert.Equal("tray_area", decision.ZoneId);
            Assert.Equal(new Vec2(250, 220), decision.Position);
        }

        [Fact]
        public void DropNearTheZoneStillSnapsAndIsPulledInside()
        {
            // 20px outside the right edge, inside the 40px snap radius. Precision is not
            // something this age group has, so "close" has to count.
            SnapDecision decision = SnapResolver.Resolve(
                "cookie_star", new Vec2(320, 220), Origin, new List<SnapZone> { Tray() });

            Assert.Equal(SnapOutcome.Snapped, decision.Outcome);
            Assert.Equal(300f, decision.Position.X);
            Assert.Equal(220f, decision.Position.Y);
        }

        [Fact]
        public void DropBeyondTheRadiusReturnsToOrigin()
        {
            SnapDecision decision = SnapResolver.Resolve(
                "cookie_star", new Vec2(600, 600), Origin, new List<SnapZone> { Tray() });

            Assert.Equal(SnapOutcome.ReturnedToOrigin, decision.Outcome);
            Assert.Equal(Origin, decision.Position);
            Assert.Null(decision.ZoneId);
        }

        [Fact]
        public void ObjectsMayBeLeftLooseWhenTheSceneSaysSo()
        {
            SnapDecision decision = SnapResolver.Resolve(
                "cookie_star", new Vec2(600, 600), Origin, new List<SnapZone> { Tray() }, returnOnMiss: false);

            Assert.Equal(SnapOutcome.LeftWhereDropped, decision.Outcome);
            Assert.Equal(new Vec2(600, 600), decision.Position);
        }

        [Fact]
        public void ZoneThatDoesNotAcceptTheObjectIsIgnored()
        {
            // Dropped right on the counter, but the counter only takes dough_a.
            SnapDecision decision = SnapResolver.Resolve(
                "cookie_star", new Vec2(30, 30), Origin, new List<SnapZone> { Counter() });

            Assert.Equal(SnapOutcome.ReturnedToOrigin, decision.Outcome);
        }

        [Fact]
        public void ExactIdsMatchAsWellAsWildcards()
        {
            SnapDecision decision = SnapResolver.Resolve(
                "dough_a", new Vec2(30, 30), Origin, new List<SnapZone> { Counter() });

            Assert.Equal(SnapOutcome.Snapped, decision.Outcome);
            Assert.Equal("counter_area", decision.ZoneId);
        }

        [Fact]
        public void WildcardDoesNotMatchAnUnrelatedId()
        {
            SnapDecision decision = SnapResolver.Resolve(
                "cake", new Vec2(250, 220), Origin, new List<SnapZone> { Tray() });

            Assert.Equal(SnapOutcome.ReturnedToOrigin, decision.Outcome);
        }

        [Fact]
        public void NearestAcceptingZoneWins()
        {
            var near = new SnapZone("near", new Rect(200, 200, 40, 40), new[] { "cookie_*" }, snapRadius: 100f);
            var far = new SnapZone("far", new Rect(400, 200, 40, 40), new[] { "cookie_*" }, snapRadius: 400f);

            SnapDecision decision = SnapResolver.Resolve(
                "cookie_moon", new Vec2(260, 220), Origin, new List<SnapZone> { far, near });

            Assert.Equal("near", decision.ZoneId);
        }

        [Fact]
        public void NoZonesAtAllReturnsToOrigin()
        {
            SnapDecision decision = SnapResolver.Resolve("cookie_star", new Vec2(5, 5), Origin, null);

            Assert.Equal(SnapOutcome.ReturnedToOrigin, decision.Outcome);
        }
    }
}
