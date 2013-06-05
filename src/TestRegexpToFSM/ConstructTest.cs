using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Phinite.Test
{
	[TestClass]
	public class ConstructTest
	{
		[TestMethod]
		public void Construct_Test1()
		{
			var fsm = ConstructFSM("a^*", 1, 1, new int[] { 0 });
		}

		[TestMethod]
		public void Construct_Test2()
		{
			var fsm = ConstructFSM("(a+b)^*", 1, 1, new int[] { 0 });
		}

		[TestMethod]
		public void Construct_Test3()
		{
			var fsm = ConstructFSM("(a+b+c)^*", 1, 1, new int[] { 0 });
		}

		[TestMethod]
		public void Construct_Test4()
		{
			var fsm = ConstructFSM("(a)^+", 2, 2, new int[] { 1 });
		}

		[TestMethod]
		public void Construct_Test5()
		{
			var fsm = ConstructFSM("(a)^+", 2, 2, new int[] { 1 });
		}

		private FiniteStateMachine ConstructFSM(string input, int statesCount, int transitionsCount,
			int[] acceptingIndexes)
		{
			RegularExpression regexp = new RegularExpression(input, true);
			FiniteStateMachine fsm = new FiniteStateMachine(regexp, true);

			var states = fsm.States;

			Assert.AreEqual(regexp, states[0]);
			Assert.AreEqual(0, fsm.RemainingStatesCount);
			Assert.AreEqual(0, fsm.RemainingTransitionsCount);

			Assert.AreEqual(statesCount, fsm.LabeledStatesCount);
			Assert.AreEqual(transitionsCount, fsm.LabeledTransitionsCount);
			for (int i = 0; i < states.Count; ++i)
				if (acceptingIndexes.Contains(i))
					Assert.IsTrue(fsm.IsAccepting(i));
				else
					Assert.IsFalse(fsm.IsAccepting(i));

			return fsm;
		}

	}
}
