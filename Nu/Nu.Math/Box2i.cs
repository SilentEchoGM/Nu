﻿//
// Box2.cs
//
// Copyright (C) 2019 OpenTK
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//

using System;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Nu
{
    /// <summary>
    /// Defines an axis-aligned 2D box (rectangle) in integers.
    /// Copied from - https://github.com/opentk/opentk/blob/opentk5.0/src/OpenTK.Mathematics/Geometry/Box2.cs
    /// Heavily modified by BGE to more closely conform to System.Numerics and use a size-preserving representation
    /// ([pos, siz] instead of [min, max]), as well as using integers.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Box2i : IEquatable<Box2i>
    {
        /// <summary>
        /// The position of the box.
        /// </summary>
        public Vector2i Position;

        /// <summary>
        /// The size of the box.
        /// </summary>
        public Vector2i Size;

        /// <summary>
        /// Initializes a new instance of the <see cref="Box2i"/> struct.
        /// </summary>
        public Box2i(Vector2i position, Vector2i size)
        {
            Position = position;
            Size = size;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Box2i"/> struct.
        /// </summary>
        public Box2i(int positionX, int positionY, int sizeX, int sizeY)
        {
            Position = new Vector2i(positionX, positionY);
            Size = new Vector2i(sizeX, sizeY);
        }

        /// <summary>
        /// Gets a box with a position 0,0 with the a size of 0,0.
        /// </summary>
        public static readonly Box2i Zero = default(Box2i);

        /// <summary>
        /// Gets a box with a position 0,0 with the a size of 1,1.
        /// </summary>
        public static readonly Box2i Unit = new Box2i(new Vector2i(0, 0), new Vector2i(1, 1));

        /// <summary>
        /// Create a bounding box by enclosing two points.
        /// </summary>
        public static Box2i Enclose(Vector2i point, Vector2i point2)
        {
            var position = new Vector2i(
                Math.Min(point.X, point2.X),
                Math.Min(point.Y, point2.Y));
            var position2 = new Vector2i(
                Math.Max(point.X, point2.X),
                Math.Max(point.Y, point2.Y));
            return new Box2i(position, position2 - position);
        }

        /// <summary>
        /// Equality comparator.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        public static bool operator ==(Box2i left, Box2i right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality comparator.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        public static bool operator !=(Box2i left, Box2i right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Box2i box && Equals(box);
        }

        /// <inheritdoc/>
        public bool Equals(Box2i other)
        {
            return
                Position.Equals(other.Position) &&
                Size.Equals(other.Size);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = Position.GetHashCode();
            hashCode = (hashCode * 397) ^ Size.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return String.Format("{0}\n{1}", Position, Size);
        }
    }
}
