using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Phinite.Test
{
	[TestClass]
	public class ConstructorTest
	{
		RegularExpression exp;

		bool thrown;

		Exception ex;

		public ConstructorTest()
		{
			//Arrange
			exp = null;
			thrown = false;
			ex = null;
		}

		[TestMethod]
		public void NonEmptyInvalidInputNotEvaluated_Test()
		{
			ConstructRegexp("ab^*^*", false);

			//Assert
			Assert.IsFalse(thrown);
			Assert.IsNotNull(exp);
		}

		[TestMethod]
		public void InvalidInput1_Test()
		{
			ConstructRegexp("(", true);

			//Assert
			Assert.IsTrue(thrown);
			Assert.IsTrue(typeof(ArgumentException).Equals(ex.GetType()));
			Assert.IsNull(exp);
		}

		[TestMethod]
		public void InvalidInput2_Test()
		{
			ConstructRegexp(")", true);

			//Assert
			Assert.IsTrue(thrown);
			Assert.IsTrue(typeof(ArgumentException).Equals(ex.GetType()));
			Assert.IsNull(exp);
		}

		[TestMethod]
		public void InvalidInput3_Test()
		{
			ConstructRegexp("(()", true);

			//Assert
			Assert.IsTrue(thrown);
			Assert.IsTrue(typeof(ArgumentException).Equals(ex.GetType()));
			Assert.IsNull(exp);
		}

		[TestMethod]
		public void InvalidInput4_Test()
		{
			ConstructRegexp("())", true);

			//Assert
			Assert.IsTrue(thrown);
			Assert.IsTrue(typeof(ArgumentException).Equals(ex.GetType()));
			Assert.IsNull(exp);
		}

		[TestMethod]
		public void EmptyInput_Test()
		{
			ConstructRegexp(String.Empty, false);

			//Assert
			Assert.IsTrue(thrown);
			Assert.IsTrue(typeof(ArgumentException).Equals(ex.GetType()));
			Assert.IsNull(exp);
		}

		[TestMethod]
		public void EmptyInput_Immediate_Test()
		{
			ConstructRegexp(String.Empty, true);

			//Assert
			Assert.IsTrue(thrown);
			Assert.IsTrue(typeof(ArgumentException).Equals(ex.GetType()));
			Assert.IsNull(exp);
		}

		[TestMethod]
		public void NullInput_Test()
		{
			ConstructRegexp(null, false);

			//Assert
			Assert.IsTrue(thrown);
			Assert.IsTrue(typeof(ArgumentNullException).Equals(ex.GetType()));
			Assert.IsNull(exp);
		}

		[TestMethod]
		public void NullInput_Immediate_Test()
		{
			ConstructRegexp(null, true);

			//Assert
			Assert.IsTrue(thrown);
			Assert.IsTrue(typeof(ArgumentNullException).Equals(ex.GetType()));
			Assert.IsNull(exp);
		}

		private void ConstructRegexp(string input, bool immediateMode)
		{
			//Act
			try
			{
				exp = new RegularExpression(input, immediateMode);
			}
			catch (ArgumentNullException e)
			{
				thrown = true;
				ex = e;
				System.Console.Out.WriteLine("ArgumentNullException thrown:\n{0}", e);
			}
			catch (ArgumentException e)
			{
				thrown = true;
				ex = e;
				System.Console.Out.WriteLine("ArgumentException thrown:\n{0}", e);
			}
			catch (Exception e)
			{
				thrown = true;
				ex = e;
				System.Console.Out.WriteLine("Exception thrown:\n{0}", e);
				Assert.Fail("Unexpected kind of exception!");
			}
		}

	}
}
