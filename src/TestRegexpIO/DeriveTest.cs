using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Phinite.Test
{
	[TestClass]
	public class DeriveTest
	{
		RegularExpression regexp;

		RegularExpression expectedDerivation;

		RegularExpression derivation;

		[TestMethod]
		public void Concatenation_Test1()
		{
			ConstructAndDeriveRegexp("abc", "bc", "a");
		}

		[TestMethod]
		public void Concatenation_Test2()
		{
			ConstructAndDeriveRegexp("abc", null, "b");
		}

		[TestMethod]
		public void Union_Test1()
		{
			ConstructAndDeriveRegexp("a+b+c", ".", "a");
		}

		[TestMethod]
		public void Union_Test2()
		{
			ConstructAndDeriveRegexp("a+b+c", ".", "b");
		}

		[TestMethod]
		public void Union_Test3()
		{
			ConstructAndDeriveRegexp("a+b+c", ".", "c");
		}

		[TestMethod]
		public void Union_Test4()
		{
			ConstructAndDeriveRegexp("a+b+c", null, "d");
		}

		private void ConstructAndDeriveRegexp(string input, string expectedResult, string removedLetter)
		{
			// Arrange
			regexp = new RegularExpression(input, true);
			Console.Out.WriteLine("{0} optimized into {1}", input, regexp);
			if (expectedResult == null)
				expectedDerivation = null;
			else
				expectedDerivation = new RegularExpression(expectedResult, true);
			Console.Out.WriteLine("{0} optimized into {1}", expectedResult, expectedDerivation);

			// Act
			derivation = regexp.Derive(removedLetter);

			// Assert
			if (expectedDerivation == null)
				Assert.AreEqual(null, derivation);
			else
				Assert.AreEqual(true, expectedDerivation.Equals(derivation));
		}
	}
}
