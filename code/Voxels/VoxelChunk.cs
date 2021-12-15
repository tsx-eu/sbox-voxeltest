using Sandbox;
using System.Linq;

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

		public void UpdateMesh( bool render, bool collision )
		{
			var writer = MarchingCubesMeshWriter.Rent();

			writer.Scale = Size;

			try
			{
				Data.UpdateMesh( writer, 0, render, collision );

				if ( writer.Vertices.Count == 0 && writer.CollisionVertices.Count == 0 )
				{
					if ( render )
					{
						EnableDrawing = false;
						EnableShadowCasting = false;
					}

					if ( collision )
					{
						if ( PhysicsBody != null && PhysicsBody.ShapeCount > 0 )
						{
							PhysicsBody.RemoveShape( PhysicsBody.Shapes.First(), false );
						}
					}

					SetModel( "" );

					return;
				}

				EnsureMeshCreated();

				if ( render )
				{
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

				if ( _model == null )
				{
					var modelBuilder = new ModelBuilder();

					modelBuilder.AddMesh( _mesh );
					modelBuilder.AddCollisionMesh( writer.CollisionVertices.ToArray(), writer.CollisionIndices.ToArray() );

					_model = modelBuilder.Create();

					SetModel( _model );
					SetupPhysicsFromModel( PhysicsMotionType.Static );
				}
				else
				{
					if ( render )
					{
						EnableDrawing = true;
						EnableShadowCasting = true;
					}

					SetModel( _model );

					if ( collision && PhysicsBody != null && PhysicsBody.IsValid() )
					{
						if ( PhysicsBody.ShapeCount > 0 )
						{
							PhysicsBody.RemoveShape( PhysicsBody.Shapes.First(), false );
						}

						PhysicsBody.AddMeshShape( writer.CollisionVertices.ToArray(), writer.CollisionIndices.ToArray() );
					}
				}

			}
			finally
			{
				writer.Return();
			}
		}

		protected override void OnDestroy()
		{

		}
	}
}
