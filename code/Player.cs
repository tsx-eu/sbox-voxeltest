using Sandbox;
using Voxels;

namespace VoxelTest
{
	partial class Player : Sandbox.Player
	{
		public override void Respawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );

			Controller = new WalkController();
			Animator = new StandardPlayerAnimator();
			Camera = new FirstPersonCamera();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			base.Respawn();
		}

		public override void OnKilled()
		{
			base.OnKilled();

			Controller = null;
			Animator = null;
			Camera = null;

			EnableAllCollisions = false;
			EnableDrawing = false;
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			if ( IsClient && Game.Voxels != null )
			{
				Game.Voxels.Subtract( new SphereSdf( (Position + EyePos) * 0.5f, 32f, 16f ),
					Matrix.CreateScale( new Vector3( 1f, 1f, 2f ) ), 8f, 0 );
			}

			if ( !IsServer )
				return;

			if ( Input.Pressed( InputButton.Flashlight ) )
			{
				var r = Input.Rotation;
				var ent = new Prop
				{
					Position = EyePos + r.Forward * 50,
					Rotation = r
				};

				ent.SetModel( "models/citizen_props/crate01.vmdl" );
				ent.Velocity = r.Forward * 1000;
			}
		}
	}
}
