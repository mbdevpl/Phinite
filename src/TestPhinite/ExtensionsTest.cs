using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Phinite.Test
{
	[TestClass]
	public class ExtensionsTest
	{
		[TestMethod]
		public void Instersects_Test1()
		{
			InvokeIntersects(new Point(0, 0), new Point(10, 10), new Point(1, 11), new Point(9, 19), false);
		}

		[TestMethod]
		public void Instersects_Test2()
		{
			InvokeIntersects(new Point(0, 4), new Point(10, 5), new Point(5, 0), new Point(5, 10), true);
		}

		[TestMethod]
		public void Instersects_Test3()
		{
			InvokeIntersects(new Point(0, 4), new Point(10, 5), new Point(5, 0), new Point(10, 10), true);
		}

		[TestMethod]
		public void Instersects_Test4()
		{
			InvokeIntersects(new Point(0, 4), new Point(10, 5), new Point(5, 0), new Point(14, 10), true);
		}

		[TestMethod]
		public void Instersects_Test5()
		{
			InvokeIntersects(new Point(0, 4), new Point(10, 5), new Point(5, 0), new Point(16, 10), false);
		}

		[TestMethod]
		public void Instersects_Test6()
		{
			InvokeIntersects(new Point(0, 4), new Point(10, 5), new Point(5, 0), new Point(2, 10), true);
		}

		[TestMethod]
		public void Instersects_Test7()
		{
			InvokeIntersects(new Point(0, 4), new Point(10, 5), new Point(5, 0), new Point(-4, 10), true);
		}

		[TestMethod]
		public void Instersects_Test8()
		{
			InvokeIntersects(new Point(0, 4), new Point(10, 5), new Point(5, 0), new Point(-9, 10), false);
		}

		[TestMethod]
		public void Instersects_Test9()
		{
			InvokeIntersects(new Point(6, 6), new Point(9, 9), new Point(10, 0), new Point(0, 10), false);
		}

		[TestMethod]
		public void Instersects_Test10()
		{
			InvokeIntersects(new Point(6, 0), new Point(5, 10), new Point(0, 5), new Point(10, -5), false);
		}

		[TestMethod]
		public void Instersects_Test11()
		{
			InvokeIntersects(new Point(6, 0), new Point(5, 10), new Point(0, 5), new Point(10, 2), true);
		}

		[TestMethod]
		public void Instersects_Test12()
		{
			InvokeIntersects(new Point(6, 0), new Point(5, 10), new Point(0, 5), new Point(10, 5), true);
		}

		[TestMethod]
		public void Instersects_Test13()
		{
			InvokeIntersects(new Point(6, 0), new Point(5, 10), new Point(0, 5), new Point(10, 7), true);
		}

		[TestMethod]
		public void Instersects_Test14()
		{
			InvokeIntersects(new Point(6, 0), new Point(5, 10), new Point(0, 5), new Point(10, 10), true);
		}

		[TestMethod]
		public void Instersects_Test15()
		{
			InvokeIntersects(new Point(6, 0), new Point(5, 10), new Point(0, 5), new Point(10, 13), true);
		}

		[TestMethod]
		public void Instersects_Test16()
		{
			InvokeIntersects(new Point(6, 0), new Point(5, 10), new Point(0, 5), new Point(10, 18), false);
		}

		private void InvokeIntersects(Point pt11, Point pt12, Point pt21, Point pt22, bool expectedResult)
		{
			// Arrange
			bool[] results = new bool[8];
			string msg = String.Format("error in {0}-{1} vs. {2}-{3}", pt11, pt12, pt21, pt22);

			// Act
			results[0] = Extensions.Intersects(pt11, pt12, pt21, pt22);
			results[1] = Extensions.Intersects(pt12, pt11, pt21, pt22);
			results[2] = Extensions.Intersects(pt11, pt12, pt22, pt21);
			results[3] = Extensions.Intersects(pt12, pt11, pt22, pt21);
			results[4] = Extensions.Intersects(pt21, pt22, pt11, pt12);
			results[5] = Extensions.Intersects(pt22, pt21, pt11, pt12);
			results[6] = Extensions.Intersects(pt21, pt22, pt12, pt11);
			results[7] = Extensions.Intersects(pt22, pt21, pt12, pt11);

			// Assert
			List<int> errors = new List<int>();
			int i = 0;
			foreach (var result in results)
			{
				if (result != expectedResult)
					errors.Add(i);
				++i;
			}
			Assert.AreEqual(0, errors.Count, String.Format("{0} in variants: [{1}]", msg, String.Join(",",errors)));

		}

	}
}
