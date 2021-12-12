using Sandbox;

namespace Voxels
{
	public partial class VoxelVolume : Entity
	{
		public Vector3 LocalSize { get; private set; }
		public float ChunkSize { get; private set; }

		private float _chunkScale;
		private Vector3i _chunkCount;
		private Vector3 _chunkOffset;

		private VoxelChunk[] _chunks;

		public VoxelVolume()
		{

		}

		public VoxelVolume( Vector3 size, float chunkSize )
		{
			LocalSize = size;
			ChunkSize = chunkSize;

			CreateChunks();
		}

		protected override void OnDestroy()
		{
			foreach ( var chunk in _chunks )
			{
				chunk.Delete();
			}

			_chunks = null;
		}

		private void CreateChunks()
		{
			if ( _chunks != null )
			{
				for ( var i = 0; i < _chunks.Length; ++i )
				{
					_chunks[i]?.Delete();
				}
			}

			_chunks = null;

			_chunkScale = 1f / ChunkSize;
			_chunkCount = Vector3i.Ceiling( LocalSize * _chunkScale );
			_chunkOffset = LocalSize * -0.5f;

			_chunks = new VoxelChunk[_chunkCount.x * _chunkCount.y * _chunkCount.z];
		}

		public void Clear()
		{
			foreach ( var chunk in _chunks )
			{
				if ( chunk == null ) continue;

				if ( chunk.Data.Clear() )
				{
					chunk.InvalidateMesh();
				}
			}
		}

		private void GetChunkBounds( Matrix transform, BBox bounds,
			out Matrix invChunkTransform, out BBox chunkBounds,
			out Vector3i minChunkIndex, out Vector3i maxChunkIndex )
		{
			var worldToLocal = Matrix.CreateRotation( Rotation.Inverse )
				* Matrix.CreateScale( 1f / Scale )
				* Matrix.CreateTranslation( -Position );

			var localTransform = worldToLocal * transform;

			invChunkTransform = Matrix.CreateScale( ChunkSize );
			invChunkTransform *= Matrix.CreateTranslation( _chunkOffset );
			invChunkTransform *= localTransform.Inverted;

			chunkBounds = (localTransform.Transform( bounds ) + -_chunkOffset) * _chunkScale;

			minChunkIndex = Vector3i.Floor( chunkBounds.Mins ) - 1;
			maxChunkIndex = Vector3i.Ceiling( chunkBounds.Maxs ) + 2;
		}

		private VoxelChunk GetOrCreateChunk( int index, Vector3i index3 )
		{
			var chunk = _chunks[index];

			if ( chunk == null )
			{
				_chunks[index] = chunk = new VoxelChunk( new ArrayVoxelData( 4, 4 ), ChunkSize );

				chunk.Name = $"Chunk {index3.x} {index3.y} {index3.z}";

				chunk.SetParent( this );
				chunk.LocalPosition = _chunkOffset + (Vector3)index3 * ChunkSize;
			}

			return chunk;
		}

		public void Add<T>( T sdf, Matrix transform, float detailSize, byte materialIndex )
			where T : ISignedDistanceField
		{
			GetChunkBounds( transform, sdf.Bounds,
				out var invChunkTransform, out var chunkBounds,
				out var minChunkIndex, out var maxChunkIndex );

			var chunkDetailSize = _chunkScale * detailSize;

			foreach ( var (chunkIndex3, chunkIndex) in _chunkCount.EnumerateArray3D( minChunkIndex, maxChunkIndex ) )
			{
				var chunk = GetOrCreateChunk( chunkIndex, chunkIndex3 );

				if ( chunk.Data.Add( sdf, chunkBounds + -chunkIndex3,
					Matrix.CreateTranslation( chunkIndex3 ) * invChunkTransform,
					chunkDetailSize, materialIndex ) )
				{
					chunk.InvalidateMesh();
				}
			}
		}

		public void Subtract<T>( T sdf, Matrix transform, float detailSize, byte materialIndex )
			where T : ISignedDistanceField
		{
			GetChunkBounds( transform, sdf.Bounds,
				out var invChunkTransform, out var chunkBounds,
				out var minChunkIndex, out var maxChunkIndex );

			var chunkDetailSize = _chunkScale * detailSize;

			foreach ( var (chunkIndex3, chunkIndex) in _chunkCount.EnumerateArray3D( minChunkIndex, maxChunkIndex ) )
			{
				var chunk = GetOrCreateChunk( chunkIndex, chunkIndex3 );

				if ( chunk.Data.Subtract( sdf, chunkBounds + -chunkIndex3,
					Matrix.CreateTranslation( chunkIndex3 ) * invChunkTransform,
					chunkDetailSize, materialIndex ) )
				{
					chunk.InvalidateMesh();
				}
			}
		}
	}
}
