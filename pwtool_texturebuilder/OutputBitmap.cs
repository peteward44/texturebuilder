using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Xml;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;


namespace pwtool_texturebuilder
{

	public class SubImage
	{
		public SubImage( string filename, string name, Rectangle rect, bool rotated )
		{
			mFilename = filename;
			mName = name;
			mRect = rect;
			mRotated = rotated;
		}

		public string mFilename = "";
		public string mName = "";
		public Rectangle mRect;
		public bool mRotated = false; // Only makes sense to rotate an input bitmap 90 degrees
	}


	public class OutputBitmap : IDisposable
	{
		Bitmap mBitmap = null;
		List<Rectangle> mAvailableRectsList = new List<Rectangle>(); // same list, just sorted by size
		List<SubImage> mSubImages = new List<SubImage>();

		Size mVirtualSize = new Size( 1, 1 );

		string mOutputFilename;
		public string OutputFilename { get { return mOutputFilename; } }

		public List<Rectangle> AvailableRects { get { return mAvailableRectsList; } }
		public List<SubImage> SubImages { get { return mSubImages; } }

		bool mSingleImageOutput = false;
		public bool SingleImageOutput { get { return mSingleImageOutput; } }


		[DllImport( "msvcrt.dll", EntryPoint = "memcpy" )]
		unsafe static extern void CopyMemory( IntPtr pDest, IntPtr pSrc, int length );

		static IntPtr AddPtr( IntPtr src, int offset )
		{
			return new IntPtr( src.ToInt64() + offset );
		}


		public void CopyImageIntoImage( Bitmap source, Point destPos, bool isRotated )
		{
			if ( !isRotated )
			{
				System.Drawing.Imaging.BitmapData destData = mBitmap.LockBits( new Rectangle( destPos, source.Size ), System.Drawing.Imaging.ImageLockMode.WriteOnly,
					System.Drawing.Imaging.PixelFormat.Format32bppArgb );
				System.Drawing.Imaging.BitmapData srcData = source.LockBits( new Rectangle( 0, 0, source.Width, source.Height ), System.Drawing.Imaging.ImageLockMode.ReadOnly,
					System.Drawing.Imaging.PixelFormat.Format32bppArgb );

				for ( int line = 0; line < source.Height; ++line )
				{
					CopyMemory( AddPtr( destData.Scan0, line * destData.Stride ), AddPtr( srcData.Scan0, line * srcData.Stride ), srcData.Width * 4 );
				}

				source.UnlockBits( srcData );
				mBitmap.UnlockBits( destData );
			}
			else
			{
				for ( int x = 0; x < source.Height; ++x )
				{
					for ( int y = 0; y < source.Width; ++y )
					{
						mBitmap.SetPixel( destPos.X + x, destPos.Y + y, source.GetPixel( source.Width - y - 1, x ) );
					}
				}
			}
		}


		int SortRectComparer( Rectangle lhs, Rectangle rhs )
		{
			int lhsArea = lhs.Width * lhs.Height;
			int rhsArea = rhs.Width * rhs.Height;
			if ( lhsArea > rhsArea )
				return -1;
			else if ( lhsArea < rhsArea )
				return 1;
			else
				return 0;
		}


		public void SortRectList()
		{
			mAvailableRectsList.Sort( new Comparison<Rectangle>( SortRectComparer ) );
		}



		public OutputBitmap( string outputName, int index )
		{
			mOutputFilename = String.Format( "{0}_{1:d3}.png", outputName, index );
			mBitmap = new Bitmap( Settings.Instance.OutputBitmapSize.Width, Settings.Instance.OutputBitmapSize.Height,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb );
		}


		public OutputBitmap( string outputName, int index, int bitmapWidth, int bitmapHeight )
		{
			mSingleImageOutput = true;
			mOutputFilename = String.Format( "{0}_{1:d3}.png", outputName, index );
			mBitmap = new Bitmap( bitmapWidth, bitmapHeight,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb );
		}


		public void Dispose()
		{
			if ( mBitmap != null )
				mBitmap.Dispose();
		}


