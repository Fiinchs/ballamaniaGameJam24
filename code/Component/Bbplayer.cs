using Sandbox;
using Sandbox.Citizen;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;


public sealed class Bbplayer : Component
{
	
	[Property]
	[Category( "Component" )]
	public GameObject Camera { get; set; }

	[Property]
	[Category( "Component" )]
	public CharacterController CharacterController { get; set; }

	[Property]
	[Category( "Component" )]
	public CitizenAnimationHelper AnimationHelper { get; set; }

	[Property]
	[Category( "Component" )]
	public Model Citizenmodel { get; set; }

	[Property]
	public Vector3 EyePosition { get; set; }

	[Property]
	public Vector3 CounterHitbox { get; set; }

	[Property]
	[Category( "Component" )]
	public ModelPhysics Ragodll { get; set; }

	[Property]
	[Category( "Component" )]
	public GameObject Batte { get; set; }

	[Property]
	[Category( "Hitbox" )]
	public CapsuleCollider Hitboxe { get; set; }


	[Property]
	[Category( "Hitbox" )]
	public SphereCollider PunchZone { get; set; }

	[Property]
	public Vector3 CranePosition { get; set; }


	public Vector3 EyeWorldPostion => Transform.Local.PointToWorld( EyePosition );
	/// <summary>
	/// MOVEMENT
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 400f )]
	public float Walkspeed { get; set; } = 200f;

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 800f )]
	public float Runspeed { get; set; } = 3500f;

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 800f )]
	public float JumpStrength { get; set; } = 400f;

	public bool AvaibleDoubleJump = false;



	/// <summary>
	/// PUNCH SYSTEM
	/// </summary>

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 1000f, 100f )]
	public float PunchStrength { get; set; } = 1f;


	[Property]
	[Category( "Stats" )]
	[Range( 0f, 5f, 0.1f )]
	public float PunchColdown { get; set; } = 0.5f;

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 200f, 5f )]
	public float PunchRange { get; set; } = 50f;

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 1000f, 5f )]
	public float DashRange { get; set; } = 500f;

	private bool isRagdolled = false; // Track the current state


	public Angles EyeAngles { get; set; }

	Transform _initialCameraTransform;


	TimeSince _lastPunch;

		protected override void DrawGizmos()
	{



		if ( !Gizmo.IsSelected ) return;
		var draw = Gizmo.Draw;
		//draw.LineSphere( EyePosition, 30f );

		//draw.LineSphere( CounterHitbox, 10f );
		//Gizmo.Draw.LineCylinder( EyePosition, EyePosition + Transform.Rotation.Forward * PunchRange, 5f, 5f, 10 );

		//draw.LineSphere( startHit, 10f );
		//draw.LineSphere( endHit, 10f );
		//draw.LineCylinder( EyePosition, EyePosition + Transform.Rotation.Forward * PunchRange, 5f, 5f, 10 );

		//draw.LineCylinder( CranePosition, CounterHitbox, 30f, 30f, 50 );


	}

	protected override void OnStart()
	{
		if ( Camera != null )
		{


			_initialCameraTransform = Camera.Transform.Local;

		}


		if ( Components.TryGet<SkinnedModelRenderer>( out var skinnedModelRenderer ) )
		{
			//a changer pour le multi ^^
			var clothing = ClothingContainer.CreateFromLocalUser();
			clothing.Apply( skinnedModelRenderer );
		}
	}





	protected override void OnUpdate()
	{
		EyeAngles += Input.AnalogLook;
		EyeAngles = EyeAngles.WithPitch( MathX.Clamp( EyeAngles.pitch, -100f, 50f ) );
		Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );



		if ( Camera != null )
		{
			var cameraTransform = _initialCameraTransform.RotateAround( EyePosition, EyeAngles.WithYaw( 0f ) );
			var cameraPosition = Transform.Local.PointToWorld( cameraTransform.Position );
			var cameraTrace = Scene.Trace.Ray( EyeWorldPostion, cameraPosition )
				.Size( 5f )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags( "player" )
				.Run();

			Camera.Transform.Position = cameraTrace.EndPosition;
			Camera.Transform.LocalRotation = cameraTransform.Rotation;
		}

		if ( Input.Pressed( "ragdoll" ) )
		{
			// Toggle the ragdoll state
			isRagdolled = !isRagdolled;
			ragdoll( isRagdolled );
		}

	}


	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if ( CharacterController == null ) return;

		if ( CharacterController.IsOnGround )
		{

			var wishSpeed = Input.Down( "Run" ) ? Runspeed : Walkspeed;
			var wishvelocity = Input.AnalogMove.Normal * wishSpeed * Transform.Rotation;

			CharacterController.Accelerate( wishvelocity );
		}



		jumpMethod();

		CharacterController.Move();

		if ( AnimationHelper != null )
		{
			AnimationHelper.IsGrounded = CharacterController.IsOnGround;
			AnimationHelper.WithVelocity( CharacterController.Velocity );

			if ( _lastPunch >= 5f )
				AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;

		}

		if ( Input.Pressed( "Punch" ) && _lastPunch >= PunchColdown )
		{
			Punch();
		}

		if ( Input.Pressed( "dash" ) )
		{
			dash();

		}





		doesHit();
	}

	public void dead()
	{
		Log.Info( "MORT" );
		Ragodll.Enabled = true;
		Batte.Components.Get<CapsuleCollider>().Enabled = false;
	}

	public Boolean doesHit()
	{
		if (Hitboxe != null)
   		{
        foreach (Collider hit in Hitboxe.Touching)
        {
			if ( hit.GameObject.Components.TryGet<Behavior>( out var behavior ) )
			{
				Log.Info( "Hitted" );               //dead();
				return true;
			}
		}
		}
		return false;
	}

