using Godot;
using Core.Utilities.Logging;
using System.Collections.Generic;

/// <summary>
/// A reusable node that provides blackboard-style key-value data storage.
/// Keys are normalized to lowercase and trimmed for consistency.
/// </summary>
[GlobalClass]
public partial class BlackBoard : Node
{
	private readonly Dictionary<string, Variant> _dataStorage = [];

	/// <summary>
	/// Normalizes a key to lowercase and trims whitespace for consistent usage.
	/// </summary>
	/// <param name="key">The raw key.</param>
	/// <returns>The normalized key.</returns>
	private static string NormalizeKey(string key) => key.ToLowerInvariant().Trim();

	/// <summary>
	/// Stores or updates data in the blackboard.
	/// </summary>
	/// <param name="key">The unique identifier for the data entry.</param>
	/// <param name="data">The <see cref="Variant"/> value to store or update.</param>
	public void SetData(string key, Variant data)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			LoggerService.Warning($"Cannot set data: key is null or empty.");
			return;
		}

		string normalizedKey = NormalizeKey(key);
		bool isUpdate = _dataStorage.ContainsKey(normalizedKey);

		_dataStorage[normalizedKey] = data;

		LoggerService.Debug($"{(isUpdate ? "Updated" : "Added")} key '{normalizedKey}' with value: {data}");
	}

	/// <summary>
	/// Retrieves data from the blackboard.
	/// </summary>
	/// <typeparam name="T">The expected type of the stored value.</typeparam>
	/// <param name="key">The key of the value to retrieve.</param>
	/// <returns>The stored value as type <typeparamref name="T"/> if available, otherwise default.</returns>
	public T? GetData<[MustBeVariant] T>(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			LoggerService.Warning("Cannot get data: key is null or empty.");
			return default;
		}

		string normalizedKey = NormalizeKey(key);

		if (!_dataStorage.TryGetValue(normalizedKey, out Variant data))
		{
			LoggerService.Info($"Key '{normalizedKey}' not found in blackboard.");
			return default;
		}

		try
		{
			return data.As<T>();
		}
		catch
		{
			if (data.Obj is null)
			{
				LoggerService.Error(
					$"Key '{normalizedKey}' is null (Variant.Nil), requested {typeof(T).Name}."
				);
			}
			else
			{
				LoggerService.Error(
					$"Type mismatch for key '{normalizedKey}': stored {data.Obj.GetType().Name}, requested {typeof(T).Name}."
				);
			}

			return default;
		}
	}

	/// <summary>
	/// Attempts to retrieve data without logging or throwing errors.
	/// </summary>
	public bool TryGetData<[MustBeVariant] T>(string key, out T? value)
	{
		value = default;

		if (string.IsNullOrWhiteSpace(key)) return false;

		string normalizedKey = NormalizeKey(key);

		if (_dataStorage.TryGetValue(normalizedKey, out Variant data))
		{
			try
			{
				value = data.As<T>();
				return true;
			}
			catch { return false; }
		}

		return false;
	}

	/// <summary>
	/// Checks whether a key exists.
	/// </summary>
	public bool ContainsData(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			LoggerService.Debug("Contains check failed: key is null or empty.");
			return false;
		}

		return _dataStorage.ContainsKey(NormalizeKey(key));
	}

	/// <summary>
	/// Removes a key-value pair from the blackboard.
	/// </summary>
	public void RemoveData(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			LoggerService.Error("Cannot remove data: key is null or empty.");
			return;
		}

		string normalizedKey = NormalizeKey(key);

		if (_dataStorage.Remove(normalizedKey))
		{
			LoggerService.Info($"Removed key '{normalizedKey}' from blackboard.");
			return;
		}

		LoggerService.Debug($"Tried to remove key '{normalizedKey}', but it was not found.");
	}

	/// <summary>
	/// Clears all stored key-value pairs.
	/// </summary>
	public void ClearAllData()
	{
		_dataStorage.Clear();
		LoggerService.Info("Cleared all blackboard data.");
	}

	/// <summary>
	/// Returns all stored keys.
	/// </summary>
	public IEnumerable<string> GetAllKeys() => _dataStorage.Keys;
}
