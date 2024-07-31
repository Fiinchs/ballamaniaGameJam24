using Sandbox;
using Sandbox.Citizen;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

public sealed class Bbplayer : Component , Component.ITriggerListener
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
	public CapsuleCollider Colliderhitbox { get; set; }
	

	[Property]
	public Vector3 CranePosition { get; set; }

	[Property]
	public Vector3 startHit { get; set; }

	[Property]
	public Vector3 endHit { get; set; }

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
	[Range( 0f, 5f, 0.1f )]
	public float PunchStrength { get; set; } = 1f;


	[Property]
	[Category( "Stats" )]
	[Range( 0f, 5f, 0.1f )]
	public float PunchColdown { get; set; } = 0.5f;

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 200f, 5f )]
	public float PunchRange { get; set; } = 50f;





	public Angles EyeAngles { get; set; }

	Transform _initialCameraTransform;


	TimeSince _lastPunch;

	protected override void DrawGizmos()
	{
	
		

		if ( !Gizmo.IsSelected ) return;
		var draw = Gizmo.Draw;
		draw.LineSphere( EyePosition, 30f );

		draw.LineSphere( CounterHitbox, 10f );
		//Gizmo.Draw.LineCylinder( EyePosition, EyePosition + Transform.Rotation.Forward * PunchRange, 5f, 5f, 10 );

		draw.LineSphere( startHit, 10f );
		draw.LineSphere( endHit, 10f );
		draw.LineCylinder( EyePosition, EyePosition + Transform.Rotation.Forward * PunchRange, 5f, 5f, 10 );

		draw.LineCylinder( CranePosition, CounterHitbox, 30f, 30f, 50 );

		
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

			if ( _lastPunch >= 0.1f )
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

	public void doesHit()
	{
		var tr = Scene.Trace
			.Capsule( new Capsule( Transform.Position, CranePosition, 40f) )
			.Run();

		if(tr.Hit)
		{
			
			if ( tr.GameObject.Components.TryGet<Behavior>( out var behavior ) )
				Log.Info( "mort" );
		}
	}

	public void dash()
	{

			Transform.LocalPosition += ((EyeAngles.Forward - new Vector3( 0, 0, 0.3f )) * 800f);
			



	}

	public void jumpMethod()
	{

		if ( CharacterController.IsOnGround )
		{
			AvaibleDoubleJump = true;
			CharacterController.Acceleration = 10f;
			CharacterController.ApplyFriction( 5f, 20f );

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


		var punchTraces = Scene.Trace
		.FromTo( EyeWorldPostion, EyeWorldPostion + EyeAngles.Forward * PunchRange )
		.Size( 30f )
		.WithoutTags( "player" )
		.IgnoreGameObjectHierarchy( GameObject )
		.RunAll();

		if ( punchTraces == null || punchTraces.Count() == 0 ) return;

		foreach ( var punchTrace in punchTraces )
		{
			if ( punchTrace.GameObject.Components.TryGet<Behavior>( out var behavior ) )
			{
				behavior.punch( EyeAngles.Forward - new Vector3( 0, 0, 0.3f ) );
			}

			if ( punchTrace.GameObject.Components.TryGet<PropppBehavior>( out var propbehavior ) )
			{
				propbehavior.punch( EyeAngles.Forward - new Vector3( 0, 0, 0.3f ) );
			}
		}



		_lastPunch = 0;
		
	}

	public void ragdoll()
	{
		Destroy();
	}

	protected override void OnAwake()
	{
		base.OnAwake();
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}


}
