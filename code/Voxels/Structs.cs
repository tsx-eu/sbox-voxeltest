using System;
using System.Diagnostics.CodeAnalysis;

namespace Voxels
{
	public struct Vector3i : IEquatable<Vector3i>
	{
		public static Vector3i Zero => new Vector3i( 0, 0, 0 );
		public static Vector3i One => new Vector3i( 1, 1, 1 );

		public static implicit operator Vector3i( int value )
		{
			return new Vector3i( value, value, value );
		}

		public static implicit operator Vector3( Vector3i vector )
		{
			return new Vector3( vector.x, vector.y, vector.z );
		}

		public static Vector3i operator +( Vector3i lhs, Vector3i rhs )
		{
			return new Vector3i( lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z );
		}

		public static Vector3i operator -( Vector3i lhs, Vector3i rhs )
		{
			return new Vector3i( lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z );
		}

		public static Vector3i operator -( Vector3i vector )
		{
			return new Vector3i( -vector.x, -vector.y, -vector.z );
		}

		public static bool operator ==( Vector3i lhs, Vector3i rhs )
		{
			return lhs.Equals( rhs );
		}

		public static bool operator !=( Vector3i lhs, Vector3i rhs )
		{
			return !lhs.Equals( rhs );
		}

		public static Vector3i Min( Vector3i lhs, Vector3i rhs )
		{
			return new Vector3i( Math.Min( lhs.x, rhs.x ), Math.Min( lhs.y, rhs.y ), Math.Min( lhs.z, rhs.z ) );
		}

		public static Vector3i Max( Vector3i lhs, Vector3i rhs )
		{
			return new Vector3i( Math.Max( lhs.x, rhs.x ), Math.Max( lhs.y, rhs.y ), Math.Max( lhs.z, rhs.z ) );
		}

		public static Vector3i Clamp( Vector3i vector, Vector3i min, Vector3i max )
		{
			return new Vector3i( Math.Clamp( vector.x, min.x, max.x ), Math.Clamp( vector.y, min.y, max.y ), Math.Clamp( vector.z, min.z, max.z ) );
		}

		public static Vector3i Floor( Vector3 vector )
		{
			return new Vector3i( (int)Math.Floor( vector.x ), (int)Math.Floor( vector.y ), (int)Math.Floor( vector.z ) );
		}

		public static Vector3i Ceiling( Vector3 vector )
		{
			return new Vector3i( (int)Math.Ceiling( vector.x ), (int)Math.Ceiling( vector.y ), (int)Math.Ceiling( vector.z ) );
		}

		public static Vector3i Round( Vector3 vector )
		{
			return new Vector3i( (int)Math.Round( vector.x ), (int)Math.Round( vector.y ), (int)Math.Round( vector.z ) );
		}

		public static int Dot( Vector3i lhs, Vector3i rhs )
		{
			return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
		}

		public int x;
		public int y;
		public int z;

		public Vector3i( int x, int y, int z )
			=> (this.x, this.y, this.z) = (x, y, z);

		public bool Equals( Vector3i other )
		{
			return x == other.x && y == other.y && z == other.z;
		}

		public override bool Equals( [NotNullWhen( true )] object obj )
		{
			return obj is Vector3i vector && Equals( vector );
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( x, y, z );
		}

		public override string ToString()
		{
			return $"({x} {y} {z})";
		}
	}

	public enum CubeFace
	{
		XMin,
		YMin,
		ZMin,
		XMax,
		YMax,
		ZMax
	}

