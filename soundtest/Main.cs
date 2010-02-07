
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using MonoTouch.AudioToolbox;
using MonoTouch.AVFoundation;
using System.IO;

namespace soundtest
{
	public class Application
	{
		static void Main (string[] args)
		{
			UIApplication.Main (args);
		}
	}
	
	// The name AppDelegate is referenced in the MainWindow.xib file.
	public partial class AppDelegate : UIApplicationDelegate
	{
		//OpenALHelper sound;
		//bool playing = false;
		
		// This method is invoked when the application has loaded its UI and its ready to run
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			window.MakeKeyAndVisible ();
			
			//create caf files with the following command line: afconvert -f caff -d LEI16@22050 -c 1 <in> <out>
			// this can handle any kind of caf files (I think)
			OpenALHelper sound = new OpenALHelper("sound2.caf");
			sound.Loop = true;
			bool playing = false;
			
			
			btn.TouchUpInside += delegate {
				if (!playing)
					sound.Play();
				else
					sound.Stop();
				
				playing = !playing;
			};
			
			sld.ValueChanged += delegate {
				AL.Source(sound.Source, ALSourcef.Pitch, sld.Value);
			};
			
			return true;
		}
	
		// This method is required in iPhoneOS 3.0
		public override void OnActivated (UIApplication application)
		{
		}
	}
}
