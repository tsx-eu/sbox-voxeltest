using System;

namespace Voxels
{
	using static Helpers;

	public interface ISignedDistanceField
	{
		Bounds Bounds { get; }

		float this[Vector3 pos] { get; }
	}

	public readonly struct SphereSdf : ISignedDistanceField
	{
		public Vector3 Center { get; }
		public float Radius { get; }
		public float MaxDistance { get; }

		private readonly float _invMaxDistance;

		public SphereSdf( Vector3 center, float radius, float maxDistance )
		{
			Center = center;
			Radius = radius;
			MaxDistance = maxDistance;

			_invMaxDistance = 1f / maxDistance;
		}

		public Bounds Bounds => new Bounds( Center - Radius - MaxDistance, Center + Radius + MaxDistance );

		public float this[Vector3 pos] => (Radius - (Center - pos).Length) * _invMaxDistance;
	}

	public readonly struct BoundsSdf : ISignedDistanceField
	{
		public Bounds Bounds { get; }
		public float MaxDistance { get; }

		private readonly float _invMaxDistance;

		Bounds ISignedDistanceField.Bounds => Bounds.Extended( MaxDistance );

		public BoundsSdf( Bounds bounds, float maxDistance )
		{
			Bounds = bounds;
			MaxDistance = maxDistance;

			_invMaxDistance = 1f / maxDistance;
		}

		public BoundsSdf( Vector3 min, Vector3 max, float maxDistance )
		{
			Bounds = new Bounds( min, max );
			MaxDistance = maxDistance;

			_invMaxDistance = 1f / maxDistance;
		}

		public float this[Vector3 pos]
		{
			get
			{
				var dist3 = Vector3.Min( pos - Bounds.Min, Bounds.Max - pos );
				return Math.Min( dist3.x, Math.Min( dist3.y, dist3.z ) ) * _invMaxDistance;
			}
		}
	}

	public readonly struct VoxelArraySdf : ISignedDistanceField
	{
		public Voxel[] Array { get; }
		public Vector3i Size { get; }

		private readonly Vector3i _stride;

		public VoxelArraySdf( Voxel[] array, Vector3i size )
		{
			Array = array;
			Size = size;

			_stride = new Vector3i( 1, size.x, size.x * size.y );
		}

		public Bounds Bounds => new Bounds( Vector3.Zero, Vector3.One );

		public float this[Vector3 pos]
		{
			get
			{
				var local = pos * (Size - 1);

				var floored = Vector3i.Floor( local );

				var min = Vector3i.Clamp( floored, 0, Size - 1 );
				var max = Vector3i.Clamp( Vector3i.Ceiling( local ), 0, Size - 1 );
				var lerp = local - floored;

				if ( min == max )
				{
					return Array[Vector3i.Dot( _stride, min )].Value;
				}

				var i000 = Vector3i.Dot( _stride, new Vector3i( min.x, min.y, min.z ) );
				var i100 = Vector3i.Dot( _stride, new Vector3i( max.x, min.y, min.z ) );
				var i010 = Vector3i.Dot( _stride, new Vector3i( min.x, max.y, min.z ) );
				var i110 = Vector3i.Dot( _stride, new Vector3i( max.x, max.y, min.z ) );
				var i001 = Vector3i.Dot( _stride, new Vector3i( min.x, min.y, max.z ) );
				var i101 = Vector3i.Dot( _stride, new Vector3i( max.x, min.y, max.z ) );
				var i011 = Vector3i.Dot( _stride, new Vector3i( min.x, max.y, max.z ) );
				var i111 = Vector3i.Dot( _stride, new Vector3i( max.x, max.y, max.z ) );

				var v000 = Array[i000].Value;
				var v100 = Array[i100].Value;
				var v010 = Array[i010].Value;
				var v110 = Array[i110].Value;
				var v001 = Array[i001].Value;
				var v101 = Array[i101].Value;
				var v011 = Array[i011].Value;
				var v111 = Array[i111].Value;

				var v_00 = Lerp( v000, v100, lerp.x );
				var v_10 = Lerp( v010, v110, lerp.x );
				var v_01 = Lerp( v001, v101, lerp.x );
				var v_11 = Lerp( v011, v111, lerp.x );

				var v__0 = Lerp( v_00, v_10, lerp.y );
				var v__1 = Lerp( v_01, v_11, lerp.y );

				return Lerp( v__0, v__1, lerp.z );
			}
		}
	}
}
