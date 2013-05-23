using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Phinite.Test
{
	[TestClass]
	public class FiniteStateMachineLayoutTest
	{
		[TestMethod]
		public void LayoutScore_Test1()
		{
			var layout = CreateLayout(".");

			// Assert
			Assert.AreEqual(layout.Score, FiniteStateMachineLayoutScore.Perfect);
		}

		[TestMethod]
		public void LayoutScore_Test2()
		{
			var layout = CreateLayout("a");

			// Assert
			Assert.AreEqual(layout.Score, FiniteStateMachineLayoutScore.Perfect);
		}

		[TestMethod]
		public void LayoutScore_Test3()
		{
			var layout = CreateLayout("(a+b)^*");

			// Assert
			Assert.AreEqual(layout.Score, FiniteStateMachineLayoutScore.Perfect);
		}

		[TestMethod]
		public void LayoutScore_Test4()
		{
			var layout = CreateLayout("(0^*1^*2^*3^+)^+");

			// Assert
			if (layout.Score.Penalty == 0)
				Assert.Inconclusive("the layout algorithm is better than possible");

			Assert.IsTrue(layout.Score.Penalty < 0);
		}

		private FiniteStateMachineLayout CreateLayout(string inputRegexp)
		{
			// Arrange
			RegularExpression regexp = new RegularExpression(inputRegexp, true);
			FiniteStateMachine fsm = new FiniteStateMachine(regexp, true);
			FiniteStateMachineLayout layout = new FiniteStateMachineLayout(fsm);

			// Act
			layout.Create();

			return layout;
		}

	}
}
