using Sandbox;
using System;
using System.Linq.Expressions;

public enum UnitType
{

	/// <summary>
	///  Environement
	/// </summary>
	[Icon( "check_box_outline_blank" )]
	None,
	/// <summary>
	///  Player / me 
	/// </summary>
	[Icon( "boy" )]
	Player,
	/// <summary>
	///  bitches / me 
	/// </summary>
	[Icon( "group_off" )]
	Ennemie,

}





public sealed class Unitinfo : Component
{
	[Property]
	public UnitType Team { get; set; }


	[Property]
	[Range( 0f, 10f, 0.1f )]
	public float HealthRegenAmount { get; set; } = 0.5f;


	[Property]
	[Range( 1f, 5f, 1f )]
	public float HealthRegenTime { get; set; } = 3f;


	[Property]
	[Range( 0.1f, 10f, 0.1f )]
	public float MaxHealt { get; set; } = 10f;

	public List<Bbplayer> Players { get; set; }

	public Scene map { get; set; }

	public float Health { get; private set; }

	public bool Alive { get; private set; } = true;

	TimeSince _lastDamage;
	TimeUntil _nextHeal;


	public event Action<float> OnDamage;
	public event Action OnDeath;

	protected override void OnStart()
	{
		Health = MaxHealt;
	}

	protected override void OnUpdate()
	{
		if ( _lastDamage >= HealthRegenTime && Health != MaxHealt && Alive )
		{
			if ( _nextHeal )
			{
				Damage( -HealthRegenAmount );
				_nextHeal = 1f;
			}
		}
		//Log.Info(Health);

	}

	/// <summary>
	/// mets des dommages sur l'unit clamp heal if set to negative
	/// </summary>
	/// <param name="damage"></param>
	public void Damage( float damage )
	{
		if ( !Alive ) return;

		Health = Math.Clamp( Health - damage, 0f, MaxHealt );

		if ( damage > 0 )
		{
			_lastDamage = 0f;
		}

		OnDamage?.Invoke( damage );

		if ( Health <= 0 )
			Krill();
	}

	/// <summary>
	/// set the hp to 0 alive to false then destroys it
	/// </summary>
	public void Krill()
	{
		Health = 0f;
		Alive = false;

		OnDeath?.Invoke();

		GameObject.Destroy();
	}



}
