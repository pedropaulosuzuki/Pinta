/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class GaussianBlurEffect : BaseEffect
{
	public override string Icon => Resources.Icons.EffectsBlursGaussianBlur;

	public sealed override bool IsTileable => false;

	public override string Name => Translations.GetString ("Gaussian Blur");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Blurs");

	public GaussianBlurData Data => (GaussianBlurData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public GaussianBlurEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new GaussianBlurData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	#region Algorithm Code Ported From PDN

	public static ImmutableArray<int> CreateGaussianBlurRow (int amount)
	{
		int size = 1 + (amount * 2);
		var weights = ImmutableArray.CreateBuilder<int> (size);
		weights.Count = size;

		for (int i = 0; i <= amount; ++i) {
			// 1 + aa - aa + 2ai - ii
			weights[i] = 16 * (i + 1);
			weights[size - i - 1] = weights[i];
		}

		return weights.MoveToImmutable ();
	}

	public override void Render (ImageSurface src, ImageSurface output, ReadOnlySpan<RectangleI> rois)
	{
		if (Data.Radius == 0)
			return; // Copy src to output

		int radius = Data.Radius;
		ImmutableArray<int> weights = CreateGaussianBlurRow (radius);
		int weights_length = weights.Length;

		int src_width = src.Width;
		int src_height = src.Height;
		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> output_data = output.GetPixelData ();

		// Blur vertically
		for (int y = 0; y < src_height; y++) {
			for (int x = 0; x < src_width; x++) {
				long red_sum = 0;
				long green_sum = 0;
				long blue_sum = 0;
				long alpha_sum = 0;
				long weights_sum = 0;

				for (int i = 0; i < weights_length; i++) {
					int pos = y - radius + i;
					if (pos < 0 || pos >= src_height) { continue; }

					var pixel = output_data[pos * src_width + x];

					red_sum += weights[i] * (long) pixel.R;
					green_sum += weights[i] * (long) pixel.G;
					blue_sum += weights[i] * (long) pixel.B;
					alpha_sum += weights[i] * (long) pixel.A;
					weights_sum += (long) weights[i];
				}

				output_data[y * src_width + x] = ColorBgra.FromBgra (
					(byte) (weights_sum == 0 ? 0 : blue_sum / weights_sum),
					(byte) (weights_sum == 0 ? 0 : green_sum / weights_sum),
					(byte) (weights_sum == 0 ? 0 : red_sum / weights_sum),
					(byte) (weights_sum == 0 ? 0 : alpha_sum / weights_sum));
			}
		}

		Pinta.Core.ColorBgra[] color_buffer = new Pinta.Core.ColorBgra[src_width];

		// Blur horizontally
		for (int y = 0; y < src_height; y++) {
			for (int x = 0; x < src_width; x++) {
				long red_sum = 0;
				long green_sum = 0;
				long blue_sum = 0;
				long alpha_sum = 0;
				long weights_sum = 0;

				color_buffer[x] = output_data[y * src_width + x];

				for (int i = 0; i < radius; i++) {
					int pos = x - radius + i;
					if (pos < 0 || pos >= src_width) { continue; }


					red_sum += weights[i] * (long) color_buffer[pos].R;
					green_sum += weights[i] * (long) color_buffer[pos].G;
					blue_sum += weights[i] * (long) color_buffer[pos].B;
					alpha_sum += weights[i] * (long) color_buffer[pos].A;
					weights_sum += (long) weights[i];
				}
				// Pixels ahead (don't need to use the buffer)
				for (int i = radius; i < weights_length; i++) {
					int pos = x - radius + i;
					if (pos < 0 || pos >= src_width) { continue; }

					var pixel = output_data[y * src_width + pos];


					red_sum += weights[i] * (long) pixel.R;
					green_sum += weights[i] * (long) pixel.G;
					blue_sum += weights[i] * (long) pixel.B;
					alpha_sum += weights[i] * (long) pixel.A;
					weights_sum += (long) weights[i];
				}

				output_data[y * src_width + x] = ColorBgra.FromBgra (
					(byte) (weights_sum == 0 ? 0 : blue_sum / weights_sum),
					(byte) (weights_sum == 0 ? 0 : green_sum / weights_sum),
					(byte) (weights_sum == 0 ? 0 : red_sum / weights_sum),
					(byte) (weights_sum == 0 ? 0 : alpha_sum / weights_sum));
			}
		}
	}
	#endregion

	public sealed class GaussianBlurData : EffectData
	{
		[Caption ("Radius")]
		[MinimumValue (0), MaximumValue (200)]
		public int Radius { get; set; } = 2;

		[Skip]
		public override bool IsDefault => Radius == 0;
	}
}
