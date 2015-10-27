using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace pwtool_texturebuilder
{
	public class PackingProcess
	{
		static int GetPaddingWidth( Rectangle rect )
		{
			return (rect.X == 0) ? 0 : Settings.Instance.Padding.Width;
		}


		static int GetPaddingHeight( Rectangle rect )
		{
			return (rect.Y == 0) ? 0 : Settings.Instance.Padding.Height;
		}


		static bool ImageFitsInRect( Rectangle rect, Bitmap bitmap, out bool isRotated )
		{
			if ( rect.Width >= bitmap.Width + GetPaddingWidth( rect ) && rect.Height >= bitmap.Height + GetPaddingHeight( rect ) )
			{
				isRotated = false;
				return true;
			}
			else if ( Settings.Instance.AllowRotation && rect.Width >= bitmap.Height + GetPaddingWidth( rect ) && rect.Height >= bitmap.Width + GetPaddingHeight( rect ) )
			{
				isRotated = true;
				return true;
			}
			else
			{
				isRotated = false;
				return false;
			}
		}


		static bool InsertImage( InputBitmap inputImage, OutputBitmap ob )
		{
			foreach ( Rectangle rect in ob.AvailableRects )
			{
				bool isRotated = false;
				if ( ImageFitsInRect( rect, inputImage.mBitmap, out isRotated ) )
				{
					Size paddingSize = new Size( GetPaddingWidth( rect ), GetPaddingHeight( rect ) );
					Point locationIncludingPadding = new Point( rect.X + paddingSize.Width, rect.Y + paddingSize.Height );
					// copy image data
					ob.CopyImageIntoImage( inputImage.mBitmap, locationIncludingPadding, isRotated );

					// remove old rect, add 2 new ones
					// when storing a square inside a larger square, will always leave 2 other rectangles, one on the bottom
					// and one on the right. These rects are added to a list and then used to see if any future input images can fit
					ob.AvailableRects.Remove( rect );
					Size subImageSize = isRotated ? new Size( inputImage.mBitmap.Height, inputImage.mBitmap.Width ) : inputImage.mBitmap.Size;
					SplitRect( ob, rect, subImageSize + paddingSize );

					ob.SubImages.Add( new SubImage( inputImage.mName, Util.FormatSubImageName( inputImage.mName ), new Rectangle( locationIncludingPadding, subImageSize ), isRotated ) );

					ob.SortRectList();

					return true;
				}
			}

			return false;
		}


		static void AddAvailableRect( OutputBitmap ob, Rectangle rect )
		{
			if ( Util.IsValidRect( rect ) )
			{
				ob.AvailableRects.Add( rect );
			}
		}


		static void SplitRect( OutputBitmap ob, Rectangle rect, Size newImageSize )
		{
			// Split area into 2 rects, calculate which will generate the largest possible child.

			Rectangle rightRectLarge = new Rectangle();
			rightRectLarge.X = rect.X + newImageSize.Width;
			rightRectLarge.Y = rect.Y;
			rightRectLarge.Width = rect.Width - newImageSize.Width;
			rightRectLarge.Height = rect.Height;

			Rectangle bottomRectSmall = new Rectangle();
			bottomRectSmall.X = rect.X;
			bottomRectSmall.Y = rect.Y + newImageSize.Height;
			bottomRectSmall.Width = rect.Width - rightRectLarge.Width;
			bottomRectSmall.Height = rect.Height - newImageSize.Height;

			Rectangle bottomRectLarge = new Rectangle();
			bottomRectLarge.X = rect.X;
			bottomRectLarge.Y = rect.Y + newImageSize.Height;
			bottomRectLarge.Width = rect.Width;
			bottomRectLarge.Height = rect.Height - newImageSize.Height;

			Rectangle rightRectSmall = new Rectangle();
			rightRectSmall.X = rect.X + newImageSize.Width;
			rightRectSmall.Y = rect.Y;
			rightRectSmall.Width = rect.Width - newImageSize.Width;
			rightRectSmall.Height = rect.Height - bottomRectLarge.Height;

			int rightRectLargeArea = rightRectLarge.Width * rightRectLarge.Height;
			int bottomRectSmallArea = bottomRectSmall.Width * bottomRectSmall.Height;
			int rightRectSmallArea = rightRectSmall.Width * rightRectSmall.Height;
			int bottomRectLargeArea = bottomRectLarge.Width * bottomRectLarge.Height;

			if ( rightRectLargeArea > bottomRectLargeArea || bottomRectSmallArea > rightRectSmallArea
				|| rightRectLargeArea > rightRectSmallArea || bottomRectSmallArea > bottomRectLargeArea )
			{
				AddAvailableRect( ob, rightRectLarge );
				AddAvailableRect( ob, bottomRectSmall );
			}
			else
			{
				AddAvailableRect( ob, bottomRectLarge );
				AddAvailableRect( ob, rightRectSmall );
			}
		}


		static bool AddSubImageIfPossible( InputBitmap ib, OutputBitmap ob, bool allowImageResize )
		{
			if ( ob.SingleImageOutput )
				return false;

			// attempt to fill a small power of 2 texture first, then expand it gradually up to the maximum size
			// if the sub images don't fit. mVirtualSize is the size of the current rect we are trying to fill
			if ( ob.AvailableRects.Count == 0 )
			{
				// first time this output bitmap has been used, try using the smallest possible virtual texture size
				ob.InitVirtualSize( ib.mBitmap.Size );
			}

			if ( allowImageResize )
			{
				while ( !InsertImage( ib, ob ) )
				{
					// no images could be inserted in current image using current virtual size.
					// increase virtual size and try again
					if ( !ob.IncreaseVirtualSize() )
						return false; // cannot fit anymore images into this output image
				}
				return true;
			}
			else
				return InsertImage( ib, ob );
		}


		static bool AddSubImageIfPossible( InputBitmap inputBitmap, List<OutputBitmap> outputBitmaps )
		{
			// Attempt to fit in to existing rects without resizing image first
			foreach ( OutputBitmap ob in outputBitmaps )
			{
				if ( AddSubImageIfPossible( inputBitmap, ob, false ) )
					return true;
			}

			// then try with resizing
			foreach ( OutputBitmap ob in outputBitmaps )
			{
				if ( AddSubImageIfPossible( inputBitmap, ob, true ) )
					return true;
			}
			return false;
		}


		static void SortBySize( List<InputBitmap> output )
		{
			output.Sort(
				delegate( InputBitmap lhs, InputBitmap rhs )
				{
					if ( lhs != null && rhs != null )
					{
						int lhsArea = lhs.mBitmap.Width * lhs.mBitmap.Height;
						int rhsArea = rhs.mBitmap.Width * rhs.mBitmap.Height;
						if ( lhsArea == rhsArea )
							return 0;
						else if ( lhsArea > rhsArea )
							return -1;
						else
							return 1;
					}
					else if ( lhs == null )
						return -1;
					else if ( rhs == null )
						return 1;
					else
						return 0;
				}
			);
		}


		static void RemoveInvalidInputBitmaps( List<InputBitmap> inputBitmaps )
		{
			//List<InputBitmap> removeThese = new List<InputBitmap>();
			//foreach ( InputBitmap ib in inputBitmaps )
			//{
			//    // output warning if input image is too big for largest size of output bitmap
			//    if ( !OutputBitmap.IsInputImageValidSize( ib.mBitmap ) )
			//    {
			//        Console.WriteLine( "Image " + ib.mName + " (" + ib.mBitmap.Width + "x" + ib.mBitmap.Height + ") larger than maximum size ("
			//            + Settings.Instance.OutputBitmapSize.Width + "x"
			//            + Settings.Instance.OutputBitmapSize.Height + ")" );
			//        removeThese.Add( ib );
			//    }
			//}

			//foreach ( InputBitmap ib in removeThese )
			//{
			//    inputBitmaps.Remove( ib );
			//}
		}


		public static List<OutputBitmap> BreakIntoOutputBitmaps( List<InputBitmap> inputBitmaps, string outputName )
		{
			RemoveInvalidInputBitmaps( inputBitmaps );
			SortBySize( inputBitmaps );

			List<OutputBitmap> outputBitmaps = new List<OutputBitmap>();
			while ( inputBitmaps.Count > 0 )
			{
				InputBitmap inputImage = inputBitmaps[ 0 ];
				if ( OutputBitmap.IsInputImageValidSize( inputBitmaps[ 0 ].mBitmap ) )
				{
					bool imageHasBeenAdded = AddSubImageIfPossible( inputBitmaps[ 0 ], outputBitmaps );

					// add a new output bitmap if an existing space can't be found
					if ( !imageHasBeenAdded )
					{
						OutputBitmap newOutput = new OutputBitmap( outputName, outputBitmaps.Count );
						outputBitmaps.Add( newOutput );
					}
					else
						inputBitmaps.Remove( inputBitmaps[ 0 ] );
				}
				else
				{
					// if larger than target atlas size, just copy the input image over and add it to the page list
					OutputBitmap newOutput = new OutputBitmap( outputName, outputBitmaps.Count, inputBitmaps[0].mBitmap.Width, inputBitmaps[0].mBitmap.Height );
					newOutput.CopyImageIntoImage( inputBitmaps[ 0 ].mBitmap, new Point( 0, 0 ), false );
					newOutput.SubImages.Add( new SubImage( inputImage.mName, Util.FormatSubImageName( inputImage.mName ), new Rectangle( new Point( 0, 0 ), inputImage.mBitmap.Size ), false ) );
					outputBitmaps.Add( newOutput );
					inputBitmaps.Remove( inputBitmaps[ 0 ] );
				}
			}
			return outputBitmaps;
		}

	}
}