		public bool IncreaseVirtualSize()
		{
			if ( mVirtualSize.Width >= Settings.Instance.OutputBitmapSize.Width
							&& mVirtualSize.Height >= Settings.Instance.OutputBitmapSize.Height )
			{
				return false;
			}

			int extremeX = 0, extremeY = 0;
			GetImageExtremes( out extremeX, out extremeY );
			int spareWidth = mVirtualSize.Width - extremeX;
			int spareHeight = mVirtualSize.Height - extremeY;

			// determine if it will be more efficient to increase on right or bottom side
			bool increasingWidth = true;
			if ( mVirtualSize.Width >= Settings.Instance.OutputBitmapSize.Width )
				increasingWidth = false; // always expand height if width is maximum
			else if ( mVirtualSize.Height >= Settings.Instance.OutputBitmapSize.Height )
				increasingWidth = true; // always expand width if height is maximum
			else
			{
				// calculate area size is we expand both via the width and height, and expand in the direction which will result in less space
				int expandedSpareWidth = mVirtualSize.Width * 2 + spareWidth;
				int expandedSpareHeight = mVirtualSize.Height * 2 + spareHeight;
				int expandedSpareWidthVolume = expandedSpareWidth * mVirtualSize.Height;
				int expandedSpareHeightVolume = expandedSpareHeight * mVirtualSize.Width;

				increasingWidth = expandedSpareHeightVolume > expandedSpareWidthVolume;
			}
			
			// then create new available rect which covers new virtual size, and merge it with any available space on that side
			Rectangle newRect = new Rectangle();
			if ( increasingWidth )
				newRect = new Rectangle( extremeX, 0, mVirtualSize.Width + spareWidth, mVirtualSize.Height );
			else
				newRect = new Rectangle( 0, extremeY, mVirtualSize.Width, mVirtualSize.Height + spareHeight );

			Size newVirtualSize = new Size( mVirtualSize.Width * (increasingWidth ? 2 : 1), mVirtualSize.Height * (increasingWidth ? 1 : 2) );

			// and reduce the size of any rectangles which occupied that merged space
			List<KeyValuePair<Rectangle, Rectangle>> replacementList = new List<KeyValuePair<Rectangle, Rectangle>>();
			foreach ( Rectangle rect in mAvailableRectsList )
			{
				if ( newRect.Contains( rect ) )
				{
					replacementList.Add( new KeyValuePair<Rectangle, Rectangle>( rect, new Rectangle() ) );
				}
				else if ( newRect.IntersectsWith( rect ) )
				{
					int diffX = rect.Right - newRect.Left;
					if ( diffX < 0 )
						diffX = 0;
					int diffY = rect.Bottom - newRect.Top;
					if ( diffY < 0 )
						diffY = 0;
					Rectangle newSubRect = new Rectangle( rect.X, rect.Y, rect.Width - diffX, rect.Height - diffY );
					replacementList.Add( new KeyValuePair<Rectangle, Rectangle>( rect, newSubRect ) );
				}
			}

			foreach ( KeyValuePair<Rectangle, Rectangle> kvp in replacementList )
			{
				mAvailableRectsList.Remove( kvp.Key );

				if ( !kvp.Value.IsEmpty )
					mAvailableRectsList.Add( kvp.Value );
			}

			mAvailableRectsList.Add( newRect );

			SortRectList();

			mVirtualSize = newVirtualSize;

			return true;
		}


		public void InitVirtualSize( Size size )
		{
			mVirtualSize = new Size( Util.RoundUpToPowerOf2( size.Width ), Util.RoundUpToPowerOf2( size.Height ) );
			mAvailableRectsList.Add( new Rectangle( new Point( 0, 0 ), mVirtualSize ) );
		}




		void GetImageExtremes( out int extremeX, out int extremeY )
		{
			extremeX = extremeY = 0;
			foreach ( SubImage subimage in mSubImages )
			{
				if ( subimage.mRect.Right > extremeX )
					extremeX = subimage.mRect.Right;
				if ( subimage.mRect.Bottom > extremeY )
					extremeY = subimage.mRect.Bottom;
			}
		}


		void ShrinkIfNecessary()
		{
			System.Diagnostics.Debug.Assert( mBitmap != null );

			// find highest y coord and furthest right x coord to find image size
			int highestX = 0;
			int highestY = 0;
			GetImageExtremes( out highestX, out highestY );

			highestX = Util.RoundUpToPowerOf2( highestX );
			highestY = Util.RoundUpToPowerOf2( highestY );

			Bitmap newBitmap = new Bitmap( highestX, highestY );

			System.Drawing.Imaging.BitmapData destData = newBitmap.LockBits( new Rectangle( 0, 0, highestX, highestY ), System.Drawing.Imaging.ImageLockMode.WriteOnly,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb );
			System.Drawing.Imaging.BitmapData srcData = mBitmap.LockBits( new Rectangle( 0, 0, highestX, highestY ), System.Drawing.Imaging.ImageLockMode.ReadOnly,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb );

			for ( int line = 0; line < highestY; ++line )
			{
				CopyMemory( AddPtr( destData.Scan0, line * destData.Stride ), AddPtr( srcData.Scan0, line * srcData.Stride ), srcData.Width * 4 );
			}

			newBitmap.UnlockBits( srcData );
			mBitmap.UnlockBits( destData );

			mBitmap.Dispose();
			mBitmap = newBitmap;
		}



