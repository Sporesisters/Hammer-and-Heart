namespace Core.ECS;

/// <summary>
/// Which side of the Monster vs Robot split an entity belongs to (GDD p.2, p.9).
/// Elaine's hammer only affects robots and Annabelle's kisses only affect monsters.
/// </summary>
public enum EntityFaction
{
	/// <summary>Not part of either faction (the girls, items, projectiles...).</summary>
	None,

	/// <summary>Organic monster: calmed by Annabelle, protected from Elaine's hammer.</summary>
	Monster,

	/// <summary>Robot: smashed by Elaine, unaffected by Annabelle's kisses.</summary>
	Robot,
}
