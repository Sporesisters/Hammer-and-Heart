namespace Core.BehaviourTrees
{
	public interface IStrategy
	{
		BTStatus Process();
		void Reset() { }
	}
}
