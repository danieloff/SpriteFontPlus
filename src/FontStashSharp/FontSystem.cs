using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FontStashSharp
{
	unsafe class FontSystem : IDisposable
	{
		class GlyphCollection
		{
			internal readonly Int32Map<FontGlyph> Glyphs = new Int32Map<FontGlyph>();
		}

		readonly Int32Map<GlyphCollection> _glyphs = new Int32Map<GlyphCollection>();

		readonly List<Font> _fonts = new List<Font>();
		float _ith;
		float _itw;
		FontAtlas _currentAtlas;
		Point _size;
		int _fontSize;

		public void SetRatio(int idx, double ratio)
        {
			_fonts[idx].Ratio = ratio;
			foreach (var f in _fonts)
			{
				f.Recalculate(_fontSize);
			}
		}

		public int FontSize
		{
			get { return _fontSize; }

			set
			{
				if (value == _fontSize)
				{
					return;
				}

				_fontSize = value;
				foreach (var f in _fonts)
				{
					f.Recalculate(_fontSize);
				}
			}
		}

		public readonly int BlurAmount;
		public readonly int StrokeAmount;
		public float Spacing;
		public float LineSpacing = 0f;
		public Vector2 Scale;
		public bool UseKernings = true;

		public int? DefaultCharacter = ' ';

		public FontAtlas CurrentAtlas
		{
			get
			{
				if (_currentAtlas == null)
				{
					_currentAtlas = new FontAtlas(_size.X, _size.Y, 256);
					Atlases.Add(_currentAtlas);
				}

				return _currentAtlas;
			}
		}

		public List<FontAtlas> Atlases { get; } = new List<FontAtlas>();

		public event EventHandler CurrentAtlasFull;

		public FontSystem(int width, int height, int blurAmount = 0, int strokeAmount = 0)
		{
			if (width <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(width));
			}

			if (height <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(height));
			}

			if (blurAmount < 0 || blurAmount > 20)
			{
				throw new ArgumentOutOfRangeException(nameof(blurAmount));
			}

			if (strokeAmount < 0 || strokeAmount > 20)
			{
				throw new ArgumentOutOfRangeException(nameof(strokeAmount));
			}

			if (strokeAmount != 0 && blurAmount != 0)
			{
				throw new ArgumentException("Cannot have both blur and stroke.");
			}

			BlurAmount = blurAmount;
			StrokeAmount = strokeAmount;

			_size = new Point(width, height);

			_itw = 1.0f / _size.X;
			_ith = 1.0f / _size.Y;
			ClearState();
		}

		public void Dispose()
		{
			if (_fonts != null)
			{
				foreach (var font in _fonts)
					font.Dispose();
				_fonts.Clear();
			}
			Atlases?.Clear();
			_currentAtlas = null;
			_glyphs?.Clear();
		}

		public void ClearState()
		{
			FontSize = 12;
			Spacing = 0;
		}

		public void AddFontMem(byte[] data)
		{
			var font = Font.FromMemory(data);

			font.Recalculate(FontSize);
			_fonts.Add(font);
		}

		GlyphCollection GetGlyphsCollection(int size)
		{
			GlyphCollection result;
			if (_glyphs.TryGetValue(size, out result))
			{
				return result;
			}

			result = new GlyphCollection();
			_glyphs[size] = result;
			return result;
		}

		private void PreDraw(string str, out GlyphCollection glyphs, out double ascent, out double lineHeight)
		{
			glyphs = GetGlyphsCollection(FontSize);

			// Determine ascent and lineHeight from first character
			ascent = 0;
			lineHeight = 0;
			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);

				var glyph = GetGlyph(null, glyphs, codepoint);
				if (glyph == null)
				{
					continue;
				}

				ascent = glyph.Font.Ascent;
				lineHeight = glyph.Font.LineHeight + LineSpacing;
				break;
			}
		}

		private void PreDraw2(string str, out GlyphCollection glyphs, out double ascent, out double lineHeight, out double descent, out double lineHeightBasic)
		{
			glyphs = GetGlyphsCollection(FontSize);

			// Determine ascent and lineHeight from first character
			ascent = 0;
			lineHeight = 0;
			descent = 0;
			lineHeightBasic = 0;
			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);

				var glyph = GetGlyph(null, glyphs, codepoint);
				if (glyph == null)
				{
					continue;
				}

				ascent = glyph.Font.Ascent;
				descent = glyph.Font.Descent;
				lineHeightBasic = glyph.Font.LineHeight;
				lineHeight = glyph.Font.LineHeight + LineSpacing;
				break;
			}
		}

		internal double GetDescent()
        {
			GlyphCollection glyphs;
			double ascent, lineHeight, descent, lineHeightBasic;
			var str = " ";
			PreDraw2(str, out glyphs, out ascent, out lineHeight, out descent, out lineHeightBasic);
			return descent;
        }

		internal double GetAscent()
		{
			GlyphCollection glyphs;
			double ascent, lineHeight, descent, lineHeightBasic;
			var str = " ";
			PreDraw2(str, out glyphs, out ascent, out lineHeight, out descent, out lineHeightBasic);
			return ascent;
		}

		internal double GetLineHeightBasic()
		{
			GlyphCollection glyphs;
			double ascent, lineHeight, descent, lineHeightBasic;
			var str = " ";
			PreDraw2(str, out glyphs, out ascent, out lineHeight, out descent, out lineHeightBasic);
			return lineHeightBasic;
		}

		public double DrawText(SpriteBatch batch, double x, double y, string str, Color color, float depth)
		{
			if (string.IsNullOrEmpty(str)) return 0.0f;

			GlyphCollection glyphs;
			double ascent, lineHeight;
			PreDraw(str, out glyphs, out ascent, out lineHeight);

			x = Math.Round(x);
			y = Math.Round(y);

			double originX = 0.0;
			double originY = 0.0;

			originY += ascent;

			FontGlyph prevGlyph = null;
			var q = new FontGlyphSquad();
			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);

				if (codepoint == '\n')
				{
					originX = 0.0f;
					originY += lineHeight;
					prevGlyph = null;
					continue;
				}

				var glyph = GetGlyph(batch.GraphicsDevice, glyphs, codepoint);
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, Spacing, ref originX, ref originY, ref q);
				if (!glyph.IsEmpty)
				{
					q.X0 = (q.X0 * Scale.X);
					q.X1 = (q.X1 * Scale.X);
					q.Y0 = (q.Y0 * Scale.Y);
					q.Y1 = (q.Y1 * Scale.Y);

					var destRect = new Rectangle((int)Math.Round(x + q.X0),
												(int)Math.Round(y + q.Y0),
												(int)Math.Round(q.X1 - q.X0),
												(int)Math.Round(q.Y1 - q.Y0));

					var sourceRect = new Rectangle((int)(q.S0 * _size.X),
												(int)(q.T0 * _size.Y),
												(int)((q.S1 - q.S0) * _size.X),
												(int)((q.T1 - q.T0) * _size.Y));

					batch.Draw(glyph.Atlas.Texture,
						destRect,
						sourceRect,
						color,
						0f,
						Vector2.Zero,
						SpriteEffects.None,
						depth);
				}

				prevGlyph = glyph;
			}

			return x;
		}

		public double DrawText(SpriteBatch batch, double x, double y, string str, Color[] glyphColors, float depth)
		{
			if (string.IsNullOrEmpty(str)) return 0.0f;

			GlyphCollection glyphs;
			double ascent, lineHeight;
			PreDraw(str, out glyphs, out ascent, out lineHeight);

			x = Math.Round(x);
			y = Math.Round(y);

			var originX = 0.0;
			var originY = 0.0;

			originY += ascent;

			FontGlyph prevGlyph = null;
			var pos = 0;
			var q = new FontGlyphSquad();
			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);

				if (codepoint == '\n')
				{
					originX = 0.0f;
					originY += lineHeight;
					prevGlyph = null;
					++pos;
					continue;
				}

				var glyph = GetGlyph(batch.GraphicsDevice, glyphs, codepoint);
				if (glyph == null)
				{
					++pos;
					continue;
				}

				GetQuad(glyph, prevGlyph, Spacing, ref originX, ref originY, ref q);
				if (!glyph.IsEmpty)
				{
					q.X0 = (q.X0 * Scale.X);
					q.X1 = (q.X1 * Scale.X);
					q.Y0 = (q.Y0 * Scale.Y);
					q.Y1 = (q.Y1 * Scale.Y);

					var destRect = new Rectangle((int)Math.Round(x + q.X0),
												(int)Math.Round(y + q.Y0),
												(int)Math.Round(q.X1 - q.X0),
												(int)Math.Round(q.Y1 - q.Y0));

					var sourceRect = new Rectangle((int)(q.S0 * _size.X),
												(int)(q.T0 * _size.Y),
												(int)((q.S1 - q.S0) * _size.X),
												(int)((q.T1 - q.T0) * _size.Y));

					batch.Draw(glyph.Atlas.Texture,
						destRect,
						sourceRect,
						glyphColors[pos],
						0f,
						Vector2.Zero,
						SpriteEffects.None,
						depth);
				}

				prevGlyph = glyph;
				++pos;
			}

			return x;
		}

		private void PreDraw(StringBuilder str, out GlyphCollection glyphs, out double ascent, out double lineHeight)
		{
			glyphs = GetGlyphsCollection(FontSize);

			// Determine ascent and lineHeight from first character
			ascent = 0;
			lineHeight = 0;
			for (int i = 0; i < str.Length; i += StringBuilderIsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = StringBuilderConvertToUtf32(str, i);

				var glyph = GetGlyph(null, glyphs, codepoint);
				if (glyph == null)
				{
					continue;
				}

				ascent = glyph.Font.Ascent;
				lineHeight = glyph.Font.LineHeight + LineSpacing;
				break;
			}
		}

		public double DrawText(SpriteBatch batch, double x, double y, StringBuilder str, Color color, float depth)
		{
			if (str == null || str.Length == 0) return 0.0f;

			GlyphCollection glyphs;
			double ascent, lineHeight;
			PreDraw(str, out glyphs, out ascent, out lineHeight);

			x = Math.Round(x);
			y = Math.Round(y);

			var originX = 0.0;
			var originY = 0.0;

			originY += ascent;

			FontGlyph prevGlyph = null;
			var q = new FontGlyphSquad();
			for (int i = 0; i < str.Length; i += StringBuilderIsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = StringBuilderConvertToUtf32(str, i);

				if (codepoint == '\n')
				{
					originX = 0.0f;
					originY += lineHeight;
					prevGlyph = null;
					continue;
				}

				var glyph = GetGlyph(batch.GraphicsDevice, glyphs, codepoint);
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, Spacing, ref originX, ref originY, ref q);
				if (!glyph.IsEmpty)
				{
					q.X0 = (q.X0 * Scale.X);
					q.X1 = (q.X1 * Scale.X);
					q.Y0 = (q.Y0 * Scale.Y);
					q.Y1 = (q.Y1 * Scale.Y);

					var destRect = new Rectangle((int)(x + q.X0),
												(int)(y + q.Y0),
												(int)(q.X1 - q.X0),
												(int)(q.Y1 - q.Y0));

					var sourceRect = new Rectangle((int)(q.S0 * _size.X),
												(int)(q.T0 * _size.Y),
												(int)((q.S1 - q.S0) * _size.X),
												(int)((q.T1 - q.T0) * _size.Y));

					batch.Draw(glyph.Atlas.Texture,
						destRect,
						sourceRect,
						color,
						0f,
						Vector2.Zero,
						SpriteEffects.None,
						depth);
				}

				prevGlyph = glyph;
			}

			return x;
		}

		public double DrawText(SpriteBatch batch, double x, double y, StringBuilder str, Color[] glyphColors, float depth)
		{
			if (str == null || str.Length == 0) return 0.0f;

			GlyphCollection glyphs;
			double ascent, lineHeight;
			PreDraw(str, out glyphs, out ascent, out lineHeight);

			x = Math.Round(x);
			y = Math.Round(y);

			var originX = 0.0;
			var originY = 0.0;

			originY += ascent;

			FontGlyph prevGlyph = null;
			var pos = 0;
			var q = new FontGlyphSquad();
			for (int i = 0; i < str.Length; i += StringBuilderIsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = StringBuilderConvertToUtf32(str, i);

				if (codepoint == '\n')
				{
					originX = 0.0f;
					originY += lineHeight;
					prevGlyph = null;
					++pos;
					continue;
				}

				var glyph = GetGlyph(batch.GraphicsDevice, glyphs, codepoint);
				if (glyph == null)
				{
					++pos;
					continue;
				}

				GetQuad(glyph, prevGlyph, Spacing, ref originX, ref originY, ref q);
				if (!glyph.IsEmpty)
				{
					q.X0 = (q.X0 * Scale.X);
					q.X1 = (q.X1 * Scale.X);
					q.Y0 = (q.Y0 * Scale.Y);
					q.Y1 = (q.Y1 * Scale.Y);

					var destRect = new Rectangle((int)Math.Round(x + q.X0),
												(int)Math.Round(y + q.Y0),
												(int)Math.Round(q.X1 - q.X0),
												(int)Math.Round(q.Y1 - q.Y0));

					var sourceRect = new Rectangle((int)(q.S0 * _size.X),
												(int)(q.T0 * _size.Y),
												(int)((q.S1 - q.S0) * _size.X),
												(int)((q.T1 - q.T0) * _size.Y));

					batch.Draw(glyph.Atlas.Texture,
						destRect,
						sourceRect,
						glyphColors[pos],
						0f,
						Vector2.Zero,
						SpriteEffects.None,
						depth);
				}

				prevGlyph = glyph;
				++pos;
			}

			return x;
		}

		public double TextBounds(double x, double y, string str, ref Bounds bounds)
		{
			if (string.IsNullOrEmpty(str)) return 0.0f;

			x = Math.Round(x);
			y = Math.Round(y);

			GlyphCollection glyphs;
			double ascent, lineHeight;
			PreDraw(str, out glyphs, out ascent, out lineHeight);

			var q = new FontGlyphSquad();
			y += ascent;

			double minx, maxx, miny, maxy;
			minx = maxx = x;
			miny = maxy = y;
			double startx = x;

			FontGlyph prevGlyph = null;
			var end = x;

			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);

				if (codepoint == '\n')
				{
					x = startx;
					y += lineHeight;
					prevGlyph = null;
					continue;
				}

				var glyph = GetGlyph(null, glyphs, codepoint);
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, Spacing, ref x, ref y, ref q);

				end = x + glyph.XAdvance * glyph.Font.Scale;
				if (q.X0 < minx)
					minx = q.X0;
				if (end > maxx)
					maxx = end;
				if (q.Y0 < miny)
					miny = q.Y0;
				if (q.Y1 > maxy)
					maxy = q.Y1;

				prevGlyph = glyph;
			}

			maxx += StrokeAmount * 2;

			double advance = end - startx;
			bounds.X = minx;
			bounds.Y = miny;
			bounds.X2 = maxx;
			bounds.Y2 = maxy;

			return advance;
		}

		public double TextBounds(double x, double y, StringBuilder str, ref Bounds bounds)
		{
			if (str == null || str.Length == 0) return 0.0f;

			x = Math.Round(x);
			y = Math.Round(y);

			GlyphCollection glyphs;
			double ascent, lineHeight;
			PreDraw(str, out glyphs, out ascent, out lineHeight);

			var q = new FontGlyphSquad();
			y += ascent;

			double minx, maxx, miny, maxy;
			minx = maxx = x;
			miny = maxy = y;
			double startx = x;

			FontGlyph prevGlyph = null;
			var end = x;

			for (int i = 0; i < str.Length; i += StringBuilderIsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = StringBuilderConvertToUtf32(str, i);

				if (codepoint == '\n')
				{
					x = startx;
					y += lineHeight;
					prevGlyph = null;
					continue;
				}

				var glyph = GetGlyph(null, glyphs, codepoint);
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, Spacing, ref x, ref y, ref q);

				end = x + glyph.XAdvance * glyph.Font.Scale;

				if (q.X0 < minx)
					minx = q.X0;
				if (end > maxx)
					maxx = end;
				if (q.Y0 < miny)
					miny = q.Y0;
				if (q.Y1 > maxy)
					maxy = q.Y1;

				prevGlyph = glyph;
			}

			maxx += StrokeAmount * 2;

			var advance = end - startx;
			bounds.X = minx;
			bounds.Y = miny;
			bounds.X2 = maxx;
			bounds.Y2 = maxy;

			return advance;
		}
		
		public List<Rectangle> GetGlyphRectsFull(double x, double y, string str){
			List<Rectangle> Rects = new List<Rectangle>();
			if (string.IsNullOrEmpty(str)) return Rects;

			x = Math.Round(x);
			y = Math.Round(y);

			GlyphCollection glyphs;
			double ascent, lineHeight;
			PreDraw(str, out glyphs, out ascent, out lineHeight);

			var q = new FontGlyphSquad();
			y += ascent;

			double minx, maxx, miny, maxy;
			minx = maxx = x;
			miny = maxy = y;
			double startx = x;

			FontGlyph prevGlyph = null;

			Rectangle l = new Rectangle((int)x, (int)y, 0, _fontSize);

			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);

				if (codepoint == '\n') {
					l = new Rectangle((int)l.X, l.Y, 0, (int)(l.Height));
					Rects.Add(l);
					x = startx;
					y += lineHeight;
					prevGlyph = null;
					continue;
				}

				var glyph = GetGlyph(null, glyphs, codepoint);
				if (glyph == null)
				{
					l = new Rectangle((int)l.X, l.Y, 0, (int)(l.Height));
					Rects.Add(l);
					continue;
				}

				GetQuad(glyph, prevGlyph, Spacing, ref x, ref y, ref q);

				l = new Rectangle((int)q.X0, (int)q.Y0, (int)(q.X1-q.X0), (int)(q.Y1-q.Y0));
				Rects.Add(l);
				prevGlyph = glyph;

				if (char.IsSurrogatePair(str, i)) {
					l = new Rectangle((int)l.X, l.Y, 0, (int)(l.Height)); //ignore the width on the second part of surrogate? //TODO see how this works on input
					Rects.Add(l);
				}
			}

			return Rects;
		}

		public List<Rectangle> GetGlyphRects(double x, double y, string str){
			List<Rectangle> Rects = new List<Rectangle>();
			if (string.IsNullOrEmpty(str)) return Rects;

			x = Math.Round(x);
			y = Math.Round(y);

			GlyphCollection glyphs;
			double ascent, lineHeight;
			PreDraw(str, out glyphs, out ascent, out lineHeight);

			var q = new FontGlyphSquad();
			y += ascent;

			double minx, maxx, miny, maxy;
			minx = maxx = x;
			miny = maxy = y;
			double startx = x;

			FontGlyph prevGlyph = null;

			for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = char.ConvertToUtf32(str, i);

				if (codepoint == '\n')
				{
					x = startx;
					y += lineHeight;
					prevGlyph = null;
					continue;
				}

				var glyph = GetGlyph(null, glyphs, codepoint);
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, Spacing, ref x, ref y, ref q);

				Rects.Add(new Rectangle((int)q.X0, (int)q.Y0, (int)(q.X1 - q.X0), (int)(q.Y1 - q.Y0)));
				prevGlyph = glyph;
			}

			return Rects;
		}

		public List<Rectangle> GetGlyphRects(double x, double y, StringBuilder str)
		{
			List<Rectangle> Rects = new List<Rectangle>();
			if (str == null || str.Length == 0) return Rects;

			x = Math.Round(x);
			y = Math.Round(y);

			GlyphCollection glyphs;
			double ascent, lineHeight;
			PreDraw(str, out glyphs, out ascent, out lineHeight);

			var q = new FontGlyphSquad();
			y += ascent;

			double minx, maxx, miny, maxy;
			minx = maxx = x;
			miny = maxy = y;
			double startx = x;

			FontGlyph prevGlyph = null;

			for (int i = 0; i < str.Length; i += StringBuilderIsSurrogatePair(str, i) ? 2 : 1)
			{
				var codepoint = StringBuilderConvertToUtf32(str, i);

				if (codepoint == '\n')
				{
					x = startx;
					y += lineHeight;
					prevGlyph = null;
					continue;
				}

				var glyph = GetGlyph(null, glyphs, codepoint);
				if (glyph == null)
				{
					continue;
				}

				GetQuad(glyph, prevGlyph, Spacing, ref x, ref y, ref q);

				Rects.Add(new Rectangle((int)q.X0, (int)q.Y0, (int)(q.X1 - q.X0), (int)(q.Y1 - q.Y0)));
				prevGlyph = glyph;
			}

			return Rects;
		}

		bool StringBuilderIsSurrogatePair(StringBuilder sb, int index)
		{
			if (index + 1 < sb.Length)
				return char.IsSurrogatePair(sb[index], sb[index + 1]);
			return false;
		}

		int StringBuilderConvertToUtf32(StringBuilder sb, int index)
		{
			if (!char.IsHighSurrogate(sb[index]))
				return sb[index];

			return char.ConvertToUtf32(sb[index], sb[index + 1]);
		}

		public void Reset(int width, int height)
		{
			Atlases.Clear();

			_glyphs.Clear();

			if (width == _size.X && height == _size.Y)
				return;

			_size = new Point(width, height);
			_itw = 1.0f / _size.X;
			_ith = 1.0f / _size.Y;
		}

		public void Reset()
		{
			Reset(_size.X, _size.Y);
		}

		int GetCodepointIndex(int codepoint, out Font font)
		{
			font = null;

			var g = 0;
			foreach (var f in _fonts)
			{
				g = f.GetGlyphIndex(codepoint);
				if (g != 0)
				{
					font = f;
					break;
				}
			}

			return g;
		}

		FontGlyph GetGlyphWithoutBitmap(GlyphCollection collection, int codepoint)
		{
			FontGlyph glyph = null;
			if (collection.Glyphs.TryGetValue(codepoint, out glyph))
			{
				return glyph;
			}

			Font font;
			var g = GetCodepointIndex(codepoint, out font);
			if (g == 0)
			{
				return null;
			}

			int advance = 0, lsb = 0, x0 = 0, y0 = 0, x1 = 0, y1 = 0;
			font.BuildGlyphBitmap(g, font.Scale, ref advance, ref lsb, ref x0, ref y0, ref x1, ref y1);

			var pad = Math.Max(FontGlyph.PadFromBlur(BlurAmount), FontGlyph.PadFromBlur(StrokeAmount));
			var gw = x1 - x0 + pad * 2;
			var gh = y1 - y0 + pad * 2;
			var offset = FontGlyph.PadFromBlur(BlurAmount);

			glyph = new FontGlyph
			{
				Font = font,
				Codepoint = codepoint,
				Size = FontSize,
				Index = g,
				Bounds = new Rectangle(0, 0, gw, gh),
				XAdvance = advance,
				XOffset = x0 - offset, //blur, interesting
				YOffset = y0 - offset
			};

			collection.Glyphs[codepoint] = glyph;

			return glyph;
		}

		FontGlyph GetGlyphInternal(GraphicsDevice graphicsDevice, GlyphCollection glyphs, int codepoint)
		{
			var glyph = GetGlyphWithoutBitmap(glyphs, codepoint);
			if (glyph == null)
			{
				return null;
			}

			if (graphicsDevice == null || glyph.Atlas != null)
				return glyph;

			var currentAtlas = CurrentAtlas;
			int gx = 0, gy = 0;
			var gw = glyph.Bounds.Width;
			var gh = glyph.Bounds.Height;
			if (!currentAtlas.AddRect(gw, gh, ref gx, ref gy))
			{
				CurrentAtlasFull?.Invoke(this, EventArgs.Empty);

				// This code will force creation of new atlas
				_currentAtlas = null;
				currentAtlas = CurrentAtlas;

				// Try to add again
				if (!currentAtlas.AddRect(gw, gh, ref gx, ref gy))
				{
					throw new Exception(string.Format("Could not add rect to the newly created atlas. gw={0}, gh={1}", gw, gh));
				}
			}

			glyph.Bounds.X = gx;
			glyph.Bounds.Y = gy;

			currentAtlas.RenderGlyph(graphicsDevice, glyph, BlurAmount, StrokeAmount);

			glyph.Atlas = currentAtlas;

			return glyph;
		}

		FontGlyph GetGlyph(GraphicsDevice graphicsDevice, GlyphCollection glyphs, int codepoint)
		{
			var result = GetGlyphInternal(graphicsDevice, glyphs, codepoint);
			if (result == null && DefaultCharacter != null)
			{
				result = GetGlyphInternal(graphicsDevice, glyphs, DefaultCharacter.Value);
			}

			return result;
		}

		private void GetQuad(FontGlyph glyph, FontGlyph prevGlyph, float spacing, ref double x, ref double y, ref FontGlyphSquad q)
		{
			if (prevGlyph != null)
			{
				int adv = 0;
				if (UseKernings && glyph.Font == prevGlyph.Font)
				{
					adv = prevGlyph.Font.GetGlyphKernAdvance(prevGlyph.Index, glyph.Index);
				}

				x += (prevGlyph.XAdvance + adv) * prevGlyph.Font.Scale;
			}

			double rx = x + glyph.XOffset;
			double ry = y + glyph.YOffset;
			q.X0 = rx;
			q.Y0 = ry;
			q.X1 = rx + glyph.Bounds.Width;
			q.Y1 = ry + glyph.Bounds.Height;
			q.S0 = glyph.Bounds.X * _itw;
			q.T0 = glyph.Bounds.Y * _ith;
			q.S1 = glyph.Bounds.Right * _itw;
			q.T1 = glyph.Bounds.Bottom * _ith;
		}
	}
}
