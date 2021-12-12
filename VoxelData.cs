using System;

namespace Voxels
{
	public interface IVoxelData
	{
		bool Clear();
		void UpdateMesh( IVoxelMeshWriter writer );

		bool Add<T>( T sdf, BBox bounds, Matrix transform, byte materialIndex )
			where T : ISignedDistanceField;
		bool Subtract<T>( T sdf, BBox bounds, Matrix transform, byte materialIndex )
			where T : ISignedDistanceField;
	}

	public class ArrayVoxelData : IVoxelData
	{
		public const int MaxSubdivisions = 5;

		public int Subdivisions { get; }

		private Voxel[] _voxels;
		private Vector3i _size;
		private Vector3 _scale;

		private bool _cleared;
		private int _margin;

		public ArrayVoxelData( int subdivisions )
		{
			if ( subdivisions < 0 || subdivisions > MaxSubdivisions )
			{
				throw new ArgumentOutOfRangeException( nameof(subdivisions),
					$"Expected {nameof(subdivisions)} to be between 0 and {MaxSubdivisions}." );
			}

			Subdivisions = subdivisions;

			_cleared = true;
			_margin = 0;
		}

		public bool Clear()
		{
			if ( _cleared || _voxels == null ) return false;

			Array.Clear( _voxels, 0, _voxels.Length );

			_cleared = true;

			return true;
		}

		public void UpdateMesh( IVoxelMeshWriter writer )
		{
			if ( _voxels == null || _cleared ) return;

			writer.Write( _voxels, _size, 0, _size );
		}

		private bool PrepareVoxelsForEditing( BBox bounds, out Vector3i outerMin, out Vector3i outerMax )
		{
			if ( _voxels == null )
			{
				var resolution = 1 << Subdivisions;

				_size = resolution + _margin * 2 + 1;
				_scale = 1f / resolution;

				_voxels = new Voxel[_size.x * _size.y * _size.z];
			}

			outerMin = Vector3i.Max( Vector3i.Floor( bounds.Mins * (_size - 1) ), 0 );
			outerMax = Vector3i.Min( Vector3i.Ceiling( bounds.Maxs * (_size - 1) ) + 1, _size );

			return outerMin.x < outerMax.x && outerMin.y < outerMax.y && outerMin.z < outerMax.z;
		}

		public bool Add<T>( T sdf, BBox bounds, Matrix transform, byte materialIndex )
			where T : ISignedDistanceField
		{
			if ( !PrepareVoxelsForEditing( bounds, out var outerMin, out var outerMax ) )
			{
				return false;
			}

			var changed = false;

			foreach ( var (index3, index) in _size.EnumerateArray3D( outerMin, outerMax ) )
			{
				var pos = transform.Transform( index3 * _scale );
				var next = new Voxel( sdf[pos], materialIndex );
				var prev = _voxels[index];

				_voxels[index] = prev + next;

				changed |= prev.RawValue < 255 && next.RawValue > 0;
			}

			if ( changed ) _cleared = false;

			return changed;
		}

		public bool Subtract<T>( T sdf, BBox bounds, Matrix transform, byte materialIndex )
			where T : ISignedDistanceField
		{
			if ( !PrepareVoxelsForEditing( bounds, out var outerMin, out var outerMax ) )
			{
				return false;
			}

			var changed = false;

			foreach ( var (index3, index) in _size.EnumerateArray3D( outerMin, outerMax ) )
			{
				var pos = transform.Transform( index3 * _scale );
				var next = new Voxel( sdf[pos], materialIndex );
				var prev = _voxels[index];

				_voxels[index] = prev - next;

				changed |= prev.RawValue > 0 && next.RawValue > 0;
			}

			if ( changed ) _cleared = false;

			return changed;
		}
	}
}
