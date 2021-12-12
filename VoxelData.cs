using System;

namespace Voxels
{
	public interface IVoxelData
	{
		bool Clear();
		void UpdateMesh( IVoxelMeshWriter writer );

		bool Add<T>( T sdf, BBox bounds, Matrix transform, float detailSize, byte materialIndex )
			where T : ISignedDistanceField;
		bool Subtract<T>( T sdf, BBox bounds, Matrix transform, float detailSize, byte materialIndex )
			where T : ISignedDistanceField;
	}

	public class ArrayVoxelData : IVoxelData
	{
		public int MinSubdivisions { get; }
		public int MaxSubdivisions { get; }

		private int _subdivisions;

		private Voxel[] _voxels;
		private Vector3i _size;
		private Vector3 _scale;
		private float _detailSize;

		private bool _cleared;

		public ArrayVoxelData( int minSubdivisions, int maxSubdivisions )
		{
			MinSubdivisions = minSubdivisions;
			MaxSubdivisions = maxSubdivisions;

			_cleared = true;
			_subdivisions = MinSubdivisions;
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

		private bool PrepareVoxelsForEditing( BBox bounds, float detailSize, out Vector3i outerMin, out Vector3i outerMax )
		{
			if ( _voxels == null || _detailSize > detailSize && _subdivisions < MaxSubdivisions )
			{
				var targetSize = 1f / detailSize;

				while ( _subdivisions < MaxSubdivisions && 1 << _subdivisions < targetSize )
				{
					++_subdivisions;
				}

				var resolution = 1 << _subdivisions;

				var oldVoxels = _voxels;
				var oldSize = _size;
				var oldDetailSize = _detailSize;

				_size = new Vector3i( resolution + 1, resolution + 1, resolution + 1 );
				_detailSize = 1f / resolution;
				_scale = new Vector3( _detailSize, _detailSize, _detailSize );

				_voxels = new Voxel[_size.x * _size.y * _size.z];

				if ( oldVoxels != null )
				{
					Add( new VoxelArraySdf( oldVoxels, oldSize ), new BBox( 0f, 1f ), Matrix.Identity, detailSize, 0 );
				}
			}

			outerMin = Vector3i.Max( Vector3i.Floor( bounds.Mins * (_size - 1) ), 0 );
			outerMax = Vector3i.Min( Vector3i.Ceiling( bounds.Maxs * (_size - 1) ) + 1, _size );

			return outerMin.x < outerMax.x && outerMin.y < outerMax.y && outerMin.z < outerMax.z;
		}

		public bool Add<T>( T sdf, BBox bounds, Matrix transform, float detailSize, byte materialIndex )
			where T : ISignedDistanceField
		{
			if ( !PrepareVoxelsForEditing( bounds, detailSize, out var outerMin, out var outerMax ) )
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

		public bool Subtract<T>( T sdf, BBox bounds, Matrix transform, float detailSize, byte materialIndex )
			where T : ISignedDistanceField
		{
			if ( !PrepareVoxelsForEditing( bounds, detailSize, out var outerMin, out var outerMax ) )
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
