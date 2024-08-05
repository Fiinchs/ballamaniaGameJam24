using System;
using System.Runtime;
using Microsoft.VisualBasic;
using Sandbox;
using System.Linq;

public sealed class Behavior : Component
{
    [Property]
    [Category("Component")]
    public GameObject Camera { get; set; }

    [Property]
    [Category("Stats")]
    [Range(1f, 1000f, 10f)]
    public float BallSpeed { get; set; } = 500f;

    public GameObject target { get; set; } = null;
    private GameObject previousTarget = null;

    [Property]
    public SphereCollider hitbox { get; set; }

    private Vector3 currentVelocity = Vector3.Zero;
    private bool isPunched = false;
    private bool isLockedOnTarget = false;

    [Property]
    private float gravityStrength = 5f;
    [Property]
    private float turnRate = 1f;

    private List<GameObject> entryTarget = new List<GameObject>();

    protected override void OnStart()
    {
        Log.Info("OnStart called.");
        foreach (var gameObject in Scene.GetAllObjects(true))
        {
            if (gameObject.Components.TryGet<Cible>(out Cible behavior))
            {
                Log.Info($"Found cible: {behavior}");
                entryTarget.Add(behavior.GameObject);

                if (target == null)
                {
                    target = behavior.GameObject;
                    Log.Info($"Initial target set to: {target}");
                }
            }
        }
        Log.Info($"Total targets found: {entryTarget.Count}");
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnFixedUpdate()
    {
        BallSpeed += 0.1f;
        //Log.Info($"BallSpeed increased to: {BallSpeed}");
        //Log.Info(entryTarget.ToList().ToString());
        if (isPunched)
        {
            Transform.Position += currentVelocity * Time.Delta;
            isPunched = false;
            //Log.Info("Ball was punched. Updating position and resetting isPunched.");
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
            Log.Info("No target to follow.");
            return;
        }

        Vector3 ballPos = Transform.Position;
        Vector3 targetPos = target.Transform.Position;

        if (isLockedOnTarget)
        {
            Vector3 directionToTarget2 = (targetPos - ballPos).Normal;
            currentVelocity = directionToTarget2 * BallSpeed;
            Transform.Position += currentVelocity * Time.Delta;
            Log.Info("Ball is locked on target. Moving directly towards target.");
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

        if (Vector3.DistanceBetween(Transform.Position, target.Transform.Position) < 10f)
        {
            isLockedOnTarget = true;
            Log.Info("Target locked.");
        }

        //   Log.Info($"Following target. New position: {Transform.Position}, Current velocity: {currentVelocity}");
    }

    public void punch(Vector3 direction)
    {
        Vector3 normalizedDirection = direction.Normal;
        BallSpeed += 5;
        currentVelocity = normalizedDirection * BallSpeed;

        isPunched = true;
        Log.Info($"Ball punched. Direction: {direction}, BallSpeed: {BallSpeed}, Current velocity: {currentVelocity}");

        GameObject closestPlayer = FindClosestPlayerInVD(direction.Normal);

        if (closestPlayer != null && closestPlayer != target && closestPlayer != previousTarget)
        {
            previousTarget = target;
            target = closestPlayer;
            Log.Info($"New target acquired: {target}");
        }
        else
        {
            // Toujours choisir une nouvelle cible parmi les cibles disponibles
            GameObject newTarget = entryTarget
                .Where(c => c != target && c != previousTarget)
                .OrderBy(c => Vector3.DistanceBetween(Transform.Position, c.Transform.Position))
                .FirstOrDefault();

            if (newTarget != null)
            {
                previousTarget = target;
                target = newTarget;
                Log.Info($"New target acquired from entryTarget: {target}");
            }
            else if (entryTarget.Count > 0)
            {
                // Si aucune nouvelle cible trouvée, choisir une cible aléatoire parmi les cibles disponibles
                Random rand = new Random();
                newTarget = entryTarget[rand.Next(entryTarget.Count)];
                previousTarget = target;
                target = newTarget;
                Log.Info($"Random new target acquired from entryTarget: {target}");
            }
            else
            {
                Log.Info("No new target found. Available targets:");
                foreach (var cible in entryTarget)
                {
                    Log.Info(cible.ToString());
                }
                Log.Info("Destroying the ball.");
                this.Destroy();
            }
        }
    }

    public GameObject FindClosestPlayerInVD(Vector3 direction)
    {
        GameObject closestPlayer = null;
        float minAngle = float.MaxValue;

        foreach (var gameObject in Scene.GetAllObjects(true))
        {
            if (gameObject.Components.TryGet<Cible>(out Cible behavior))
            {
                if (behavior.GameObject == target || behavior.GameObject == previousTarget) continue;

                Vector3 toPlayer = behavior.Transform.LocalPosition - this.Transform.LocalPosition;
                Vector3 normalizedToP = toPlayer.Normal;

                float angle = Vector3.Dot(direction, normalizedToP);

                // Log.Info($"Checking player {behavior}. Angle: {angle}, Direction: {direction}, ToPlayer: {normalizedToP}");

                if (angle > 0 && angle < minAngle)
                {
                    minAngle = angle;
                    closestPlayer = behavior.GameObject;
                }
            }
        }
        // Log.Info($"Closest player in direction: {closestPlayer}");
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

                    Log.Info("Collision with terrain detected. Adjusting position and velocity.");
                    break; // Sortir de la boucle après la première collision détectée
                }
            }
        }
    }
}
