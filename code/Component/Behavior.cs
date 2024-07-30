using Sandbox;
using System;

public sealed class Behavior : Component
{
	[Property]
	[Category( "Component" )]
	public GameObject Camera { get; set; }

	[Property]
	public Vector3 EyePosition { get; set; }

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 10000f )]
	public float BallSpeed { get; set; } = 500f;


	[Property]
	public Bbplayer target { get; set; }


	protected override void OnStart()
	{
		base.OnStart();


	}

	protected override void OnUpdate()
	{

	}

	protected override void OnFixedUpdate()
	{
		Transform.Position += new Vector3( 5, 0, 0 );
	}

	public void Follow()
	{
		Vector3 TargetPos = target.Transform.Position;
		Vector3 BallPos = target.Transform.Position; 

		Vector3 desiredPos = BallPos - TargetPos;
		Transform.Position += desiredPos - 100; 
	}

}
