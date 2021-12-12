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

		public TimeSince LastEdit { get; private set; }

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			if ( IsClient && Game.Voxels != null && LastEdit > 1f / 60f )
			{
				LastEdit = 0f;

				var transform = Matrix.CreateTranslation( Vector3.Lerp( Position, EyePos, 0.5f ) );

				Game.Voxels.Subtract( new SphereSdf( Vector3.Zero, 32f, 16f ), transform, 0 );
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
