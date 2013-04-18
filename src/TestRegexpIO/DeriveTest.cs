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
		public void Concatenation1_Test()
		{
			ConstructAndDeriveRegexp("abc", "bc", "a");
		}

		[TestMethod]
		public void Concatenation2_Test()
		{
			ConstructAndDeriveRegexp("abc", null, "b");
		}

		[TestMethod]
		public void Union1_Test()
		{
			ConstructAndDeriveRegexp("a+b+c", ".", "a");
		}

		[TestMethod]
		public void Union2_Test()
		{
			ConstructAndDeriveRegexp("a+b+c", ".", "b");
		}

		[TestMethod]
		public void Union3_Test()
		{
			ConstructAndDeriveRegexp("a+b+c", ".", "c");
		}

		[TestMethod]
		public void Union4_Test()
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
