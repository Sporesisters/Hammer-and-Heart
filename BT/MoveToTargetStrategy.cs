namespace Core.BehaviourTrees
{
	public class MoveToTargetStrategy : IStrategy
	{
		private readonly Entity actor;
		private readonly Entity target;
		private bool pathInProgress = false;

		public MoveToTargetStrategy(Entity actor, Entity target)
		{
			this.actor = actor;
			this.target = target;
		}

		public BTStatus Process()
		{
			var actorPos = actor.GetComponent<TransformComponent>().Position;
			var targetPos = target.GetComponent<TransformComponent>().Position;
			var nav = actor.GetComponent<NavigationComponent>();

			if (actorPos.DistanceTo(targetPos) < 1f)
			{
				pathInProgress = false;
				return BTStatus.Success;
			}

			nav.MoveTo(targetPos); // Handles movement per frame
			pathInProgress = true;
			return BTStatus.Running;
		}

		public void Reset() => pathInProgress = false;
	}
}
