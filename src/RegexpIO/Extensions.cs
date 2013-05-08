using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phinite
{
	/// <summary>
	/// Simple extension methods for arrays.
	/// </summary>
	public static class ArrayExtensions
	{
		/// <summary>
		/// Finds index of maximum value. Returns -1 if argument is empty. Throws if argument is null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <returns></returns>
		public static int IndexOfMax<T>(this T[] array) where T : IComparable
		{
			if (array == null)
				throw new ArgumentNullException("array");

			if (array.Length == 0)
				return -1;
			if (array.Length == 1)
				return 0;

			T max = array[0];
			int maxIndex = 0;
			int i = 0;
			foreach (T element in array)
			{
				if (element.CompareTo(max) > 0)
				{
					max = element;
					maxIndex = i;
				}
				++i;
			}
			return maxIndex;
		}

		/// <summary>
		/// Finds index of minimum value. Returns -1 if argument is empty. Throws if argument is null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <returns></returns>
		public static int IndexOfMin<T>(this T[] array) where T : IComparable
		{
			if (array == null)
				throw new ArgumentNullException("array");

			if (array.Length == 0)
				return -1;
			if (array.Length == 1)
				return 0;

			T min = array[0];
			int minIndex = 0;
			int i = 0;
			foreach (T element in array)
			{
				if (element.CompareTo(min) < 0)
				{
					min = element;
					minIndex = i;
				}
				++i;
			}
			return minIndex;
		}

	}
}
