using Sandbox;
using Sandbox.Citizen;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
public sealed class BotPlayer : Component
{

	[Property]
	public Bbplayer component {  get; set; }

	public void hitted()
	{
		var tr = Scene.Trace
			.Capsule( new Capsule( component.Transform.Position, component.CranePosition, 35f ) )
			.Run();

		if ( tr.Hit )
		{

			if ( tr.GameObject.Components.TryGet<Behavior>( out var behavior ) )
			{
				Log.Info( "Hitted propPLayer" );
				//behavior.nouvelCible();
				//dead();
				Random rand = new Random();
				Vector3 randomVector = new Vector3(
					(float)rand.NextDouble(),
					(float)rand.NextDouble(),
					(float)rand.NextDouble()
					);
				behavior.punch(randomVector );
			}
		}
	
	}

	protected override void OnFixedUpdate()
	{
		hitted();
	}

	
}
