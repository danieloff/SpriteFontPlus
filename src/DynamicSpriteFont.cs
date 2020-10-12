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
using System.Text;

namespace SpriteFontPlus
{
	static class SpriteFontPlusExtensions
	{
		public static double MeasureShapedText(string text, SKPaint paint)
		{
			if (paint == null)
				throw new ArgumentNullException(nameof(paint));

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

        public double Height { get
			{
				return Font.Metrics.Bottom - Font.Metrics.Top;
			}
		}

        public static FontSk FromMemory(byte[] data)
        {
			var font = new FontSk();

			using (var m = new MemoryStream(data))
			{
				font.Typeface = SKTypeface.FromStream(m);
				font.Font = font.Typeface.ToFont();
				font.Shaper = new SKShaper(font.Typeface);
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
		public List<FontSk> Fonts = new List<FontSk>();

		public readonly int BlurAmount;
		public readonly int StrokeAmount;
		public float Spacing;
		public float LineSpacing = 0f;
		public Vector2 Scale;
		public bool UseKernings = true;

		public SKBitmap ScratchDrawing;
		public byte[] ScratchImageBuffer;
		public Texture2D ScratchTexture;

		public int? DefaultCharacter = ' ';

		public void AddFontMem(byte[] data)
		{
			var font = FontSk.FromMemory(data);

			//font.Recalculate(FontSize);
			Fonts.Add(font);
		}

		public FontSystemSk()
        {
			ScratchDrawing = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
		}

		internal double DrawText(SpriteBatch batch, float x, float y, string text, Color color, float depth)
		{
			//draw using font one until there is a glyph that doesn't exist in font one, then use font 2... etc

			var entry = Fonts[0];

			var paint = new SKPaint();

			paint.Typeface = entry.Typeface;

			paint.TextSize = entry.Font.Size;

			paint.Color = SKColors.White;

			var dimx = SpriteFontPlusExtensions.MeasureShapedText(text, paint);
			var dimy = entry.Height;

			if (ScratchDrawing.Width < dimx || ScratchDrawing.Height < dimy)
			{
				ScratchDrawing = new SKBitmap((int)dimx + 50, (int)dimy + 50, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			}

			UpdateScratchTexture(batch);

			var shaper = entry.Shaper;

			using (var canvas = new SKCanvas(ScratchDrawing))
			{
				canvas.Clear(SKColors.Transparent);
				canvas.DrawShapedText(shaper, text, new SKPoint(0, -entry.Font.Metrics.Ascent), paint);
			}

			var spanb = ScratchDrawing.GetPixelSpan();

			spanb.CopyTo(ScratchImageBuffer);

				ScratchTexture.SetData(ScratchImageBuffer);

			var destRect = new Rectangle((int)Math.Round(x),
											(int)Math.Round(y),
											(int)Math.Round(dimx),
											(int)Math.Round(dimy));

			var sourceRect = new Rectangle((int)(0),
										(int)(0),
										(int)(dimx),
										(int)(dimy));

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
			List<Rectangle> result = new List<Rectangle>();

			var entry = Fonts[0];

			var paint = new SKPaint();

			paint.Typeface = entry.Typeface;

			paint.TextSize = entry.Font.Size;

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
			var entry = Fonts[0];

			var paint = new SKPaint();

			paint.Typeface = entry.Typeface;

			paint.TextSize = entry.Font.Size;

			var dimx = SpriteFontPlusExtensions.MeasureShapedText(text, paint);
			var dimy = paint.TextSize;

			bounds.X = x;
			bounds.Y = y;
			bounds.X2 = x + dimx;
			bounds.Y2 = y + dimy;
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

				var tascent = glyph.Font.Font.Metrics.Ascent;
				var tlineheight = glyph.Font.Height + LineSpacing;

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
            foreach (var entry in Fonts)
            {
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
			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);

				var glyph = GetGlyph(codepoint);
				if (glyph == null)
				{
					continue;
				}

				ascent = glyph.Font.Font.Metrics.Ascent;
				descent = glyph.Font.Font.Metrics.Descent;
				lineHeightBasic = glyph.Font.Height; // LineHeight;
				lineHeight = glyph.Font.Height + LineSpacing;
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

		internal double GetAscent(string str)
		{
			GlyphCollection glyphs;
			double ascent, lineHeight, descent, lineHeightBasic;
			PreDraw2(str, out glyphs, out ascent, out lineHeight, out descent, out lineHeightBasic);
			return ascent;
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

		public int Size
		{
			//TODO remove int
			get { return (int)_fontSystem.Fonts[0].Font.Size; }
			set { _fontSystem.Fonts[0].Font.Size = value; }
		}

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

		DynamicSpriteFont(byte[] ttf, int defaultSize, int textureWidth, int textureHeight, int blur, int stroke)
		{
			_fontSystem = new FontSystemSk();

			_fontSystem.AddFontMem(ttf);

			_fontSystem.Fonts[_fontSystem.Fonts.Count - 1].Font.Size = defaultSize;
		}

		public void Dispose()
		{
			//_fontSystem?.Dispose();
		}

		public double DrawString(SpriteBatch batch, string text, Vector2 pos, Color color)
		{
			return DrawString(batch, text, pos, color, Vector2.One);
		}

		public double DrawString(SpriteBatch batch, string text, Vector2 pos, Color color, Vector2 scale, float depth = 0f)
		{
			_fontSystem.Scale = scale;

			var result = _fontSystem.DrawText(batch, pos.X, pos.Y, text, color, depth);

			_fontSystem.Scale = Vector2.One;

			return result;
		}

		/*
		public double DrawString(SpriteBatch batch, string text, Vector2 pos, Color[] glyphColors)
		{
			return DrawString(batch, text, pos, glyphColors, Vector2.One);
		}

		public double DrawString(SpriteBatch batch, string text, Vector2 pos, Color[] glyphColors, Vector2 scale, float depth = 0f)
		{
			_fontSystem.Scale = scale;

			var result = _fontSystem.DrawText(batch, pos.X, pos.Y, text, glyphColors, depth);

			_fontSystem.Scale = Vector2.One;

			return result;
		}
		*/

		/*
		public double DrawString(SpriteBatch batch, StringBuilder text, Vector2 pos, Color color)
		{
			return DrawString(batch, text, pos, color, Vector2.One);
		}
		*/

		/*
		public double DrawString(SpriteBatch batch, StringBuilder text, Vector2 pos, Color color, Vector2 scale, float depth = 0f)
		{
			_fontSystem.Scale = scale;

			var result = _fontSystem.DrawText(batch, pos.X, pos.Y, text, color, depth);

			_fontSystem.Scale = Vector2.One;

			return result;
		}
		*/

		/*
		public double DrawString(SpriteBatch batch, StringBuilder text, Vector2 pos, Color[] glyphColors)
		{
			return DrawString(batch, text, pos, glyphColors, Vector2.One);
		}
		*/

        /*public double GetDescent()
        {
			return _fontSystem.GetDescent();
        }*/

		public double GetAscent(string str)
		{
			return _fontSystem.GetAscent(str);
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

			return new Vector2((float)bounds.X2, (float)bounds.Y2);
		}

		/*
		public Vector2 MeasureString(StringBuilder text)
		{
			Bounds bounds = new Bounds();
			_fontSystem.TextBounds(0, 0, text, ref bounds);

			return new Vector2((float)bounds.X2, (float)bounds.Y2);
		}
		*/

		public Rectangle GetTextBounds(Vector2 position, string text)
		{
			Bounds bounds = new Bounds();
			_fontSystem.TextBounds(position.X, position.Y, text, ref bounds);

			return new Rectangle((int)bounds.X, (int)bounds.Y, (int)(bounds.X2 - bounds.X), (int)(bounds.Y2 - bounds.Y));
		}

		/*
		public Rectangle GetTextBounds(Vector2 position, StringBuilder text)
		{
			Bounds bounds = new Bounds();
			_fontSystem.TextBounds(position.X, position.Y, text, ref bounds);

			return new Rectangle((int)bounds.X, (int)bounds.Y, (int)(bounds.X2 - bounds.X), (int)(bounds.Y2 - bounds.Y));
		}
		*/

		public List<Rectangle> GetGlyphRectsFull(Vector2 position, string text) {
			return _fontSystem.GetGlyphRectsFull(position.X, position.Y, text);
		}

		/*
		public List<Rectangle> GetGlyphRects(Vector2 position, string text){
			return _fontSystem.GetGlyphRects(position.X, position.Y, text);
		}

		public List<Rectangle> GetGlyphRects(Vector2 position, StringBuilder text){
			return _fontSystem.GetGlyphRects(position.X, position.Y, text);
		}
		*/

		public static DynamicSpriteFont FromTtf(byte[] ttf, int defaultSize, int textureWidth = 1024, int textureHeight = 1024, int blur = 0, int stroke = 0)
		{
			return new DynamicSpriteFont(ttf, defaultSize, textureWidth, textureHeight, blur, stroke);
		}

		public static DynamicSpriteFont FromTtf(Stream ttfStream, int defaultSize, int textureWidth = 1024, int textureHeight = 1024, int blur = 0, int stroke = 0)
		{
			return FromTtf(ttfStream.ToByteArray(), defaultSize, textureWidth, textureHeight, blur, stroke);
		}
	}
}
