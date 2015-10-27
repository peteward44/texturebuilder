using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Drawing;

namespace pwtool_texturebuilder
{
	public class Settings
	{
		public static Settings Instance = new Settings();

		public bool Recursive = true;
		public bool AllowRotationForced = false;
		public bool AllowRotation = false;
		public List<string> InputDirectories = new List<string>();
		public bool PaddingForced = false;
		public Size Padding = new Size( 1, 1 );
		public bool ForceBuild = false;
		
		public string OutputName = "default";
		public bool SubDirectoryOutput = false;
		public string OutputDir = "";
		public string RootDir = "";
		public bool OutputBitmapSizeForced = false;
		public Size OutputBitmapSize = new Size( 2048, 2048 );


		public void MergeSettings( Settings settingsFromXml )
		{
			if ( !AllowRotationForced )
				AllowRotation = settingsFromXml.AllowRotation;
			if ( !PaddingForced )
				Padding = settingsFromXml.Padding;
			if ( !OutputBitmapSizeForced )
				OutputBitmapSize = settingsFromXml.OutputBitmapSize;
		}


		void OutputSettingsAttributes( XmlWriter xmlWriter )
		{
			xmlWriter.WriteAttributeString( "PaddingWidth", Padding.Width.ToString() );
			xmlWriter.WriteAttributeString( "PaddingHeight", Padding.Height.ToString() );
			xmlWriter.WriteAttributeString( "OutputWidth", OutputBitmapSize.Width.ToString() );
			xmlWriter.WriteAttributeString( "OutputHeight", OutputBitmapSize.Height.ToString() );
			xmlWriter.WriteAttributeString( "AllowRotation", AllowRotation.ToString() );
		}


		public void OutputToXml( XmlWriter xmlWriter )
		{
			xmlWriter.WriteStartElement( "Settings" );
			OutputSettingsAttributes( xmlWriter );
			xmlWriter.WriteEndElement();
		}


		public bool VerifySameSettings( Settings otherSettings )
		{
			if ( !Padding.Equals( otherSettings.Padding ) )
				return false;
			if ( !OutputBitmapSize.Equals( otherSettings.OutputBitmapSize ) )
				return false;
			if ( AllowRotation != otherSettings.AllowRotation )
				return false;
			return true;
		}


		public static Settings LoadFromXml( XmlReader xmlReader )
		{
			Settings settings = new Settings();
			xmlReader.MoveToAttribute( 0 );
			do
			{
				switch ( xmlReader.Name.ToLower() )
				{
					case "paddingwidth":
						{
							int pw = 0;
							if ( !int.TryParse( xmlReader.Value, out pw ) )
								throw new Exception( "Failed to parse paddingwidth" );
							settings.Padding.Width = pw;
						}
						break;
					case "paddingheight":
						{
							int ph = 0;
							if ( !int.TryParse( xmlReader.Value, out ph ) )
								throw new Exception( "Failed to parse paddingheight" );
							settings.Padding.Height = ph;
						}
						break;
					case "outputwidth":
						{
							int ow = 0;
							if ( !int.TryParse( xmlReader.Value, out ow ) )
								throw new Exception( "Failed to parse outputwidth" );
							settings.OutputBitmapSize.Width = ow;
						}
						break;
					case "outputheight":
						{
							int oh = 0;
							if ( !int.TryParse( xmlReader.Value, out oh ) )
								throw new Exception( "Failed to parse outputheight" );
							settings.OutputBitmapSize.Height = oh;
						}
						break;
					case "allowrotation":
						{
							bool ar = false;
							if ( !bool.TryParse( xmlReader.Value, out ar ) )
								throw new Exception( "Failed to parse allowrotation" );
							settings.AllowRotation = ar;
						}
						break;
				}
			} while ( xmlReader.MoveToNextAttribute() );

			return settings;
		}

	}

}
