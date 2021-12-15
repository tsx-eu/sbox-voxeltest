using Sandbox;

namespace Voxels
{
	public partial class VoxelChunk : ModelEntity
	{
		[Net] public ArrayVoxelData Data { get; private set; }
		[Net] public float Size { get; private set; }

		private Mesh _mesh;
		private Model _model;

		private bool _meshInvalid;
		private int _lastNetReadCount;

		public VoxelChunk()
		{

		}

		public VoxelChunk( ArrayVoxelData data, float size )
		{
			Data = data;
			Size = size;

			CollisionBounds = new BBox( 0f, size );
		}

		public void InvalidateMesh()
		{
			_meshInvalid = true;
		}

		[Event.Tick.Client]
		public void ClientTick()
		{
			if ( _lastNetReadCount != Data.NetReadCount )
			{
				_lastNetReadCount = Data.NetReadCount;

				InvalidateMesh();
			}

			if ( _meshInvalid )
			{
				_meshInvalid = false;

				UpdateMesh( true, true );
			}
		}

		[Event.Tick.Server]
		public void ServerTick()
		{
			if ( _meshInvalid )
			{
				_meshInvalid = false;

				UpdateMesh( false, true );
				Data.WriteNetworkData();
			}
		}

		private void EnsureMeshCreated()
		{
			if ( _mesh != null ) return;

			var material = Material.Load( "materials/voxeltest.vmat" );

			_mesh = new Mesh( material )
			{
				Bounds = new BBox( 0f, Size )
			};
		}

		public void UpdateMesh( bool model, bool collision )
		{
			var writer = MarchingCubesMeshWriter.Rent();

			writer.Scale = Size;

			try
			{
				Data.UpdateMesh( writer );

				if ( writer.Vertices.Count == 0 )
				{
					EnableDrawing = false;
					EnableShadowCasting = false;

					SetModel( "" );
					return;
				}

				EnsureMeshCreated();

				if ( _mesh.HasVertexBuffer )
				{
					_mesh.SetVertexBufferSize( writer.Vertices.Count );
					_mesh.SetVertexBufferData( writer.Vertices );
				}
				else
				{
					_mesh.CreateVertexBuffer( writer.Vertices.Count, VoxelVertex.Layout, writer.Vertices );
				}

				_mesh.SetVertexRange( 0, writer.Vertices.Count );
			}
			finally
			{
				writer.Return();
			}

			if ( _model == null )
			{
				var modelBuilder = new ModelBuilder();

				modelBuilder.AddMesh( _mesh );

				_model = modelBuilder.Create();
			}

			SetModel( _model );

			EnableDrawing = true;
			EnableShadowCasting = true;
		}

		protected override void OnDestroy()
		{

		}
	}
}
