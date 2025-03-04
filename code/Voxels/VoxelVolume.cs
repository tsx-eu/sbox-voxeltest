﻿using Sandbox;
using System.Collections.Generic;

namespace Voxels
{
	public partial class VoxelVolume : Entity
	{
		public Vector3 LocalSize { get; private set; }
		public float ChunkSize { get; private set; }
		public int ChunkSubdivisions { get; private set; }
		public NormalStyle NormalStyle { get; private set;}

		private float _chunkScale;
		private Vector3i _chunkCount;
		protected Vector3 _chunkOffset;

		private int _margin;

		protected readonly Dictionary<Vector3i, VoxelChunk> _chunks = new Dictionary<Vector3i, VoxelChunk>();

		public VoxelVolume()
		{

		}

		public VoxelVolume( Vector3 size, float chunkSize, int chunkSubdivisions = 4, NormalStyle normalStyle = NormalStyle.Smooth )
		{
			LocalSize = size;
			ChunkSize = chunkSize;
			ChunkSubdivisions = chunkSubdivisions;
			NormalStyle = normalStyle;

			_chunkScale = 1f / ChunkSize;
			_chunkCount = Vector3i.Ceiling( LocalSize * _chunkScale );
			_chunkOffset = LocalSize * -0.5f;

			_margin = normalStyle == NormalStyle.Flat ? 1 : 2;
		}

		protected override void OnDestroy()
		{
			Clear();
		}

		public void Clear()
		{
			foreach ( var pair in _chunks )
			{
				pair.Value.Delete();
			}

			_chunks.Clear();
		}

		private void GetChunkBounds( Matrix transform, BBox bounds,
			out Matrix invChunkTransform, out BBox chunkBounds,
			out Vector3i minChunkIndex, out Vector3i maxChunkIndex )
		{
			var worldToLocal = Matrix.CreateScale( 1f / Scale )
				* Matrix.CreateRotation( Rotation.Inverse )
				* Matrix.CreateTranslation( -Position );

			var localTransform = transform * worldToLocal;

			invChunkTransform = Matrix.CreateScale( ChunkSize )
				* Matrix.CreateTranslation( _chunkOffset )
				* localTransform.Inverted;

			chunkBounds = localTransform.Transform( bounds ) + -_chunkOffset;
			chunkBounds = new BBox( chunkBounds.Mins - _margin - 1, chunkBounds.Maxs + _margin + 1 ) * _chunkScale;

			minChunkIndex = Vector3i.Floor( chunkBounds.Mins );
			maxChunkIndex = Vector3i.Ceiling( chunkBounds.Maxs ) + 1;
		}

		protected virtual VoxelChunk GetOrCreateChunk( Vector3i index3 )
		{
			if ( _chunks.TryGetValue( index3, out var chunk ) ) return chunk;

			_chunks.Add( index3, chunk = new VoxelChunk( new ArrayVoxelData( ChunkSubdivisions, NormalStyle ), ChunkSize ) );

			chunk.Name = $"Chunk {index3.x} {index3.y} {index3.z}";

			chunk.SetParent( this );
			chunk.LocalPosition = _chunkOffset + (Vector3)index3 * ChunkSize;

			return chunk;
		}

		public void Add<T>( T sdf, Matrix transform, byte materialIndex )
			where T : ISignedDistanceField
		{
			GetChunkBounds( transform, sdf.Bounds,
				out var invChunkTransform, out var chunkBounds,
				out var minChunkIndex, out var maxChunkIndex );

			foreach ( var (chunkIndex3, _) in _chunkCount.EnumerateArray3D( minChunkIndex, maxChunkIndex ) )
			{
				var chunk = GetOrCreateChunk( chunkIndex3 );

				if ( chunk.Data.Add( sdf, chunkBounds + -chunkIndex3,
					Matrix.CreateTranslation( chunkIndex3 ) * invChunkTransform,
					materialIndex ) )
				{
					chunk.InvalidateMesh();
				}
			}
		}

		public void Subtract<T>( T sdf, Matrix transform, byte materialIndex )
			where T : ISignedDistanceField
		{
			GetChunkBounds( transform, sdf.Bounds,
				out var invChunkTransform, out var chunkBounds,
				out var minChunkIndex, out var maxChunkIndex );

			foreach ( var (chunkIndex3, _) in _chunkCount.EnumerateArray3D( minChunkIndex, maxChunkIndex ) )
			{
				if ( !_chunks.TryGetValue( chunkIndex3, out var chunk ) ) continue;

				if ( chunk.Data.Subtract( sdf, chunkBounds + -chunkIndex3,
					Matrix.CreateTranslation( chunkIndex3 ) * invChunkTransform,
					materialIndex ) )
				{
					chunk.InvalidateMesh();
				}
			}
		}
	}
}
