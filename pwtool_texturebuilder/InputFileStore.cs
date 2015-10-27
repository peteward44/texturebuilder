using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;


namespace pwtool_texturebuilder
{
	public class InputFileStore
	{
		Dictionary<string, DateTime> mModificationTimes = new Dictionary<string, DateTime>();
		Settings mSettings;
		bool mIsSignatureValid = false;

		public Settings XmlSettings { get { return mSettings; } }


		public static InputFileStore LoadXmlFile( string xmlFile )
		{
			return new InputFileStore( xmlFile );
		}


		InputFileStore( string xmlFile )
		{
			System.IO.FileStream fs = null;
			XmlTextReader xmlReader = null;

			CheckXmlFileSignature( xmlFile );

			try
			{
				fs = new System.IO.FileStream( xmlFile, System.IO.FileMode.Open );
				xmlReader = new XmlTextReader( fs );

				if ( !xmlReader.ReadToFollowing( "Settings" ) )
					throw new Exception( "Settings xml not found" );
				mSettings = Settings.LoadFromXml( xmlReader );

				if ( !xmlReader.ReadToFollowing( "Image" ) )
					return;

				do
				{
					if ( !xmlReader.ReadToFollowing( "SubImage" ) )
						return;

					do
					{
						string filename = "";
						long lastModified = 0;

						do
						{
							string lowerName = xmlReader.Name.ToLower();

							switch ( lowerName )
							{
								case "name":
									filename = xmlReader.Value;
									break;
								case "lastmodified":
									long.TryParse( xmlReader.Value, out lastModified );
									break;
							}

						} while ( xmlReader.MoveToNextAttribute() );

						if ( filename.Length > 0 && lastModified > 0 )
							mModificationTimes.Add( filename, new DateTime( lastModified ) );
					} while ( xmlReader.ReadToNextSibling( "SubImage" ) );
				}
				while ( xmlReader.ReadToFollowing( "Image" ) );
			}
			catch ( System.Exception )
			{
			}
			finally
			{
				if ( xmlReader != null )
					xmlReader.Close();
				if ( fs != null )
					fs.Close();
			}
		}


		void CheckXmlFileSignature( string fileName )
		{
			try
			{
				if ( System.IO.File.Exists( fileName ) )
				{
					// Create a new CspParameters object to specify
					// a key container.
					CspParameters cspParams = new CspParameters();
					cspParams.KeyContainerName = "XML_DSIG_RSA_KEY";

					// Create a new RSA signing key and save it in the container. 
					RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider( cspParams );

					// Create a new XML document.
					XmlDocument xmlDoc = new XmlDocument();

					// Load an XML file into the XmlDocument object.
					xmlDoc.PreserveWhitespace = true;
					xmlDoc.Load( fileName );

					// Verify the signature of the signed XML.
					// Create a new SignedXml object and pass it
					// the XML document class.
					SignedXml signedXml = new SignedXml( xmlDoc );

					// Find the "Signature" node and create a new
					// XmlNodeList object.
					XmlNodeList nodeList = xmlDoc.GetElementsByTagName( "Signature" );

					// Throw an exception if no signature was found.
					if ( nodeList.Count <= 0 )
					{
						throw new CryptographicException( "Verification failed: No Signature was found in the document." );
					}

					// This example only supports one signature for
					// the entire XML document.  Throw an exception 
					// if more than one signature was found.
					if ( nodeList.Count >= 2 )
					{
						throw new CryptographicException( "Verification failed: More that one signature was found for the document." );
					}

					// Load the first <signature> node.  
					signedXml.LoadXml( (XmlElement)nodeList[ 0 ] );

					// Check the signature and return the result.
					mIsSignatureValid = signedXml.CheckSignature( rsaKey );
				}
			}
			catch ( System.Exception )
			{
				mIsSignatureValid = false;
			}
		}


		public bool RequiresBuild( List<string> inputDirectories )
		{
			try
			{
				if ( mModificationTimes.Count == 0 )
					return true;

				if ( !mIsSignatureValid ) // Settings file has been modified - rebuild it using new settings
					return true;

				int filesProcessedCount = 0;
				foreach ( string dir in inputDirectories )
				{
					if ( TestForModificationsRecursive( dir, ref filesProcessedCount ) )
						return true;
				}

				if ( filesProcessedCount != mModificationTimes.Count )
					return true; // this means a file has been removed from the directory since the textures were last built
			}
			catch ( System.Exception )
			{
				return true;
			}

			return false;
		}




		bool CheckForModification( string file, ref int filesProcessed )
		{
			if ( Util.IsImageFile( file ) )
			{
				DateTime currentFile = PWLib.Platform.Windows.File.GetLastWriteTimeUtc( file );

				string keyName = Util.FormatSubImageName( file );
				if ( mModificationTimes.ContainsKey( keyName ) )
				{
					if ( currentFile.Equals( mModificationTimes[ keyName ] ) )
					{
						filesProcessed++;
						return false;
					}
				}

				return true; // new file or	has been modified since last check
			}
			else
				return false;
		}


		bool TestForModificationsRecursive( string dir, ref int filesProcessed )
		{
			if ( System.IO.Directory.Exists( dir ) )
			{
				foreach ( string file in System.IO.Directory.GetFiles( dir ) )
				{
					if ( CheckForModification( file, ref filesProcessed ) )
						return true;
				}

				if ( Settings.Instance.Recursive )
				{
					foreach ( string file in System.IO.Directory.GetDirectories( dir ) )
					{
						if ( Util.IsValidDir( file ) )
						{
							if ( TestForModificationsRecursive( file, ref filesProcessed ) )
								return true;
						}
					}
				}
			}
			else if ( System.IO.File.Exists( dir ) )
			{
				if ( CheckForModification( dir, ref filesProcessed ) )
					return true;
			}
			return false;
		}

	}
}
