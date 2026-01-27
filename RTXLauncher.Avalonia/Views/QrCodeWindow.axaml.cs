using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using QRCoder;
using SkiaSharp;
using System;
using System.IO;

namespace RTXLauncher.Avalonia.Views;

public partial class QrCodeWindow : Window
{
	public bool Result { get; private set; }

	public QrCodeWindow()
	{
		InitializeComponent();
	}

	public void SetQrCode(string challengeUrl)
	{
		if (DataContext is ViewModels.QrCodeViewModel viewModel)
		{
			viewModel.ChallengeUrl = challengeUrl;
			viewModel.QrCodeImage = GenerateQrCodeBitmap(challengeUrl);
		}
	}

	private static Bitmap GenerateQrCodeBitmap(string url)
	{
		using var qrGenerator = new QRCodeGenerator();
		using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
		
		var moduleCount = qrCodeData.ModuleMatrix.Count;
		var pixelsPerModule = 10;
		var imageSize = moduleCount * pixelsPerModule;
		
		using var surface = SKSurface.Create(new SKImageInfo(imageSize, imageSize));
		var canvas = surface.Canvas;
		canvas.Clear(SKColors.White);
		
		using var paint = new SKPaint
		{
			Color = SKColors.Black,
			IsAntialias = false,
			Style = SKPaintStyle.Fill
		};
		
		for (int row = 0; row < moduleCount; row++)
		{
			for (int col = 0; col < moduleCount; col++)
			{
				if (qrCodeData.ModuleMatrix[row][col])
				{
					canvas.DrawRect(
						col * pixelsPerModule,
						row * pixelsPerModule,
						pixelsPerModule,
						pixelsPerModule,
						paint);
				}
			}
		}
		
		using var image = surface.Snapshot();
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);
		var stream = new MemoryStream();
		data.SaveTo(stream);
		stream.Position = 0;
		
		return new Bitmap(stream);
	}

	private void CancelButton_Click(object? sender, RoutedEventArgs e)
	{
		Result = false;
		Close();
	}
}
