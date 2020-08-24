using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteFontPlus.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SpriteFontPlus
{
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

		readonly FontSystem _fontSystem;

		public IEnumerable<Texture2D> Textures
		{
			get { return new TextureEnumerator(_fontSystem); }
		}

		public int Size
		{
			get { return _fontSystem.FontSize; }
			set { _fontSystem.FontSize = value; }
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

		public event EventHandler CurrentAtlasFull
		{
			add { _fontSystem.CurrentAtlasFull += value; }

			remove { _fontSystem.CurrentAtlasFull -= value; }
		}

		DynamicSpriteFont(byte[] ttf, int defaultSize, int textureWidth, int textureHeight, int blur, int stroke)
		{
			_fontSystem = new FontSystem(textureWidth, textureHeight, blur, stroke)
			{
				FontSize = defaultSize
			};

			_fontSystem.AddFontMem(ttf);
		}

		public void Dispose()
		{
			_fontSystem?.Dispose();
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

		public float DrawString(SpriteBatch batch, string text, Vector2 pos, Color[] glyphColors)
		{
			return DrawString(batch, text, pos, glyphColors, Vector2.One);
		}

		public float DrawString(SpriteBatch batch, string text, Vector2 pos, Color[] glyphColors, Vector2 scale, float depth = 0f)
		{
			_fontSystem.Scale = scale;

			var result = _fontSystem.DrawText(batch, pos.X, pos.Y, text, glyphColors, depth);

			_fontSystem.Scale = Vector2.One;

			return result;
		}

		public float DrawString(SpriteBatch batch, StringBuilder text, Vector2 pos, Color color)
		{
			return DrawString(batch, text, pos, color, Vector2.One);
		}

		public float DrawString(SpriteBatch batch, StringBuilder text, Vector2 pos, Color color, Vector2 scale, float depth = 0f)
		{
			_fontSystem.Scale = scale;

			var result = _fontSystem.DrawText(batch, pos.X, pos.Y, text, color, depth);

			_fontSystem.Scale = Vector2.One;

			return result;
		}

		public float DrawString(SpriteBatch batch, StringBuilder text, Vector2 pos, Color[] glyphColors)
		{
			return DrawString(batch, text, pos, glyphColors, Vector2.One);
		}

        public double GetDescent()
        {
			return _fontSystem.GetDescent();
        }

		public double GetAscent()
		{
			return _fontSystem.GetAscent();
		}

		public double GetLineHeightBasic()
		{
			return _fontSystem.GetLineHeightBasic();
		}

		public float DrawString(SpriteBatch batch, StringBuilder text, Vector2 pos, Color[] glyphColors, Vector2 scale, float depth = 0f)
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

		public Vector2 MeasureString(StringBuilder text)
		{
			Bounds bounds = new Bounds();
			_fontSystem.TextBounds(0, 0, text, ref bounds);

			return new Vector2((float)bounds.X2, (float)bounds.Y2);
		}

		public Rectangle GetTextBounds(Vector2 position, string text)
		{
			Bounds bounds = new Bounds();
			_fontSystem.TextBounds(position.X, position.Y, text, ref bounds);

			return new Rectangle((int)bounds.X, (int)bounds.Y, (int)(bounds.X2 - bounds.X), (int)(bounds.Y2 - bounds.Y));
		}

		public Rectangle GetTextBounds(Vector2 position, StringBuilder text)
		{
			Bounds bounds = new Bounds();
			_fontSystem.TextBounds(position.X, position.Y, text, ref bounds);

			return new Rectangle((int)bounds.X, (int)bounds.Y, (int)(bounds.X2 - bounds.X), (int)(bounds.Y2 - bounds.Y));
		}

		public List<Rectangle> GetGlyphRectsFull(Vector2 position, string text) {
			return _fontSystem.GetGlyphRectsFull(position.X, position.Y, text);
		}
		public List<Rectangle> GetGlyphRects(Vector2 position, string text){
			return _fontSystem.GetGlyphRects(position.X, position.Y, text);
		}

		public List<Rectangle> GetGlyphRects(Vector2 position, StringBuilder text){
			return _fontSystem.GetGlyphRects(position.X, position.Y, text);
		}

		public void Reset(int width, int height)
		{
			_fontSystem.Reset(width, height);
		}

		public void Reset()
		{
			_fontSystem.Reset();
		}

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
