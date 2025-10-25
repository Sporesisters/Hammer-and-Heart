using System;
using System.Collections.Generic;

namespace Core.Utilities.Extensions;

public static class ListExtensions
{
	private static readonly Random _rng = new();

	public static List<T> Shuffle<T>(this IEnumerable<T> source)
	{
		List<T> list = [.. source];
		int listSize = list.Count;

		while (listSize > 1)
		{
			listSize--;
			int k = _rng.Next(listSize + 1);
			(list[k], list[listSize]) = (list[listSize], list[k]);
		}

		return list;
	}
}
