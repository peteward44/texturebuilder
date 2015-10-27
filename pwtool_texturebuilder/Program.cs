using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PWLib.Platform;
using System.Drawing;
using System.Xml;

// Test arguments -r -d -rdir="../Target/testimages" -odir="../Target/out" "../Target/testimages"

namespace pwtool_texturebuilder
{

	static class Program
	{
		static string AppendDirSeperator( string s )
		{
			if ( s.EndsWith( new string( System.IO.Path.DirectorySeparatorChar, 1 ) ) )
				return s;
			else
				return s + System.IO.Path.DirectorySeparatorChar;
		}


		static int GetIntArg( string s, int def )
		{
			int w = 0;
			if ( int.TryParse( s, out w ) )
				return w;
			else
				return def;
		}


		static bool CheckForValidArgs( OptionSet optionSet, string[] args )
		{
			optionSet.Add( "r|recursive", "Recursively search input directories", delegate( string s ) { Settings.Instance.Recursive = true; } );
			optionSet.Add( "o=|output=", "Specify name of output xml and texture files", delegate( string s ) { Settings.Instance.OutputName = s; } );
			optionSet.Add( "d|directoryoutput", "Generates a texture atlas for each subdirectory of the input directory", delegate( string s ) { Settings.Instance.SubDirectoryOutput = true; } );
			optionSet.Add( "pw=|paddingwidth=", "Specify padding width used for packing sub images", delegate( string s ) { Settings.Instance.PaddingForced = true; Settings.Instance.Padding.Width = GetIntArg( s, Settings.Instance.Padding.Width ); } );
			optionSet.Add( "ph=|paddingheight=", "Specify padding height used for packing sub images", delegate( string s ) { Settings.Instance.PaddingForced = true; Settings.Instance.Padding.Height = GetIntArg( s, Settings.Instance.Padding.Height ); } );
			optionSet.Add( "f|force", "Force building of textures", delegate( string s ) { Settings.Instance.ForceBuild = true; } );
			optionSet.Add( "odir=|outputdir=", "Specify output directory", delegate( string s ) { Settings.Instance.OutputDir = AppendDirSeperator( s ); } );
			optionSet.Add( "rdir=|rootdir=", "Specify root directory", delegate( string s ) { Settings.Instance.RootDir = AppendDirSeperator( s ); } );
			optionSet.Add( "ow=|outputwidth=", "Specify maximum width of output textures", delegate( string s ) { Settings.Instance.OutputBitmapSizeForced = true; Settings.Instance.OutputBitmapSize.Width = GetIntArg( s, Settings.Instance.OutputBitmapSize.Width ); } );
			optionSet.Add( "oh=|outputheight=", "Specify maximum height of output textures", delegate( string s ) { Settings.Instance.OutputBitmapSizeForced = true; Settings.Instance.OutputBitmapSize.Height = GetIntArg( s, Settings.Instance.OutputBitmapSize.Height ); } );
			optionSet.Add( "ar|allowrotation", "Allows 90 degree rotation of sub images", delegate( string s ) { Settings.Instance.AllowRotationForced = true; Settings.Instance.AllowRotation = true; } );

			Settings.Instance.InputDirectories = optionSet.Parse( args );

			if ( Settings.Instance.InputDirectories.Count == 0 )
				return false;

			Settings.Instance.OutputDir = CleanDirString( Settings.Instance.OutputDir );
			Settings.Instance.RootDir = CleanDirString( Settings.Instance.RootDir );
			for ( int i = 0; i < Settings.Instance.InputDirectories.Count; ++i )
			{
				Settings.Instance.InputDirectories[ i ] = CleanDirString( Settings.Instance.InputDirectories[ i ] );
			}

			return true;
		}


		static string CleanDirString( string str )
		{
			while ( str.IndexOf( @"\\" ) >= 0 )
				str = str.Replace( @"\\", @"\" );
			while ( str.IndexOf( @"//" ) >= 0 )
				str = str.Replace( @"//", @"/" );
			return str;
		}


		static void PrintHelp( OptionSet optionSet )
		{
			Console.WriteLine( "Usage: pwtool_texturebuilder.exe [Options] {InputDirectories}" );
			optionSet.WriteOptionDescriptions( Console.Out );
		}


		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main( string[] args )
		{
			try
			{
				System.Console.WriteLine( "---------------------------" );
				System.Console.WriteLine( "| PW Texture Builder v0.1 |" );
				System.Console.WriteLine( "---------------------------" );

				OptionSet optionSet = new OptionSet();

				if ( !CheckForValidArgs( optionSet, args ) )
				{
					PrintHelp( optionSet );
					return -1;
				}
				else
				{
					Interface.Run();
				}
			}
			catch ( System.Exception e )
			{
				Console.WriteLine( "Exception caught " + e.Message );
				Console.WriteLine( e.StackTrace );
				return -2;
			}

			System.Console.WriteLine( "---------------------------" );

			return 0;
		}
	}
}