	public readonly struct CubeVertex : IEquatable<CubeVertex>
	{
		public static readonly CubeVertex X0Y0Z0 = new CubeVertex( 0, 0, 0 );
		public static readonly CubeVertex X1Y0Z0 = new CubeVertex( 1, 0, 0 );
		public static readonly CubeVertex X0Y1Z0 = new CubeVertex( 0, 1, 0 );
		public static readonly CubeVertex X1Y1Z0 = new CubeVertex( 1, 1, 0 );
		public static readonly CubeVertex X0Y0Z1 = new CubeVertex( 0, 0, 1 );
		public static readonly CubeVertex X1Y0Z1 = new CubeVertex( 1, 0, 1 );
		public static readonly CubeVertex X0Y1Z1 = new CubeVertex( 0, 1, 1 );
		public static readonly CubeVertex X1Y1Z1 = new CubeVertex( 1, 1, 1 );

		public static explicit operator int( CubeVertex vertex )
		{
			return vertex.Index;
		}

		public static implicit operator Vector3i( CubeVertex vertex )
		{
			return new Vector3i( vertex.X, vertex.Y, vertex.Z );
		}

		public static implicit operator Vector3( CubeVertex vertex )
		{
			return new Vector3( vertex.X, vertex.Y, vertex.Z );
		}

		public static void GetFace( CubeFace face,
			out CubeVertex v00, out CubeVertex v01,
			out CubeVertex v10, out CubeVertex v11 )
		{
			switch ( face )
			{
				case CubeFace.XMin:
					v00 = X0Y0Z0;
					v01 = X0Y1Z0;
					v10 = X0Y0Z1;
					v11 = X0Y1Z1;
					break;

				case CubeFace.XMax:
					v00 = X1Y0Z1;
					v01 = X1Y1Z1;
					v10 = X1Y0Z0;
					v11 = X1Y1Z0;
					break;

				case CubeFace.YMin:
					v00 = X0Y0Z0;
					v01 = X0Y0Z1;
					v10 = X1Y0Z0;
					v11 = X1Y0Z1;
					break;

				case CubeFace.YMax:
					v00 = X1Y1Z0;
					v01 = X1Y1Z1;
					v10 = X0Y1Z0;
					v11 = X0Y1Z1;
					break;

				case CubeFace.ZMin:
					v00 = X1Y0Z0;
					v01 = X1Y1Z0;
					v10 = X0Y0Z0;
					v11 = X0Y1Z0;
					break;

				case CubeFace.ZMax:
					v00 = X0Y0Z1;
					v01 = X0Y1Z1;
					v10 = X1Y0Z1;
					v11 = X1Y1Z1;
					break;

				default:
					throw new ArgumentException();
			}
		}

		public readonly byte Index;

		public int X => Index & 1;
		public int Y => (Index & 2) >> 1;
		public int Z => (Index & 4) >> 2;

		public CubeVertex( int x, int y, int z )
		{
			Index = (byte)((x & 1) | ((y & 1) << 1) | ((z & 1) << 2));
		}

		public bool Equals( CubeVertex other )
		{
			return Index == other.Index;
		}

		public override bool Equals( object obj )
		{
			return obj is CubeVertex other && Equals( other );
		}

		public override int GetHashCode()
		{
			return Index.GetHashCode();
		}
	}

	public readonly struct Intersection : IEquatable<Intersection>
	{
		public readonly CubeVertex Vert0;
		public readonly CubeVertex Vert1;
		public readonly float Along;
		public readonly Vector3 Pos;

		public Intersection( CubeVertex vert0, CubeVertex vert1, float along )
		{
			Vert0 = vert0;
			Vert1 = vert1;
			Along = along;
			Pos = vert0 + ((Vector3)vert1 - vert0) * along;
		}

		public bool Equals( Intersection other )
		{
			return Vert0.Equals( other.Vert0 ) && Vert1.Equals( other.Vert1 ) || Vert0.Equals( other.Vert1 ) && Vert1.Equals( other.Vert0 );
		}

		public override bool Equals( object obj )
		{
			return obj is Intersection other && Equals( other );
		}

		public override int GetHashCode()
		{
			return Vert0.Index <= Vert1.Index ? Vert0.Index | (Vert1.Index << 3) : Vert1.Index | (Vert0.Index << 3);
		}
	}
}
