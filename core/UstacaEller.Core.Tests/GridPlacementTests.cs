using UstacaEller.Core.Geometry;
using UstacaEller.Core.Mechanics;
using Xunit;

namespace UstacaEller.Core.Tests
{
    public class GridPlacementTests
    {
        /// <summary>The kitchen shelf: 4 columns, 2 rows, cells 100x100.</summary>
        private static GridPlacement Shelf() =>
            new GridPlacement(new Rect(0, 0, 400, 200), columns: 4, rows: 2);

        [Fact]
        public void ObjectLandsInTheCellItWasDroppedOn()
        {
            GridPlacement shelf = Shelf();

            Assert.True(shelf.TryPlace("jar_flour", new Vec2(250, 150), out GridCell cell));

            Assert.Equal(new GridCell(2, 1), cell);
            Assert.Equal("jar_flour", shelf.OccupantOf(cell));
        }

        [Fact]
        public void ADropBetweenCellsPicksTheNearestOne()
        {
            GridPlacement shelf = Shelf();

            shelf.TryPlace("jar_flour", new Vec2(149, 50), out GridCell cell);

            Assert.Equal(new GridCell(1, 0), cell);
        }

        [Fact]
        public void SecondObjectOnAnOccupiedCellGoesToTheNextNearestFreeOne()
        {
            GridPlacement shelf = Shelf();
            shelf.TryPlace("jar_flour", new Vec2(150, 50), out GridCell first);

            Assert.True(shelf.TryPlace("jar_sugar", new Vec2(150, 50), out GridCell second));

            Assert.NotEqual(first, second);
            Assert.Equal("jar_flour", shelf.OccupantOf(first));
            Assert.Equal("jar_sugar", shelf.OccupantOf(second));
        }

        [Fact]
        public void PlacingTheSameObjectAgainMovesItInsteadOfConsumingTwoCells()
        {
            GridPlacement shelf = Shelf();
            shelf.TryPlace("jar_flour", new Vec2(50, 50), out GridCell first);

            shelf.TryPlace("jar_flour", new Vec2(350, 150), out GridCell second);

            Assert.NotEqual(first, second);
            Assert.Equal(1, shelf.OccupiedCount);
            Assert.Null(shelf.OccupantOf(first));
        }

        [Fact]
        public void AFullShelfRefusesMore()
        {
            GridPlacement shelf = Shelf();
            for (int i = 0; i < shelf.Capacity; i++)
            {
                Assert.True(shelf.TryPlace($"jar_{i}", new Vec2(200, 100), out _));
            }

            Assert.True(shelf.IsFull);
            Assert.False(shelf.TryPlace("jar_extra", new Vec2(200, 100), out _));
        }

        [Fact]
        public void RemovingAnObjectFreesItsCell()
        {
            GridPlacement shelf = Shelf();
            shelf.TryPlace("jar_flour", new Vec2(50, 50), out GridCell cell);

            Assert.True(shelf.Remove("jar_flour"));

            Assert.False(shelf.IsOccupied(cell));
            Assert.Equal(0, shelf.OccupiedCount);
        }

        [Fact]
        public void RemovingSomethingThatIsNotThereIsHarmless()
        {
            Assert.False(Shelf().Remove("jar_ghost"));
        }

        [Fact]
        public void CellCentresLineUpWithTheShelfBounds()
        {
            GridPlacement shelf = Shelf();

            Assert.Equal(new Vec2(50, 50), shelf.CenterOf(new GridCell(0, 0)));
            Assert.Equal(new Vec2(350, 150), shelf.CenterOf(new GridCell(3, 1)));
        }
    }
}
