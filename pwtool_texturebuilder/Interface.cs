using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Xml;


namespace pwtool_texturebuilder
{
	public class Interface
	{

		static string GetXmlFilename( string outputName )
		{
			return System.IO.Path.Combine( Settings.Instance.OutputDir, outputName + ".xml" );
		}


		static void LoadImage( string file, List<InputBitmap> bitmapList )
		{
			if ( Util.IsImageFile( file ) )
			{
				Bitmap bm = DevIL.DevIL.LoadBitmap( file );
				if ( bm != null )
				{
					bitmapList.Add( new InputBitmap( file, bm ) );
					System.Console.WriteLine( "Loaded " + file );
				}
			}
		}


		static void ParseDir( string dir, List<InputBitmap> bitmapList )
		{
			if ( System.IO.Directory.Exists( dir ) )
			{
				foreach ( string file in System.IO.Directory.GetFiles( dir ) )
				{
					LoadImage( file, bitmapList );
				}

				if ( Settings.Instance.Recursive )
				{
					foreach ( string file in System.IO.Directory.GetDirectories( dir ) )
					{
						if ( Util.IsValidDir( file ) )
							ParseDir( file, bitmapList );
					}
				}
			}
			else if ( System.IO.File.Exists( dir ) )
			{
				LoadImage( dir, bitmapList );
			}
		}


		public static void Run()
		{
			if ( Settings.Instance.SubDirectoryOutput )
			{
				foreach ( string inputDir in Settings.Instance.InputDirectories )
				{
					foreach ( string inputSubDir in PWLib.Platform.Windows.Directory.GetDirectories( inputDir ) )
					{
						string directoryOnlyName = PWLib.Platform.Windows.Path.GetLeafName( inputSubDir );
						if ( !directoryOnlyName.StartsWith( "." ) )
						{
							List<string> tempList = new List<string>();
							tempList.Add( inputSubDir );
							RunDirectory( tempList, directoryOnlyName );
						}
					}
				}
			}
			else
			{
				RunDirectory( Settings.Instance.InputDirectories, Settings.Instance.OutputName );
			}
		}


		static void RunDirectory( List< string > inputDirectories, string outputName )
		{
			// Load xml file (if exists)
			string xmlFilename = GetXmlFilename( outputName );
			InputFileStore fileStore = InputFileStore.LoadXmlFile( xmlFilename );

			// see if any images have been added / removed / modified
			if ( Settings.Instance.ForceBuild || fileStore.RequiresBuild( inputDirectories ) )
			{
				System.Console.WriteLine( "Modifications detected, processing..." );

				if ( fileStore.XmlSettings != null )
					Settings.Instance.MergeSettings( fileStore.XmlSettings );

				// if they have, rebuild entire texture listing
				List<InputBitmap> inputBitmaps = new List<InputBitmap>();

				foreach ( string dir in inputDirectories )
				{
					ParseDir( dir, inputBitmaps );
				}

				List<OutputBitmap> outputBitmaps = PackingProcess.BreakIntoOutputBitmaps( inputBitmaps, outputName );

				int index = 0;
				foreach ( OutputBitmap obi in outputBitmaps )
				{
					obi.SaveFile();
					index++;
				}

				foreach ( InputBitmap ib in inputBitmaps )
				{
					ib.mBitmap.Dispose();
					ib.mBitmap = null;
				}

				OutputBitmap.OutputXmlFile( outputBitmaps, xmlFilename );

				System.Console.WriteLine( "Texture building completed successfully" );
			}
			else
			{
				System.Console.WriteLine( "No modifications detected, exiting..." );
			}
		}
	}
}
