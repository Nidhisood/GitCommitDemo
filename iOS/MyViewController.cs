﻿using System;

using UIKit;

namespace GitCommitDemo.iOS
{
	public partial class MyViewController : UIViewController
	{
		public MyViewController () : base ("MyViewController", null)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			// Perform any additional setup after loading the view, typically from a nib.

			// test branch....
		}

		public void test ()
		{
			// edited by master branch
		}

		public void test1 ()
		{

		}
		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}


