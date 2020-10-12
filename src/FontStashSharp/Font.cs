using System;
using System.Runtime.InteropServices;
using static StbTrueTypeSharp.StbTrueType;

namespace FontStashSharp
{
	internal unsafe class Font: IDisposable
	{
		private GCHandle? dataPtr = null;
		private float AscentBase, DescentBase, LineHeightBase;
		readonly Int32Map<int> _kernings = new Int32Map<int>();

		public double Ascent { get; private set; }
		public double Descent { get; private set; }
		public double LineHeight { get; private set; }
		public double Scale { get; private set; }

		private double _sizeratio = 1.0;
		public double Ratio { get
			{
				return _sizeratio;
			}
			set
            {
				_sizeratio = value;
            } 
		}

		public stbtt_fontinfo _font = new stbtt_fontinfo();

		public Font(byte[] data)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			dataPtr = GCHandle.Alloc(data, GCHandleType.Pinned);
		}

		~Font()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (dataPtr != null)
			{
				dataPtr.Value.Free();
				dataPtr = null;
			}
		}

		public void Recalculate(float size)
		{
			Ascent = AscentBase * size * _sizeratio;
			Descent = DescentBase * size * _sizeratio;
			LineHeight = LineHeightBase * size * _sizeratio;
			Scale = stbtt_ScaleForPixelHeight(_font, (float)(size * _sizeratio));
		}

		public int GetGlyphIndex(int codepoint)
		{
			return stbtt_FindGlyphIndex(_font, codepoint);
		}

		public void BuildGlyphBitmap(int glyph, double scale, ref int advance, ref int lsb, ref int x0, ref int y0, ref int x1, ref int y1)
		{
			int advanceTemp, lsbTemp;
			stbtt_GetGlyphHMetrics(_font, glyph, &advanceTemp, &lsbTemp);
			advance = advanceTemp;
			lsb = lsbTemp;

			/* TODO I see, subpixel shifts make cashing to bitmap trickier, because a letter will appear different across each subpixel shift to a slight degree?
			 fread(buffer, 1, 1000000, fopen("c:/windows/fonts/arialbd.ttf", "rb"));
   stbtt_InitFont(&font, buffer, 0);

   scale = stbtt_ScaleForPixelHeight(&font, 15);
   stbtt_GetFontVMetrics(&font, &ascent,0,0);
   baseline = (int) (ascent*scale);

   while (text[ch]) {
      int advance,lsb,x0,y0,x1,y1;
      float x_shift = xpos - (float) floor(xpos);
      stbtt_GetCodepointHMetrics(&font, text[ch], &advance, &lsb);
      stbtt_GetCodepointBitmapBoxSubpixel(&font, text[ch], scale,scale,x_shift,0, &x0,&y0,&x1,&y1);
      stbtt_MakeCodepointBitmapSubpixel(&font, &screen[baseline + y0][(int) xpos + x0], x1-x0,y1-y0, 79, scale,scale,x_shift,0, text[ch]);
      // note that this stomps the old data, so where character boxes overlap (e.g. 'lj') it's wrong
      // because this API is really for baking character bitmaps into textures. if you want to render
      // a sequence of characters, you really need to render each bitmap to a temp buffer, then
      // "alpha blend" that into the working buffer
      xpos += (advance * scale);
      if (text[ch+1])
         xpos += scale*stbtt_GetCodepointKernAdvance(&font, text[ch],text[ch+1]);
      ++ch;
   } */

			float x0Temp, y0Temp; //to later allow shifting?
			int x1Temp, y1Temp, x2Temp, y2Temp;
			var tempscale = (float)scale;
			stbtt_GetGlyphBitmapBoxSubpixel(_font, glyph, tempscale, tempscale, 0, 0, &x1Temp, &y1Temp, &x2Temp, &y2Temp);
			x0 = x1Temp;
			y0 = y1Temp;
			x1 = x2Temp;
			y1 = y2Temp;
		}

		public void RenderGlyphBitmap(byte *output, int outWidth, int outHeight, int outStride, int glyph)
		{
			float scale = (float)Scale; //thinking...
			stbtt_MakeGlyphBitmap(_font, output, outWidth, outHeight, outStride, scale, scale, glyph);
		}

		public int GetGlyphKernAdvance(int glyph1, int glyph2)
		{
			/*var key = ((glyph1 << 16) | (glyph1 >> 16)) ^ glyph2;
			int result;
			if (_kernings.TryGetValue(key, out result))
			{
				return result;
			}*/
			int result = stbtt_GetGlyphKernAdvance(_font, glyph1, glyph2);
			//_kernings[key] = result;
			return result;
		}

		public static Font FromMemory(byte[] data)
		{
			var font = new Font(data);

			byte* dataPtr = (byte *)font.dataPtr.Value.AddrOfPinnedObject();
			if (stbtt_InitFont(font._font, dataPtr, 0) == 0)
				throw new Exception("stbtt_InitFont failed");

			int ascent, descent, lineGap;
			stbtt_GetFontVMetrics(font._font , &ascent, &descent, &lineGap);

			var fh = ascent - descent;
			font.AscentBase = ascent / (float)fh;
			font.DescentBase = descent / (float)fh;
			font.LineHeightBase = (fh + lineGap) / (float)fh;

			return font;
		}
	}
}
