using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace pwtool_texturebuilder
{
	public class Util
	{
		public static bool IsImageFile( string file )
		{
			string ext = PWLib.Platform.Windows.Path.GetExtension( file ).ToLower();
			return ext == ".png" || ext == ".dds";
		}


		public static bool IsValidDir( string dir )
		{
			string dirname = PWLib.Platform.Windows.Path.GetLeafName( dir );
			return !dirname.StartsWith( "." );
		}


		public static bool IsValidRect( Rectangle rect )
		{
			return rect.X >= 0 && rect.Y >= 0 && rect.Width > 0 && rect.Height > 0;
		}


		public static int RoundUpToPowerOf2( int num )
		{
			for ( int test = 1; true; test *= 2 )
			{
				if ( num <= test )
					return test;
			}
		}


		public static string FormatSubImageName( string inputBitmapName )
		{
			if ( inputBitmapName.ToLower().StartsWith( Settings.Instance.RootDir.ToLower() ) )
			{
				return inputBitmapName.Substring( Settings.Instance.RootDir.Length );
			}
			else
				return inputBitmapName;
		}

	}
}
