namespace Core.Events;

/// <summary>
/// Specifies the relative importance of an event listener,
/// determining the order in which it is executed when an event is published.
/// </summary>
public enum EventPriority
{
	/// <summary>
	/// Listener executes later than <see cref="Normal"/> and <see cref="High"/> priority listeners.
	/// </summary>
	Low,

	/// <summary>
	/// Default priority. Listener executes after <see cref="High"/>
	/// but before <see cref="Low"/> priority listeners.
	/// </summary>
	Normal,

	/// <summary>
	/// Listener executes before <see cref="Normal"/> and <see cref="Low"/> priority listeners.
	/// </summary>
	High,
}
