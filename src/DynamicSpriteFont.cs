using FontStashSharp;
using HarfBuzzSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using SpriteFontPlus.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SpriteFontPlus
{
	static class SpriteFontPlusExtensions
	{
		public static void DrawShapedText2(this SKCanvas canvas, SKShaper shaper, SKShaper shaper2, string text, float x, float y, SKPaint paint, SKFont paintfont, HarfBuzzSharp.Font hbFont1, HarfBuzzSharp.Font hbFont2)
		{
			if (string.IsNullOrEmpty(text))
				return;

			if (canvas == null)
				throw new ArgumentNullException(nameof(canvas));
			if (shaper == null)
				throw new ArgumentNullException(nameof(shaper));
			if (paint == null)
				throw new ArgumentNullException(nameof(paint));

			List<(string, bool)> chunks1 = new List<(string, bool)>();

			int startidx = 0;
            {
				var str = text;
				bool inside = true;
				for (int i = 0; i < str.Length;)
				{
					var codepoint = char.ConvertToUtf32(str, i);

					var glyph = shaper.Typeface.GetGlyph(codepoint);
					if (glyph == 0 && inside)
					{
						if (i - startidx - 1 > 0)
						{
							chunks1.Add((str.Substring(startidx, i - startidx - 1), inside));
						}

						inside = false;
						startidx = i;
					}
					else if(glyph != 0 && !inside)
                    {
						if (i - startidx - 1 > 0)
						{
							chunks1.Add((str.Substring(startidx, i - startidx - 1), inside));
						}

						inside = true;
						startidx = i;
					}

					i += char.IsSurrogatePair(str, i) ? 2 : 1;
					
					if (i == str.Length)
					{
						if (i - startidx > 0)
						{
							chunks1.Add((str.Substring(startidx, i - startidx), inside));
						}
					}
				}
			}

			foreach (var chunk in chunks1)
			{

				//using (var font = paint.ToFont())
				HarfBuzzSharp.Font hbFont;

				{
					// shape the text
					SKShaper.Result result;

					if (chunk.Item2)
					{
						result = shaper.Shape(chunk.Item1, x, y, paint);
						paintfont.Typeface = shaper.Typeface;
						hbFont = hbFont1;
					}
					else
					{
						if (shaper2 != null)
						{
							result = shaper2.Shape(chunk.Item1, x, y, paint);
							paintfont.Typeface = shaper2.Typeface;
							hbFont = hbFont2;
						}
						else
						{
							//this will render boxes of unknown for sure
							result = shaper.Shape(chunk.Item1, x, y, paint);
							paintfont.Typeface = shaper.Typeface;
							hbFont = hbFont1;
						}
					}


					// create the text blob
					using (var builder = new SKTextBlobBuilder())
					{
						var run = builder.AllocatePositionedRun(paintfont, result.Codepoints.Length);

						// copy the glyphs
						var g = run.GetGlyphSpan();
						var p = run.GetPositionSpan();

						for (var i = 0; i < result.Codepoints.Length; i++)
						{
							g[i] = (ushort)result.Codepoints[i];
							p[i] = result.Points[i];
						}

						// build
						using (var textBlob = builder.Build())
						{

							// draw the text
							canvas.DrawText(textBlob, 0, 0, paint);
						}
					}



					x += (float)MeasureShapedText(chunk.Item1, paintfont.Size, paintfont.Typeface, hbFont);
				}

			}
		}

		public static double MeasureShapedText(string text, double fontsize, SKTypeface typeface, HarfBuzzSharp. Font hbFont)
		{
			//if (paint == null)
			//	throw new ArgumentNullException(nameof(paint));

			//double fontsize = paint.TextSize;

			//using (var blob = typeface.OpenStream().ToHarfBuzzBlob())
			{
				//using (var hbFace = new Face(blob, 0))
				{
					//using (var hbFont = new HarfBuzzSharp.Font(hbFace))
                    {
						using (var buffer = new HarfBuzzSharp.Buffer())
                        {
							buffer.AddUtf16(text);
							buffer.GuessSegmentProperties();
							hbFont.Shape(buffer);
							hbFont.GetScale(out var xScale, out _);

							//todo measure multi line y

							var scale = fontsize / xScale;
							return buffer.GlyphPositions.Sum(x => x.XAdvance) * scale;
                        }
                    }
				}
			}

			/*
			using (var font = paint.ToFont())
			{
				font.Typeface = shaper.Typeface;

				// shape the text
				var result = shaper.Shape(text, x, y, paint);

				// create the text blob
				using (var builder = new SKTextBlobBuilder())
				{
					var run = builder.AllocatePositionedRun(font, result.Codepoints.Length);

					// copy the glyphs
					var g = run.GetGlyphSpan();
					var p = run.GetPositionSpan();
					for (var i = 0; i < result.Codepoints.Length; i++)
					{
						g[i] = (ushort)result.Codepoints[i];
						p[i] = result.Points[i];
					}

					// build
					using (var textBlob = builder.Build())
					{

						// draw the text
						canvas.DrawText(textBlob, 0, 0, paint);
					}
				}
			}
			*/
		}

		internal static double MeasureShapedText2(string text, float size, SKTypeface typeface, SKTypeface typeface2, HarfBuzzSharp.Font hbFont1, HarfBuzzSharp.Font hbFont2)
		{
			List<(string, bool)> chunks1 = new List<(string, bool)>();

			int startidx = 0;
			{
				var str = text;
				bool inside = true;
				for (int i = 0; i < str.Length;)
				{
					var codepoint = char.ConvertToUtf32(str, i);

					var glyph = typeface.GetGlyph(codepoint);
					if (glyph == 0 && inside)
					{
						if (i - startidx - 1 > 0)
						{
							chunks1.Add((str.Substring(startidx, i - startidx - 1), inside));
						}

						inside = false;
						startidx = i;
					}
					else if (glyph != 0 && !inside)
					{
						if (i - startidx - 1 > 0)
						{
							chunks1.Add((str.Substring(startidx, i - startidx - 1), inside));
						}

						inside = true;
						startidx = i;
					}

					i += char.IsSurrogatePair(str, i) ? 2 : 1;

					if (i == str.Length)
					{
						if (i - startidx > 0)
						{
							chunks1.Add((str.Substring(startidx, i - startidx), inside));
						}
					}
				}
			}


			var result = 0.0;

			foreach (var chunk in chunks1)
			{
				if (chunk.Item2)
				{
					result += MeasureShapedText(chunk.Item1, size, typeface, hbFont1);
				}
				else
				{
					if (typeface2 != null)
					{
						result += MeasureShapedText(chunk.Item1, size, typeface2, hbFont2);
					}
					else
					{
						//this will render boxes of unknown for sure
						result += MeasureShapedText(chunk.Item1, size, typeface, hbFont1);
					}
				}
			}

			return result;
		}


		public static string GetHashSHA1(this byte[] data)
		{
			using (var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider())
			{
				return string.Concat(sha1.ComputeHash(data).Select(x => x.ToString("X2")));
			}
		}
	}

	class GlyphCollection
	{
		internal readonly Int32Map<FontGlyph> Glyphs = new Int32Map<FontGlyph>();
	}

	public class Glyph
    {
		public FontSk Font;
		public int codepoint;
		public ushort glyph;
    }
    public class FontSk
    {
		public SKTypeface Typeface;
		public SKFont Font;
		public SKShaper Shaper;
		public HarfBuzzSharp.Font HBFont;

        /*public double Height { get
			{
				return Font.Size;
			}
		}*/

        public static FontSk FromMemory(byte[] data)
        {
			var font = new FontSk();

			using (var m = new MemoryStream(data))
			{
				font.Typeface = SKTypeface.FromStream(m);
				font.Font = font.Typeface.ToFont();
				font.Shaper = new SKShaper(font.Typeface);
				using (var blob = font.Typeface.OpenStream().ToHarfBuzzBlob())
				{
					using (var hbFace = new Face(blob, 0))
					{
						font.HBFont = new HarfBuzzSharp.Font(hbFace);
					}
				}
			}

			return font;
		}

        internal double GetDescent()
        {
			return Font.Metrics.Descent;
        }

        internal double GetAscent()
        {
			return Font.Metrics.Ascent;
        }

        internal double GetLineHeightBasic()
        {
			return Font.Size; //TODO dunno if that is right
        }
    }

	public class FontSystemSk
	{
		public static Dictionary<string, FontSk> SysFonts = new Dictionary<string, FontSk>();

		public List<string> Fonts = new List<string>();

		public readonly int BlurAmount;
		public readonly int StrokeAmount;
		public float Spacing;
		public float LineSpacing = 0f;
		public Vector2 Scale;
		public bool UseKernings = true;
		public float FontSize;



		OperatingSystem _platform = Environment.OSVersion;

		public float PlatFormFontSize
		{
			get
			{
				if (_platform.Platform == PlatformID.Unix)
				{
					return FontSize * 72.0f / 96.0f;
				}
				return FontSize;
			}
		}

		public static SKBitmap ScratchDrawing = null;
		public static byte[] ScratchImageBuffer = null;
		public static Texture2D ScratchTexture = null;

		public int? DefaultCharacter = ' ';

		public void AddFontMem(byte[] data)
		{

			var id = data.GetHashSHA1();

			if (!SysFonts.ContainsKey(id))
			{
				var font = FontSk.FromMemory(data);

				SysFonts[id] = font;
			}

			//font.Recalculate(FontSize);
			Fonts.Add(id);
		}

		public FontSystemSk()
		{
			if (ScratchDrawing == null)
			{
				ScratchDrawing = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
			}

			_paint = new SKPaint();
			_paint.Color = SKColors.White;
			_paint.IsStroke = false;
			_paint.IsAntialias = true;
			_paint.LcdRenderText = true;
			_paintfont = _paint.ToFont();
		}

		public FontSk GetFont(int val)
		{
			return SysFonts[Fonts[val]];
        }

		SKPaint _paint;
		SKFont _paintfont;

		internal double DrawText(SpriteBatch batch, float x, float y, string text, Color color, float depth, bool bottomleft = false)
		{
			//draw using font one until there is a glyph that doesn't exist in font one, then use font 2... etc

			var entry = GetFont(0);

			var shaper = entry.Shaper;

			var entry2 = Fonts.Count > 1 ? GetFont(1) : null;

			var shaper2 = entry2?.Shaper;

			var hbfont = entry.HBFont;

			var hbfont2 = entry2?.HBFont;


			entry.Font.Size = FontSize;
			if (entry2 != null)
			{
				entry2.Font.Size = FontSize;
			}
			//points = pixels * 72 / 96
			_paint.TextSize = FontSize;
			//_paint.TextSize = FontSize * 72.0f / 96.0f;

			_paint.IsAntialias = true;

			_paint.LcdRenderText = true;

			_paintfont.Size = FontSize;
			//_paintfont.Size = FontSize * 72.0f / 96.0f;


			var dimx = SpriteFontPlusExtensions.MeasureShapedText2(text, FontSize, entry.Typeface, entry2?.Typeface, hbfont, hbfont2);
			var dimy = FontSize;
			var top = Math.Min(entry.Font.Metrics.Top, entry2 == null ? 0 : entry2.Font.Metrics.Top);
			var bottom = Math.Max(entry.Font.Metrics.Bottom, entry2 == null ? 0 : entry2.Font.Metrics.Bottom);

			if (ScratchDrawing.Width < dimx || ScratchDrawing.Height < bottom - top)
			{
				ScratchDrawing = new SKBitmap((int)dimx + 50, (int)dimy + 50, SKColorType.Rgba8888, SKAlphaType.Premul);
			}

			UpdateScratchTexture(batch);

			using (var canvas = new SKCanvas(ScratchDrawing))
			{
				canvas.Clear(SKColors.Transparent);
				canvas.DrawShapedText2(shaper, shaper2, text, 0, -top, _paint, _paintfont, hbfont, hbfont2);
			}

			var spanb = ScratchDrawing.GetPixelSpan();

			spanb.CopyTo(ScratchImageBuffer);

			ScratchTexture.SetData(ScratchImageBuffer);

			//y should be baseline

			//baseline in source is drawn at entry.Font.Metrics.Bottom instead of entry.Font.Metrics.Descent

			var desty = y;
			if (bottomleft)
            {
				desty = y - entry.Font.Metrics.Descent;
            }

			var destRect = new Rectangle((int)Math.Round(x),
											(int)Math.Round(desty),
											(int)Math.Round(dimx),
											(int)Math.Round(bottom - top));

			var sourceRect = new Rectangle((int)(0),
										(int)(0),
										(int)(dimx),
										(int)(bottom - top));

			batch.Draw(ScratchTexture,
				destRect,
				sourceRect,
				color,
				0f,
				Vector2.Zero,
				SpriteEffects.None,
				depth);

			batch.End();
			batch.Begin();

			return dimx;
		}

        private void UpdateScratchTexture(SpriteBatch batch)
        {
            if (ScratchTexture == null || ScratchTexture.Width != ScratchDrawing.Width || ScratchTexture.Height != ScratchDrawing.Height)
            {
				ScratchTexture = new Texture2D(batch.GraphicsDevice, ScratchDrawing.Width, ScratchDrawing.Height);
				ScratchImageBuffer = new byte[ScratchDrawing.Width * ScratchDrawing.Height * 4];
            }
        }

        internal double DrawText(SpriteBatch batch, float x, float y, StringBuilder text, Color[] glyphColors, float depth)
        {
            throw new NotImplementedException();
        }

        internal List<Rectangle> GetGlyphRectsFull(float x, float y, string text)
        {
			//TODO update this for dual font
			List<Rectangle> result = new List<Rectangle>();

			var entry = GetFont(0);

			var paint = new SKPaint();

			paint.Typeface = entry.Typeface;

			paint.TextSize = FontSize;
			//paint.TextSize = FontSize * 72.0f / 96.0f;

			double fontsize = paint.TextSize;

			using (var blob = paint.Typeface.OpenStream().ToHarfBuzzBlob())
			{
				using (var hbFace = new Face(blob, 0))
				{
					using (var hbFont = new HarfBuzzSharp.Font(hbFace))
					{
						using (var buffer = new HarfBuzzSharp.Buffer())
						{
							buffer.AddUtf16(text);
							buffer.GuessSegmentProperties();
							hbFont.Shape(buffer);
							hbFont.GetScale(out var xScale, out _);

							//todo measure multi line y

							var scale = fontsize / xScale;
							//return buffer.GlyphPositions.Sum(x => x.XAdvance) * scale;


							foreach (var pos in buffer.GlyphPositions)
                            {
								result.Add(new Rectangle((int)x, (int)y, pos.XAdvance, (int)paint.TextSize));
								x += pos.XAdvance;
								y += pos.YAdvance;
                            }

						}
					}
				}
			}

			return result;
		}

        internal void TextBounds(float x, float y, string text, ref Bounds bounds)
        {
			var entry = GetFont(0);
			var entry1 = Fonts.Count > 1 ? GetFont(1) : null;

			//var paint = new SKPaint();

			//paint.Typeface = entry.Typeface;

			//paint.TextSize = entry.Font.Size;

			entry.Font.Size = FontSize;
			//entry.Font.Size = FontSize * 72.0f / 96.0f;


			var visiblebottom = Math.Max(Math.Max(0, entry.Font.Metrics.Descent), 0); //, entry1 == null ? 0 : entry1.Font.Metrics.Bottom);
			var visibletop = Math.Min(Math.Min(0, entry.Font.Metrics.Ascent), 0); //, entry1 == null ? 0 : entry1.Font.Metrics.Top);

			var dimx = SpriteFontPlusExtensions.MeasureShapedText2(text, FontSize, entry.Typeface, entry1?.Typeface, entry.HBFont, entry1?.HBFont);
			//var dimy = visibleheight;//FontSize * 96.0 / 72.0; //TODO MULTIPLE LINES?... //entry.Font.Metrics.Descent - entry.Font.Metrics.Ascent; //TODO measure font two, if needed...

			bounds.X = x;
			bounds.Y = y + visibletop;
			bounds.X2 = x + dimx;
			bounds.Y2 = y + visiblebottom;
		}

		private void PreDraw(string str, out double ascent, out double lineHeight)
		{
			//glyphs = GetGlyphsCollection(FontSize);

			// Determine ascent and lineHeight from first character
			ascent = 0;
			lineHeight = 0;

			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);

				var glyph = GetGlyph(codepoint);
				if (glyph == null)
				{
					continue;
				}

				glyph.Font.Font.Size = FontSize;
				glyph.Font.Font.Size = FontSize * 72.0f / 96.0f;
				var tascent = glyph.Font.Font.Metrics.Ascent;
				var tlineheight = FontSize + LineSpacing;

				if (tascent > ascent)
                {
					ascent = tascent;
                }
				if (tlineheight > lineHeight)
                {
					lineHeight = tlineheight;
                }
			}
		}

        private Glyph GetGlyph(int codepoint)
        {
            foreach (var entryid in Fonts)
            {
				var entry = SysFonts[entryid];
				var g = entry.Typeface.GetGlyph(codepoint);
				if (g != 0)
                {
					return new Glyph()
					{
						Font = entry, codepoint = codepoint, glyph = g
					};
                }
            }

			return null;
        }

		private void PreDraw2(string str, out GlyphCollection glyphcollection, out double ascent, out double lineHeight, out double descent, out double lineHeightBasic)
		{
			glyphcollection = new GlyphCollection();

			// Determine ascent and lineHeight from first character
			ascent = 0;
			lineHeight = 0;
			descent = 0;
			lineHeightBasic = 0;

			var topmax = 0.0;
			var bottommax = 0.0;

			if (str == null)
            {
				foreach (var entryid in Fonts)
				{
					var entry = SysFonts[entryid];
					entry.Font.Size = FontSize;
					//entry.Font.Size = FontSize * 72.0f / 96.0f;
					var lineHeightBasic2 = entry.Font.Metrics.Descent - entry.Font.Metrics.Ascent;
					lineHeightBasic = Math.Max(lineHeightBasic, lineHeightBasic2);
					topmax = Math.Min(topmax, entry.Font.Metrics.Top);
					bottommax = Math.Max(bottommax, entry.Font.Metrics.Bottom);
					lineHeight = Math.Max(lineHeight, bottommax - topmax);//Math.Max(FontSize + LineSpacing, lineHeight); // lineHeightBasic + LineSpacing;
				}
				return;
			}

			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);

				var glyph = GetGlyph(codepoint);
				if (glyph == null)
				{
					continue;
				}

				glyph.Font.Font.Size = FontSize;
				//glyph.Font.Font.Size = FontSize * 72.0f / 96.0f;
				ascent = glyph.Font.Font.Metrics.Ascent;
				descent = glyph.Font.Font.Metrics.Descent;
				lineHeightBasic = FontSize; // LineHeight;
				lineHeight = lineHeightBasic + LineSpacing;
				var lineHeightBasic2 = glyph.Font.Font.Metrics.Descent - glyph.Font.Font.Metrics.Ascent;
				lineHeightBasic = Math.Max(lineHeightBasic, lineHeightBasic2);
				lineHeight = Math.Max(FontSize + LineSpacing, lineHeight);
				break;
			}
		}

		internal double GetDescent(string str)
		{
			GlyphCollection glyphs;
			double ascent, lineHeight, descent, lineHeightBasic;
			PreDraw2(str, out glyphs, out ascent, out lineHeight, out descent, out lineHeightBasic);
			return descent;
		}

		internal double GetFullHeight()
		{
			GlyphCollection glyphs;
			double ascent, lineHeight, descent, lineHeightBasic;
			PreDraw2(null, out glyphs, out ascent, out lineHeight, out descent, out lineHeightBasic);
			return lineHeight;
		}

		/*internal double GetVisibleHeight()
		{
			GlyphCollection glyphs;
			double ascent, lineHeight, descent, lineHeightBasic, visibleheight;
			visibleheight = 0;
			PreDraw2(null, out glyphs, out ascent, out lineHeight, out descent, out lineHeightBasic, visibleheight);
			return lineHeight;
		}*/

		internal double GetAscent(string str)
		{
			GlyphCollection glyphs;
			double ascent, lineHeight, descent, lineHeightBasic;
			PreDraw2(str, out glyphs, out ascent, out lineHeight, out descent, out lineHeightBasic);
			return ascent;
		}
		internal double GetLineHeightBasic()
		{
			GlyphCollection glyphs;
			double ascent, lineHeight, descent, lineHeightBasic;
			PreDraw2(null, out glyphs, out ascent, out lineHeight, out descent, out lineHeightBasic);
			return lineHeightBasic;
		}

		internal double GetLineHeightBasic(string str)
		{
			GlyphCollection glyphs;
			double ascent, lineHeight, descent, lineHeightBasic;
			PreDraw2(str, out glyphs, out ascent, out lineHeight, out descent, out lineHeightBasic);
			return lineHeightBasic;
		}
	}

	public class DynamicSpriteFont : IDisposable
	{
		internal struct TextureEnumerator : IEnumerable<Texture2D>
		{
			readonly FontSystem _font;

			public TextureEnumerator(FontSystem font)
			{
				_font = font;
			}

			public IEnumerator<Texture2D> GetEnumerator()
			{
				foreach (var atlas in _font.Atlases)
				{
					yield return atlas.Texture;
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		readonly FontSystemSk _fontSystem;

		//public IEnumerable<Texture2D> Textures
		//{
		//	get { return new TextureEnumerator(_fontSystem); }
		//}

		public float Size
		{
			get { return _fontSystem.FontSize; }
			set { _fontSystem.FontSize = value; }
		}

		/*
		public float Size2
		{ 
			get { return (float) (Size * 72.0 / 96.0); }
		}
		*/

		public float Spacing
		{
			get { return _fontSystem.Spacing; }
			set { _fontSystem.Spacing = value; }
		}

		public bool UseKernings
		{
			get { return _fontSystem.UseKernings; }

			set { _fontSystem.UseKernings = value; }
		}

		public int? DefaultCharacter
		{
			get { return _fontSystem.DefaultCharacter; }

			set { _fontSystem.DefaultCharacter = value; }
		}

		DynamicSpriteFont(byte[] ttf, double defaultSize, int textureWidth, int textureHeight, int blur, int stroke)
		{
			_fontSystem = new FontSystemSk();

			_fontSystem.AddFontMem(ttf);

			_fontSystem.FontSize = (float)defaultSize;
		}

		public void Dispose()
		{
			//_fontSystem?.Dispose();
		}

		public double DrawString(SpriteBatch batch, string text, Vector2 pos, Color color, bool bottomleft = false)
		{
			return DrawString(batch, text, pos, color, Vector2.One, bottomleft: bottomleft);
		}

		public double DrawString(SpriteBatch batch, string text, Vector2 pos, Color color, Vector2 scale, float depth = 0f, bool bottomleft = false)
		{
			_fontSystem.Scale = scale;

			var result = _fontSystem.DrawText(batch, pos.X, pos.Y, text, color, depth, bottomleft);

			_fontSystem.Scale = Vector2.One;

			return result;
		}

		public double GetAscent(string str)
		{
			return _fontSystem.GetAscent(str);
		}

		public double GetDescent(string str)
		{
			return _fontSystem.GetDescent(str);
		}

		public double GetFullHeight()
        {
			return _fontSystem.GetFullHeight();
        }

		/*public double GetVisibleHeight()
		{
			return _fontSystem.GetVisibleHeight();
		}*/

		public double GetLineHeightBasic()
		{
			return _fontSystem.GetLineHeightBasic();
		}

		public double GetLineHeightBasic(string str)
		{
			return _fontSystem.GetLineHeightBasic(str);
		}

		public double DrawString(SpriteBatch batch, StringBuilder text, Vector2 pos, Color[] glyphColors, Vector2 scale, float depth = 0f)
		{
			_fontSystem.Scale = scale;

			var result = _fontSystem.DrawText(batch, pos.X, pos.Y, text, glyphColors, depth);

			_fontSystem.Scale = Vector2.One;

			return result;
		}

		public void AddTtf(byte[] ttf)
		{
			_fontSystem.AddFontMem(ttf);
		}

		public void AddTtf(Stream ttfStream)
		{
			AddTtf(ttfStream.ToByteArray());
		}

		public Vector2 MeasureString(string text)
		{
			Bounds bounds = new Bounds();
			_fontSystem.TextBounds(0, 0, text, ref bounds);

			return new Vector2((float)(bounds.X2 - bounds.X), (float)(bounds.Y2 - bounds.Y));
		}

		public List<Rectangle> GetGlyphRectsFull(Vector2 position, string text) {
			return _fontSystem.GetGlyphRectsFull(position.X, position.Y, text);
		}

		public static DynamicSpriteFont FromTtf(byte[] ttf, double defaultSize, int textureWidth = 1024, int textureHeight = 1024, int blur = 0, int stroke = 0)
		{
			return new DynamicSpriteFont(ttf, defaultSize, textureWidth, textureHeight, blur, stroke);
		}

		public static DynamicSpriteFont FromTtf(Stream ttfStream, double defaultSize, int textureWidth = 1024, int textureHeight = 1024, int blur = 0, int stroke = 0)
		{
			return FromTtf(ttfStream.ToByteArray(), defaultSize, textureWidth, textureHeight, blur, stroke);
		}
	}
}
