using Sandbox;
using System.Diagnostics;
using Voxels;

namespace VoxelTest
{
	public partial class Game : Sandbox.Game
	{
		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			var player = new Player();
			client.Pawn = player;

			player.Respawn();
		}

		public override void PostLevelLoaded()
		{
			if ( !IsServer )
				return;

		}

		public static VoxelVolume Voxels { get; private set; }

		[ClientCmd( "clear_voxels" )]
		public static void ClearVoxels()
		{
			Voxels?.Delete();
			Voxels = null;
		}

		public static VoxelVolume GetOrCreateVoxelVolume()
		{
			if ( Voxels != null ) return Voxels;

			Voxels = new VoxelVolume( new Vector3( 32_768f, 32_768f, 32_768f ), 256f );

			return Voxels;
		}

		[ClientCmd( "spawn_spheres" )]
		public static void SpawnSphere( int count = 1 )
		{
			var voxels = GetOrCreateVoxelVolume();

			var timer = new Stopwatch();
			timer.Start();

			var bounds = new BBox( new Vector3( -512f, -512f, 0f ), new Vector3( 512f, 512f, 512f ) ) + Local.Client.Pawn.Position;
			var smoothing = 16f;

			for ( var i = 0; i < count; ++i )
			{
				var radius = Rand.Float( 32f, 64f );
				var centerRange = new BBox( bounds.Mins + radius + smoothing, bounds.Maxs - radius - smoothing );

				voxels.Add( new SphereSdf( centerRange.RandomPointInside, radius, smoothing ), Matrix.Identity, 0 );
			}

			Log.Info( $"Spawned {count} spheres in {timer.Elapsed.TotalMilliseconds:F2}ms" );
		}

		[ClientCmd( "spawn_boxes" )]
		public static void SpawnBoxes( int count = 1 )
		{
			var voxels = GetOrCreateVoxelVolume();

			var timer = new Stopwatch();
			timer.Start();

			var sizeRange = new BBox( 32, 128f );

			var bounds = new BBox( new Vector3( -512f, -512f, 0f ), new Vector3( 512f, 512f, 512f ) ) + Local.Client.Pawn.Position;
			var smoothing = 16f;

			for ( var i = 0; i < count; ++i )
			{
				var size = sizeRange.RandomPointInside;
				var centerRange = new BBox( bounds.Mins + size + smoothing, bounds.Maxs - size - smoothing );
				var center = centerRange.RandomPointInside;

				voxels.Add( new BBoxSdf( center - size * 0.5f, center + size * 0.5f, smoothing ), Matrix.Identity, 0 );
			}

			Log.Info( $"Spawned {count} boxes in {timer.Elapsed.TotalMilliseconds:F2}ms" );
		}
	}
}
