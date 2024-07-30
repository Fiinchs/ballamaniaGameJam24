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
	[Range( 1f, 100f )]
	public float BallSpeed { get; set; } = 40;


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
		Follow();
	}

	public void Follow()
	{
		Vector3 TargetPos = target.Transform.Position + new Vector3(0, 0, 50);
		Vector3 BallPos = Transform.Position; 

		Vector3 desiredPos = TargetPos - BallPos;

		//Log.Info( desiredPos );

		Transform.Position += desiredPos/BallSpeed ;
		
	
	}

	public void punch(Vector3 direction)
	{
		Vector3 BallPos = Transform.Position;
		Vector3 desiredPos = direction - BallPos;

		Log.Info( desiredPos );

		Transform.Position += desiredPos / BallSpeed;
	}



}
