﻿//
// Copyright 2011-2012 Xamarin Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Android.App;
using Android.OS;
using Android.Widget;
using MonoDroid.Dialog;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;

namespace Android.NUnitLite.UI {

    public class RunnerActivity : Activity {
		
		Section main;
		
		public RunnerActivity ()
		{
			Initialized = (AndroidRunner.AssemblyLevel.Count > 0);
		}
		
		public bool Initialized {
			get; private set;
		}
		
		public AndroidRunner Runner {
			get { return AndroidRunner.Runner; }
		}

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

			if (Runner.Options == null)
				Runner.Options = new Options (this);
			
			var menu = new RootElement ("Test Runner");
			
			main = new Section ("Test Suites");
			foreach (TestSuite suite in AndroidRunner.AssemblyLevel) {
				main.Add (new TestSuiteElement (suite));
			}
			menu.Add (main);

			Section options = new Section () {
				new ActionElement ("Run Everything", Run),
				new ActivityElement ("Options", typeof (OptionsActivity)),
				new ActivityElement ("Credits", typeof (CreditsActivity))
			};
			menu.Add (options);

			var lv = new ListView (this) {
				Adapter = new DialogAdapter (this, menu)
			};
			SetContentView (lv);

			// AutoStart running the tests (with either the supplied 'writer' or the options)
			if (Runner.AutoStart) {
				string msg = String.Format ("Automatically running tests{0}...", 
					Runner.TerminateAfterExecution ? " and closing application" : String.Empty);
				Toast.MakeText (this, msg, ToastLength.Long).Show ();
				ThreadPool.QueueUserWorkItem (delegate {
					RunOnUiThread (delegate {
						Run ();	
						// optionally end the process, e.g. click "Andr.Unit" -> log tests results, return to springboard...
						if (Runner.TerminateAfterExecution)
							Finish ();
					});
				});
			}
		}

        NUnitLiteTestAssemblyBuilder builder = new NUnitLiteTestAssemblyBuilder();

		public void Add (Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException ("assembly");

			// this can be called many times but we only want to load them
			// once since we need to share them across most activities
			if (!Initialized)
			{
                var ts = builder.Build(assembly, new Dictionary<string, object>());

                if (ts == null) return;

				// TestLoader.Load always return a TestSuite so we can avoid casting many times
				AndroidRunner.AssemblyLevel.Add (ts);
				Add (ts);
			}
		}
		
		void Add (TestSuite suite)
		{
			AndroidRunner.Suites.Add (suite.FullName ?? suite.Name, suite);
			foreach (ITest test in suite.Tests) {
				TestSuite ts = (test as TestSuite);
				if (ts != null)
					Add (ts);
			}
		}

		void Run ()
		{
			if (!Runner.OpenWriter ("Run Everything", this))
				return;
			
			try {
				foreach (TestSuite suite in AndroidRunner.AssemblyLevel)
				{
				    Runner.Run(suite);
				}
			}
			finally {
				Runner.CloseWriter ();
			}
			
			foreach (TestElement te in main) {
				te.Update ();
			}
		}
    }
}