using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UITesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UITest.Extension;
using Keyboard = Microsoft.VisualStudio.TestTools.UITesting.Keyboard;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Phinite.Test
{
	/// <summary>
	/// Summary description for CodedUITest1
	/// </summary>
	[CodedUITest]
	public class UITest
	{
		private PhiniteProcess phinite;

		public UITest() { }

		[TestInitialize]
		public void TestInitialize()
		{
			phinite = new PhiniteProcess();
			bool phiniteLaunched = phinite.Start();
			Assert.IsTrue(phiniteLaunched, "unable to launch Phinite for UI tests");
		}

		[TestCleanup]
		public void TestCleanup()
		{
			bool exitedInTime = phinite.WaitForExit();
			Assert.IsTrue(exitedInTime, "Phinite takes too much time to exit");
		}

		[TestMethod]
		public void RegexpExample1_Test()
		{
			// To generate code for this test, select "Generate Code for Coded UI Test" from the shortcut menu and select one of the menu items.
			// For more information on generated code, see http://go.microsoft.com/fwlink/?LinkId=179463

			this.UIMap.ClickExampleEmptyWord();
			this.UIMap.AssertRegexpInputTextEqual(App.ExpressionExamples["Empty word"]);

			ConstructAndEvaluateImmediately();
		}

		[TestMethod]
		public void RegexpExample2_Test()
		{
			this.UIMap.ClickExampleConcatenation();
			this.UIMap.AssertRegexpInputTextEqual(App.ExpressionExamples["Concatenation"]);

			ConstructAndEvaluateImmediately();
		}

		[TestMethod]
		public void RegexpExample22_Test()
		{
			this.UIMap.ClickExampleYay();
			this.UIMap.AssertRegexpInputTextEqual(App.ExpressionExamples["Yay!"]);

			ConstructAndEvaluateImmediately();
		}

		private void ConstructAndEvaluateImmediately()
		{
			Thread.Sleep(1000);
			this.UIMap.ClickRegexpInputImmediate();
			bool inConstructionMode = true;
			for (int i = 0; i < 10; ++i)
			{
				if (inConstructionMode)
					try
					{
						this.UIMap.ClickEvaluate();
					}
					catch (FailedToPerformActionOnBlockedControlException)
					{
						inConstructionMode = false;
					}
				try
				{
					this.UIMap.ClickWordInputImmediate();
					break;
				}
				catch (FailedToPerformActionOnBlockedControlException)
				{
					// the evaluation
					Thread.Sleep(4000);
				}
			}
			Thread.Sleep(2000);
			this.UIMap.ClickExit();
		}

		private void ConstructAndEvaluateStepByStep()
		{
			//this.UIMap.ClickRegexpInputStepByStep();
			// ...
			//this.UIMap.ClickAbort();
			this.UIMap.ClickEvaluate();
			//this.UIMap.ClickParseTreeStepByStep();
			//this.UIMap.ClickFirstScreen();
			this.UIMap.ClickExit();
		}

		#region Additional test attributes

		// You can use the following additional attributes as you write your tests:

		////Use TestInitialize to run code before running each test 
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{        
		//    // To generate code for this test, select "Generate Code for Coded UI Test" from the shortcut menu and select one of the menu items.
		//    // For more information on generated code, see http://go.microsoft.com/fwlink/?LinkId=179463
		//}

		////Use TestCleanup to run code after each test has run
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{        
		//    // To generate code for this test, select "Generate Code for Coded UI Test" from the shortcut menu and select one of the menu items.
		//    // For more information on generated code, see http://go.microsoft.com/fwlink/?LinkId=179463
		//}

		#endregion

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}
		private TestContext testContextInstance;

		public UIMap UIMap
		{
			get
			{
				if ((this.map == null))
				{
					this.map = new UIMap();
				}

				return this.map;
			}
		}

		private UIMap map;

	}
}
