using Sandbox;
using Sandbox.Citizen;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;


public sealed class Batte : Component
{

	[Property]
	public Vector3 battePostion { get; set; }



	protected override void DrawGizmos()
	{



		if ( !Gizmo.IsSelected ) return;
		var draw = Gizmo.Draw;
		draw.LineSphere( battePostion, 5f );

		


	}

	protected override void OnUpdate()
	{

	}
}
