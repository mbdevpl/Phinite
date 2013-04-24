using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Phinite.Test
{
	[TestClass]
	public class EqualsTest
	{
		RegularExpression exp1;

		RegularExpression exp2;

		[TestMethod]
		public void EmptyWord_Test()
		{
			ConstructAndCompareRegexp(".", ".", true);
		}

		[TestMethod]
		public void Concatenation_Test1()
		{
			ConstructAndCompareRegexp("aba", "aba", true);
		}

		[TestMethod]
		public void Concatenation_Test2()
		{
			ConstructAndCompareRegexp("aba", "abb", false);
		}

		[TestMethod]
		public void Concatenation_Test3()
		{
			ConstructAndCompareRegexp("aba", "abc", false);
		}

		[TestMethod]
		public void Union_Test1()
		{
			ConstructAndCompareRegexp("a+b", "b+a", true);
		}

		[TestMethod]
		public void Union_Test2()
		{
			ConstructAndCompareRegexp("a+b", "b+a+b", true);
		}

		[TestMethod]
		public void Union_Test3()
		{
			ConstructAndCompareRegexp("a+b+.", "a+b", false);
		}

		[TestMethod]
		public void Union_Test4()
		{
			ConstructAndCompareRegexp("a+b+c", "a+b", false);
		}

		[TestMethod]
		public void Union_Test5()
		{
			ConstructAndCompareRegexp("a+b+c", "c+a+b", true);
		}

		[TestMethod]
		public void KleeneConcatenation_Test()
		{
			ConstructAndCompareRegexp("aa^*", "a^+", false);
		}

		[TestMethod]
		public void KleeneUnion_Test()
		{
			ConstructAndCompareRegexp("a+b^*+c", "c+.+b^++a", false);
		}

		[TestMethod]
		public void Parenthesis_Test1()
		{
			ConstructAndCompareRegexp("a(bc)", "(ab)c", true);
		}

		[TestMethod]
		public void Parenthesis_Test2()
		{
			ConstructAndCompareRegexp("a+(b+c)", "(a+b)+c", true);
		}

		[TestMethod]
		public void Parenthesis_Test3()
		{
			ConstructAndCompareRegexp("a(b+c)", "(a+b)c", false);
		}

		[TestMethod]
		public void Parenthesis_Test4()
		{
			ConstructAndCompareRegexp("a+(bc)", "(ab)+c", false);
		}

		private void ConstructAndCompareRegexp(string input1, string input2, bool expectedEqual)
		{
			//Arrange
			exp1 = new RegularExpression(input1, true);
			Console.Out.WriteLine("{0} optimized into {1}", input1, exp1);
			exp2 = new RegularExpression(input2, true);
			Console.Out.WriteLine("{0} optimized into {1}", input2, exp2);

			//Act

			//Assert
			Assert.AreEqual(expectedEqual, exp1.Equals(exp2),
				String.Format("the expressions \"{0}\" and \"{1}\" were expected to{2} be equal, but they are{3}",
				exp1, exp2, expectedEqual ? "" : " not", expectedEqual ? " not" : ""));

			Console.Out.WriteLine("the expressions \"{0}\" and \"{1}\" are{2} equal, as expected",
				exp1, exp2, expectedEqual ? "" : " not");
		}

	}
}
