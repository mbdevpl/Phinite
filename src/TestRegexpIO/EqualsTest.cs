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
		public void Concatenation1_Test()
		{
			ConstructAndCompareRegexp("aba", "aba", true);
		}

		[TestMethod]
		public void Concatenation2_Test()
		{
			ConstructAndCompareRegexp("aba", "abb", false);
		}

		[TestMethod]
		public void Concatenation3_Test()
		{
			ConstructAndCompareRegexp("aba", "abc", false);
		}

		[TestMethod]
		public void Union1_Test()
		{
			ConstructAndCompareRegexp("a+b", "b+a", true);
		}

		[TestMethod]
		public void Union2_Test()
		{
			ConstructAndCompareRegexp("a+b", "b+a+b", true);
		}

		[TestMethod]
		public void Union3_Test()
		{
			ConstructAndCompareRegexp("a+b+.", "a+b", false);
		}

		[TestMethod]
		public void Union4_Test()
		{
			ConstructAndCompareRegexp("a+b+c", "a+b", false);
		}

		[TestMethod]
		public void Union5_Test()
		{
			ConstructAndCompareRegexp("a+b+c", "c+a+b", true);
		}

		[TestMethod]
		public void KleeneConcatenation_Test()
		{
			ConstructAndCompareRegexp("aa^*", "a^+", true);
		}

		[TestMethod]
		public void KleeneUnion_Test()
		{
			ConstructAndCompareRegexp("a+b^*+c", "c+.+b^++a", true);
		}

		[TestMethod]
		public void Parenthesis1_Test()
		{
			ConstructAndCompareRegexp("a(bc)", "(ab)c", true);
		}

		[TestMethod]
		public void Parenthesis2_Test()
		{
			ConstructAndCompareRegexp("a+(b+c)", "(a+b)+c", true);
		}

		[TestMethod]
		public void Parenthesis3_Test()
		{
			ConstructAndCompareRegexp("a(b+c)", "(a+b)c", false);
		}

		[TestMethod]
		public void Parenthesis4_Test()
		{
			ConstructAndCompareRegexp("a+(bc)", "(ab)+c", false);
		}

		private void ConstructAndCompareRegexp(string input1, string input2, bool expectedEqual)
		{
			//Arrange

			//Act
			exp1 = new RegularExpression(input1, true);
			Console.Out.WriteLine("{0} optimized into {1}", input1, exp1);
			exp2 = new RegularExpression(input2, true);
			Console.Out.WriteLine("{0} optimized into {1}", input2, exp2);

			//Assert
			Assert.AreEqual(expectedEqual, exp1.Equals(exp2),
				String.Format("the expressions \"{0}\" and \"{1}\" were expected to{2} be equal, but they are{3}",
				exp1, exp2, expectedEqual ? "" : " not", expectedEqual ? " not" : ""));

			Console.Out.WriteLine("the expressions \"{0}\" and \"{1}\" are{2} equal, as expected",
				exp1, exp2, expectedEqual ? "" : " not");
		}
	}
}
