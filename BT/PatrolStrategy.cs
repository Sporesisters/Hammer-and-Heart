using System.Collections.Generic;
using Godot;

namespace Core.BehaviourTrees
{
	public class PatrolStrategy : IStrategy
	{
		private readonly Entity actor;
		private readonly List<Vector3> patrolPoints;
		private readonly NavigationComponent nav;
		private int currentIndex = 0;

		public PatrolStrategy(Entity actor, List<Vector3> patrolPoints)
		{
			this.actor = actor;
			this.patrolPoints = patrolPoints;
			nav = actor.GetComponent<NavigationComponent>();
		}

		public BTStatus Process()
		{
			if (patrolPoints.Count == 0) return BTStatus.Failure;

			var target = patrolPoints[currentIndex];
			nav.MoveTo(target);

			var actorPos = actor.GetComponent<TransformComponent>().Position;
			if (actorPos.DistanceTo(target) < 0.5f)
			{
				currentIndex = (currentIndex + 1) % patrolPoints.Count;
			}

			return BTStatus.Running;
		}

		public void Reset() => currentIndex = 0;
	}
}
