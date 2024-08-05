using Sandbox;

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
	private Bbplayer previousTarget = null;

	[Property]
	public SphereCollider hitbox { get; set; }

	private Vector3 currentVelocity = Vector3.Zero;
	private bool isPunched = false;
	private bool isLockedOnTarget = false;

	private float gravityStrength = 5f;
	private float turnRate = 1f;

	protected override void OnStart()
	{
		foreach ( var gameObject in Scene.GetAllObjects( true ) )
		{
			if ( gameObject.Components.TryGet<Bbplayer>( out Bbplayer behavior ) )
			{
				//target = behavior;
				break;
			}
		}
	}

	protected override void OnUpdate()
	{
	}

	protected override void OnFixedUpdate()
	{
		BallSpeed += 0.1f;
		if ( isPunched )
		{
			Transform.Position += currentVelocity * Time.Delta;
			isPunched = false;
		}
		else
		{
			//Follow();
		}
		Transform.Position += currentVelocity * Time.Delta;
		terrainColider();
		
	}

	public void Follow()
	{
		if ( target == null )
		{
			return;
		}

		Vector3 ballPos = Transform.Position;
		Vector3 targetPos = target.Transform.Position;

		if ( isLockedOnTarget )
		{
			Vector3 directionToTarget2 = (targetPos - ballPos).Normal;
			currentVelocity = directionToTarget2 * BallSpeed;
			Transform.Position += currentVelocity * Time.Delta;
			return;
		}

		Vector3 gravityVector = (targetPos - ballPos).Normal * gravityStrength;
		Vector3 directionToTarget = (targetPos - ballPos).Normal;
		Vector3 currentDirection = currentVelocity.Normal;
		Vector3 desiredDirection = (directionToTarget + gravityVector).Normal;
		Vector3 interpolatedDirection = Vector3.Lerp( currentDirection, desiredDirection, turnRate * Time.Delta );

		Vector3 move = interpolatedDirection * BallSpeed * Time.Delta;
		Vector3 newPosition = Transform.Position + move;

		Transform.Position = newPosition;
		currentVelocity = move;

		if ( Vector3.DistanceBetween( Transform.Position, target.Transform.Position ) < 5f )
		{
			isLockedOnTarget = true;
		}
	}

	public void punch( Vector3 direction )
	{
		Vector3 normalizedDirection = direction.Normal;
		BallSpeed += 20;
		currentVelocity = normalizedDirection * BallSpeed;

		isPunched = true;

		Bbplayer closestPlayer = FindClosestPlayerInVD( direction.Normal );

		if ( closestPlayer != null )
		{
			previousTarget = target;
			target = closestPlayer;
		}
	}

	public Bbplayer FindClosestPlayerInVD( Vector3 direction )
	{
		Bbplayer closestPlayer = null;
		float minAngle = float.MaxValue;

		foreach ( var gameObject in Scene.GetAllObjects( true ) )
		{
			if ( gameObject.Components.TryGet<Bbplayer>( out Bbplayer behavior ) )
			{
				if ( behavior == target || behavior == previousTarget ) continue;

				Vector3 toPlayer = behavior.Transform.LocalPosition - this.Transform.LocalPosition;
				Vector3 normalizedToP = toPlayer.Normal;

				float angle = Vector3.Dot( direction, normalizedToP );

				if ( angle > 0 && angle < minAngle )
				{
					minAngle = angle;
					closestPlayer = behavior;
				}
			}
		}
		return closestPlayer;
	}

	private float lastCollisionTime = -1f;
	private float collisionCooldown = 0.1f; // 100ms cooldown between collisions

	public void terrainColider()
	{
		if ( hitbox != null && (lastCollisionTime < 0 || Time.Now - lastCollisionTime > collisionCooldown) )
		{
			foreach ( Collider hit in hitbox.Touching )
			{
				if ( hit != null && hit.GameObject.Tags.Has( "map" ) )
				{
					// Debugging: Print which collider we are hitting
					Log.Info( $"Hitting collider: {hit.GameObject.Name}" );

					// Calculate the normal of the surface hit
					Vector3 hitNormal = hit.Transform.LocalScale;

					// Debugging: Print the hit normal and current velocity
					Log.Info( $"Hit normal: {hitNormal}" );
					Log.Info( $"Current velocity before reflect: {currentVelocity}" );

					// Reflect the current velocity using the hit normal
					currentVelocity = Vector3.Reflect( currentVelocity, hitNormal );

					// Debugging: Print the new velocity after reflect
					Log.Info( $"New velocity after reflect: {currentVelocity}" );

					// Optionally, apply a damping factor to simulate energy loss on bounce
					float dampingFactor = 0.8f;
					currentVelocity *= dampingFactor;

					// Adjust the position slightly to ensure it is not stuck in the collider
					Transform.Position += hitNormal * 0.1f;

					// Debugging: Print the new position
					Log.Info( $"New position: {Transform.Position}" );

					// Set isPunched to false to prevent immediate re-application of velocity
					isPunched = false;

					// Record the time of this collision
					lastCollisionTime = Time.Now;

					// Break after handling the first collision to avoid multiple collisions in a single frame
					break;
				}
			}
		}
	}

}
