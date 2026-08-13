using System;
using System.Collections.Generic;
using UstacaEller.Core.Geometry;

namespace UstacaEller.Core.Mechanics
{
    public readonly struct GridCell : IEquatable<GridCell>
    {
        public readonly int Column;
        public readonly int Row;

        public GridCell(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public bool Equals(GridCell other) => Column == other.Column && Row == other.Row;

        public override bool Equals(object obj) => obj is GridCell other && Equals(other);

        public override int GetHashCode() => unchecked((Column * 397) ^ Row);

        public override string ToString() => $"({Column}, {Row})";
    }

    /// <summary>
    /// The building mechanic.
    ///
    /// There is no physics here on purpose. Realistic toppling blocks are the most
    /// expensive thing to simulate on an entry-level Android phone, and for this age
    /// group they mostly produce frustration: a tower that falls over is a failure the
    /// child did not choose. Snapping to a grid with a settle animation is cheaper and
    /// reads as success every time.
    /// </summary>
    public sealed class GridPlacement
    {
        private readonly Dictionary<GridCell, string> _occupants = new Dictionary<GridCell, string>();

        public GridPlacement(Rect bounds, int columns, int rows)
        {
            if (columns < 1) throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows < 1) throw new ArgumentOutOfRangeException(nameof(rows));

            Bounds = bounds;
            Columns = columns;
            Rows = rows;
        }

        public Rect Bounds { get; }

        public int Columns { get; }

        public int Rows { get; }

        public int Capacity => Columns * Rows;

        public int OccupiedCount => _occupants.Count;

        public bool IsFull => _occupants.Count >= Capacity;

        public float CellWidth => Bounds.Width / Columns;

        public float CellHeight => Bounds.Height / Rows;

        public Vec2 CenterOf(GridCell cell) => new Vec2(
            Bounds.X + (cell.Column + 0.5f) * CellWidth,
            Bounds.Y + (cell.Row + 0.5f) * CellHeight);

        public bool IsOccupied(GridCell cell) => _occupants.ContainsKey(cell);

        public string OccupantOf(GridCell cell) => _occupants.TryGetValue(cell, out string id) ? id : null;

        /// <summary>
        /// Places an object at the free cell nearest the drop point. Returns false only
        /// when every cell is taken — a near miss still places, because aiming at a
        /// specific cell is beyond this age group.
        /// </summary>
        public bool TryPlace(string objectId, Vec2 dropPosition, out GridCell cell)
        {
            if (objectId == null) throw new ArgumentNullException(nameof(objectId));

            Remove(objectId);

            if (!TryFindNearestFreeCell(dropPosition, out cell))
            {
                return false;
            }

            _occupants[cell] = objectId;
            return true;
        }

        public bool Remove(string objectId)
        {
            // Find first, remove after: mutating a Dictionary mid-enumeration is only
            // safe on newer runtimes, and this assembly also runs on Unity's Mono.
            bool found = false;
            GridCell occupied = default;

            foreach (KeyValuePair<GridCell, string> entry in _occupants)
            {
                if (!string.Equals(entry.Value, objectId, StringComparison.Ordinal)) continue;

                occupied = entry.Key;
                found = true;
                break;
            }

            return found && _occupants.Remove(occupied);
        }

        private bool TryFindNearestFreeCell(Vec2 position, out GridCell nearest)
        {
            nearest = default;
            float bestDistance = float.MaxValue;
            bool found = false;

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    var candidate = new GridCell(column, row);
                    if (IsOccupied(candidate)) continue;

                    float distance = Vec2.Distance(position, CenterOf(candidate));
                    if (distance >= bestDistance) continue;

                    bestDistance = distance;
                    nearest = candidate;
                    found = true;
                }
            }

            return found;
        }
    }
}
