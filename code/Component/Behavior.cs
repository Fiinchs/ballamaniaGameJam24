using Sandbox;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

public sealed class Behavior : Component
{
	[Property]
	[Category( "Component" )]
	public GameObject Camera { get; set; }

	[Property]
	[Category( "Stats" )]
	[Range( 1f, 1000f, 10f )]
	public float BallSpeed { get; set; } = 500f;

	public Bbplayer target { get; set; } = null;

	[Property]
	public SphereCollider hitbox { get; set; }

	// Force of the punch

	private Vector3 currentVelocity = Vector3.Zero; // Current velocity of the ball
	private bool isPunched = false; // Punch indicator
	private float punchTimer = 0f; // Timer for punch duration
	public float punchDuration = 1f; // Duration for which the ball is repelled

	protected override void OnStart()
	{
		var traceball = Scene.Trace
		.Sphere(1000f, Transform.LocalPosition, Transform.LocalPosition )
		.Size( 1000f )
		.Run();
		


		Log.Info( traceball );

		if ( traceball.GameObject.Components.TryGet<Bbplayer>( out Bbplayer behavior ) )
		{
			target = behavior;
			Log.Info( target );
			
		}
	

		

	}

	protected override void OnUpdate()
	{
		// Debug information
		//Log.Info( $"OnUpdate - Position: {Transform.Position}, Velocity: {currentVelocity}" );
	}

	protected override void OnFixedUpdate()
	{
		BallSpeed += 0.1f;
		if ( isPunched )
		{
			// Update position based on applied force
			Transform.Position += currentVelocity * Time.Delta;

			// Gradually reduce velocity to simulate friction
			//currentVelocity = Vector3.Lerp( currentVelocity, Vector3.Zero, Time.Delta * 2f );

			// Update punch timer
			punchTimer += Time.Delta;

			// If punch duration is over, stop punch movement and resume following
			if ( punchTimer >= punchDuration )
			{
				isPunched = false;
				punchTimer = 0f;
				//currentVelocity = Vector3.Zero; // Ensure velocity is reset
			}
		}
		else
		{
			Follow();
		}
	}

	public void Follow()
	{
		Vector3 targetPos = target.Transform.Position + new Vector3( 0, 0, 50 );
		Vector3 ballPos = Transform.Position;
		Vector3 direction = (targetPos - ballPos).Normal;
		float distance = Vector3.DistanceBetween( targetPos, ballPos );
		Vector3 move = direction * BallSpeed  * Time.Delta;

		Transform.Position += move;

		// Debug information
		//Log.Info( $"Follow - TargetPos: {targetPos}, BallPos: {ballPos}, Direction: {direction}, Move: {move}" );
	}

	public void punch( Vector3 direction )
	{
		// Normalize the direction
		Vector3 normalizedDirection = direction.Normal;

		// Calculate velocity
		currentVelocity = normalizedDirection * BallSpeed;

		// Apply the punch force in the specified direction
		isPunched = true;
		punchTimer = 0f; // Reset the punch timer

		// Debug information
		Log.Info( $"Punch - Direction: {direction}, Normalized Direction: {normalizedDirection}, Velocity: {currentVelocity}" );
	}


}
