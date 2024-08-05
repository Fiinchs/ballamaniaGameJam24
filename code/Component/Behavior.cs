using System;
using Sandbox;

public sealed class Behavior : Component
{
	[Property]
	[Category("Component")]
	public GameObject Camera { get; set; }

	[Property]
	[Category("Stats")]
	[Range(1f, 1000f, 10f)]
	public float BallSpeed { get; set; } = 500f;

	public Cible target { get; set; } = null;
	private Cible previousTarget = null;

	[Property]
	public SphereCollider hitbox { get; set; }

	private Vector3 currentVelocity = Vector3.Zero;
	private bool isPunched = false;
	private bool isLockedOnTarget = false;

	[Property]
	private float gravityStrength = 5f;
	[Property]
	private float turnRate = 1f;

	protected override void OnStart()
	{
		foreach (var gameObject in Scene.GetAllObjects(true))
		{
			if (gameObject.Components.TryGet<Cible>(out Cible behavior))
			{
				target = behavior;
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

		if (isPunched)
		{
			Transform.Position += currentVelocity * Time.Delta;
			isPunched = false;
		}
		else
		{
			Follow();
		}

		// Apply the current velocity to the position before collision detection and handling
		Transform.Position += currentVelocity * Time.Delta;

		terrainColider();
	}

	public void Follow()
	{
		if (target == null)
		{
			return;
		}

		Vector3 ballPos = Transform.Position;
		Vector3 targetPos = target.Transform.Position;

		if (isLockedOnTarget)
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
		Vector3 interpolatedDirection = Vector3.Lerp(currentDirection, desiredDirection, turnRate * Time.Delta);

		Vector3 move = interpolatedDirection * BallSpeed * Time.Delta;
		Vector3 newPosition = Transform.Position + move;

		Transform.Position = newPosition;
		currentVelocity = move;

		if (Vector3.DistanceBetween(Transform.Position, target.Transform.Position) < 5f)
		{
			isLockedOnTarget = true;
		}
	}

	public void punch(Vector3 direction)
	{
		Vector3 normalizedDirection = direction.Normal;
		BallSpeed += 20;
		currentVelocity = normalizedDirection * BallSpeed;

		isPunched = true;

		Cible closestPlayer = FindClosestPlayerInVD(direction.Normal);

		if (closestPlayer != null)
		{
			previousTarget = target;
			target = closestPlayer;
		}
	}

	public Cible FindClosestPlayerInVD(Vector3 direction)
	{
		Cible closestPlayer = null;
		float minAngle = float.MaxValue;

		foreach (var gameObject in Scene.GetAllObjects(true))
		{
			if (gameObject.Components.TryGet<Cible>(out Cible behavior))
			{
				if (behavior == target || behavior == previousTarget) continue;

				Vector3 toPlayer = behavior.Transform.LocalPosition - this.Transform.LocalPosition;
				Vector3 normalizedToP = toPlayer.Normal;

				float angle = Vector3.Dot(direction, normalizedToP);

				if (angle > 0 && angle < minAngle)
				{
					minAngle = angle;
					closestPlayer = behavior;
				}
			}
		}
		return closestPlayer;
	}



public void terrainColider()
{
    if (hitbox != null)
    {
        foreach (Collider hit in hitbox.Touching)
        {
            if (hit != null && hit.GameObject.Tags.Has("map"))
            {
                // Envoyer la balle vers le haut avec sa vitesse actuelle
                currentVelocity = new Vector3(currentVelocity.x, currentVelocity.y, Math.Abs(currentVelocity.Length));

                // Repositionner la balle légèrement au-dessus du point de collision pour éviter de rester coincée
                Transform.Position += Vector3.Up * (hitbox.Radius + 0.1f); // Ajustez le décalage en fonction de votre hitbox

                break; // Sortir de la boucle après la première collision détectée
            }
        }
    }
}


}
