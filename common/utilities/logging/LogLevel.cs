namespace Core.Utilities.Logging;

/// <summary>
/// Defines the severity levels used to categorize log entries.
/// </summary>
public enum LogLevel
{
	/// <summary>
	/// Logs highly detailed diagnostic information,
	/// primarily useful during development and debugging.
	/// <para>Typically excluded from production builds.</para>
	/// </summary>
	Debug,

	/// <summary>
	/// Logs general runtime events that describe
	/// the normal operation of the application.
	/// </summary>
	Info,

	/// <summary>
	/// Logs indications of unexpected conditions or potential issues
	/// that do not interrupt execution but may require attention.
	/// </summary>
	Warning,

	/// <summary>
	/// Logs significant errors that impact functionality
	/// and should be investigated promptly.
	/// </summary>
	Error,

	/// <summary>
	/// Logs critical failures that cause the application
	/// to terminate or enter an unrecoverable state.
	/// </summary>
	Fatal,
}
