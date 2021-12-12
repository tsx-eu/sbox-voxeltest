using Sandbox;

namespace Voxels
{
	public partial class VoxelChunk : ModelEntity
	{
		public IVoxelData Data { get; private set; }

		public float Size { get; private set; }

		private Mesh _mesh;
		private Model _model;

		private bool _meshInvalid;

		public VoxelChunk()
		{

		}

		public VoxelChunk( IVoxelData data, float size )
		{
			Data = data;
			Size = size;
		}

		public void InvalidateMesh()
		{
			_meshInvalid = true;
		}

		[Event.Tick.Client]
		public void Tick()
		{
			if ( _meshInvalid )
			{
				_meshInvalid = false;

				UpdateMesh();
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

		public void UpdateMesh()
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