public void dash()
{
    var start = EyeWorldPostion;
    var direction = EyeAngles.Forward;
    var end = start + (direction * DashRange);

    var dashTrace = Scene.Trace
        .FromTo(EyeWorldPostion, EyeWorldPostion + EyeAngles.Forward * DashRange)
        .Size(100f)
        .IgnoreGameObjectHierarchy(GameObject)
        .Run();

    if (dashTrace.Hit)
    {
        // Téléporter à la position de l'objet frappé, en conservant la même hauteur
        Transform.LocalPosition = new Vector3(dashTrace.HitPosition.x, dashTrace.HitPosition.y, dashTrace.HitPosition.z);
        Log.Info(dashTrace.HitPosition);
    }
    else
    {
        // Téléporter à la distance maximale DashRange dans la direction du regard, en conservant la même hauteur
        Transform.LocalPosition = new Vector3(end.x, end.y, end.z);
    }
}


	public void jumpMethod()
	{

		if ( CharacterController.IsOnGround )
		{
			AvaibleDoubleJump = true;
			CharacterController.Acceleration = 20f;
			CharacterController.ApplyFriction( 10f, 20f);

			if ( Input.Down( "Jump" ) )
			{
				CharacterController.Punch( Vector3.Up * JumpStrength );
				if ( AnimationHelper != null )
					AnimationHelper.TriggerJump();

			}

		}


		else
		{
			CharacterController.Acceleration = 3f;
			CharacterController.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;

			if ( Input.Pressed( "Jump" ) )
			{
				if ( AvaibleDoubleJump )
				{
					AvaibleDoubleJump = false;
					CharacterController.Velocity = CharacterController.Velocity.WithZ( JumpStrength );
					if ( AnimationHelper != null )
						//triger double jump when implemented
						AnimationHelper.TriggerJump();
				}
			}
		}



	}

	public void Punch()
	{
		if ( AnimationHelper != null )
		{

			AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
			AnimationHelper.Target.Set( "b_attack", true );


		}
		if ( PunchZone == null ) return;

		foreach (Collider hit in PunchZone.Touching)
        {
			if ( hit.GameObject.Components.TryGet<Behavior>( out var behavior ) )
			{
				Log.Info( "punched" );
				behavior.punch( EyeAngles.Forward );
			}

			if ( hit.GameObject.Components.TryGet<PropppBehavior>( out var propbehavior ) )
			{
				propbehavior.punch( EyeAngles.Forward , PunchStrength);
			}
		}



		_lastPunch = 0;

	}

	public void ragdoll( bool enable )
	{
		if ( enable )
		{
			Log.Info( "Ragdolled" );
			Ragodll.Enabled = true;
			Batte.Components.Get<CapsuleCollider>().Enabled = false;
		}
		else
		{
			Log.Info( "Retour normal" );
			Ragodll.Enabled = false;
			Batte.Components.Get<CapsuleCollider>().Enabled = true;
		}
	}

	protected override void OnAwake()
	{
		base.OnAwake();
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
	}

}