		public void SaveFile()
		{
			if ( !mSingleImageOutput )
				ShrinkIfNecessary();
			System.IO.Directory.CreateDirectory( Settings.Instance.OutputDir );
			string filename = System.IO.Path.Combine( Settings.Instance.OutputDir, mOutputFilename );
			if ( PWLib.Platform.Windows.File.Exists( filename ) )
				PWLib.Platform.Windows.File.Delete( filename );
			DevIL.DevIL.SaveBitmap( filename, mBitmap );
			System.Console.WriteLine( "Created " + PWLib.Platform.Windows.Path.GetFileName( filename )
				+ " (containing " + mSubImages.Count + " sub images)" );
		}


		public static bool IsInputImageValidSize( Bitmap inputBitmap )
		{
			return inputBitmap.Width < Settings.Instance.OutputBitmapSize.Width && inputBitmap.Height < Settings.Instance.OutputBitmapSize.Height;
		}


		static void SignXmlFile( string outputName )
		{
			CspParameters cspParams = new CspParameters();
			cspParams.KeyContainerName = "XML_DSIG_RSA_KEY";
			RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider( cspParams );
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.PreserveWhitespace = true;
			xmlDoc.Load( outputName );
			SignedXml signedXml = new SignedXml( xmlDoc );
			signedXml.SigningKey = rsaKey;

			// Create a Reference object that describes what to sign. To sign the entire document, set the Uri property to "".
			Reference reference = new Reference();
			reference.Uri = "";

			// Add an XmlDsigEnvelopedSignatureTransform object to the Reference object. A transformation allows the verifier to represent the XML data in the identical manner that the signer used.
			// XML data can be represented in different ways, so this step is vital to verification.
			XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
			reference.AddTransform( env );

			signedXml.AddReference( reference );
			signedXml.ComputeSignature();

			XmlElement xmlDigitalSignature = signedXml.GetXml();
			xmlDoc.DocumentElement.AppendChild( xmlDoc.ImportNode( xmlDigitalSignature, true ) );
			xmlDoc.Save( outputName );
		}


		public static void OutputXmlFile( List<OutputBitmap> outputList, string outputName )
		{
			string dirName = System.IO.Path.GetDirectoryName( outputName );
			System.IO.Directory.CreateDirectory( dirName );
			XmlTextWriter xmlWriter = new XmlTextWriter( outputName, Encoding.ASCII );
			xmlWriter.Formatting = Formatting.Indented;

			xmlWriter.WriteStartDocument();
			xmlWriter.WriteStartElement( "TextureBuilder" );
			xmlWriter.WriteAttributeString( "Version", "0.1" );

			Settings.Instance.OutputToXml( xmlWriter );

			foreach ( OutputBitmap outputBitmap in outputList )
			{
				outputBitmap.OutputToXml( xmlWriter );
			}

			xmlWriter.WriteFullEndElement();
			xmlWriter.Close();

			System.Console.WriteLine( "Signing XML " + outputName );

			// sign XML document after is has been saved
			SignXmlFile( outputName );

			System.Console.WriteLine( "Created " + outputName );
		}




		public void OutputToXml( XmlWriter xmlWriter )
		{
			xmlWriter.WriteStartElement( "Image" );
			xmlWriter.WriteAttributeString( "Filename", mOutputFilename );
			xmlWriter.WriteAttributeString( "w", mSingleImageOutput ? mBitmap.Width.ToString() : mVirtualSize.Width.ToString() );
			xmlWriter.WriteAttributeString( "h", mSingleImageOutput ? mBitmap.Height.ToString() : mVirtualSize.Height.ToString() );

			foreach ( SubImage subImage in mSubImages )
			{
				xmlWriter.WriteStartElement( "SubImage" );

				xmlWriter.WriteAttributeString( "name", subImage.mName );
				xmlWriter.WriteAttributeString( "x", subImage.mRect.X.ToString() );
				xmlWriter.WriteAttributeString( "y", subImage.mRect.Y.ToString() );
				xmlWriter.WriteAttributeString( "w", subImage.mRect.Width.ToString() );
				xmlWriter.WriteAttributeString( "h", subImage.mRect.Height.ToString() );
				xmlWriter.WriteAttributeString( "rotated", subImage.mRotated.ToString() );
				xmlWriter.WriteAttributeString( "lastmodified", PWLib.Platform.Windows.Directory.GetLastWriteTimeUtc( subImage.mFilename ).Ticks.ToString() );
				xmlWriter.WriteEndElement();
			}

			xmlWriter.WriteEndElement();
		}
	}

}
