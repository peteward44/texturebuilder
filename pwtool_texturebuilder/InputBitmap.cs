using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;



namespace pwtool_texturebuilder
{
	public class InputBitmap
	{
		public InputBitmap( string name, Bitmap bm )
		{
			mName = name;
			mBitmap = bm;
		}

		public Bitmap mBitmap;
		public string mName;

		public override bool Equals( object obj )
		{
			if ( obj != null && obj is InputBitmap )
			{
				InputBitmap ib = (InputBitmap)obj;
				return ib.mName.Equals( this.mName );
			}
			return false;
		}

		public override int GetHashCode()
		{
			return mName.GetHashCode();
		}
	}
}
