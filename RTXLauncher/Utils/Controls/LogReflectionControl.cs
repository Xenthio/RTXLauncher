using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace RTXLauncher.Controls
{
	// Create a new custom control for the reflection
	public class LogReflectionControl : Control
	{
		private RichTextBox _sourceTextBox;
		private Bitmap _reflectionBitmap;
		private string _lastText = "";
		private int _lastScrollPosition = 0;
		private Size _lastSourceSize = Size.Empty;
		private System.Windows.Forms.Timer _updateTimer;
		private System.Windows.Forms.Timer _updateTimer2;

		public LogReflectionControl()
		{
			// Set up control properties for flicker-free rendering
			this.SetStyle(ControlStyles.DoubleBuffer, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			this.SetStyle(ControlStyles.ResizeRedraw, true);
			this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

			// Important for flicker-free rendering
			this.BackColor = Color.Transparent;

			// Create a timer to handle updates (prevents too many rapid updates)
			_updateTimer = new System.Windows.Forms.Timer();
			_updateTimer.Interval = 50; // 50ms debounce
			_updateTimer.Tick += (s, e) =>
			{
				_updateTimer.Stop();
				UpdateReflection(false);
			};

			_updateTimer2 = new System.Windows.Forms.Timer();
			_updateTimer2.Interval = 200; //
			_updateTimer2.Tick += (s, e) =>
			{
				UpdateReflection(true);
			};
		}

		public RichTextBox SourceTextBox
		{
			get { return _sourceTextBox; }
			set
			{
				if (_sourceTextBox != value)
				{
					// Remove old event handlers
					if (_sourceTextBox != null)
					{
						_sourceTextBox.TextChanged -= SourceTextBox_Changed;
						_sourceTextBox.VScroll -= SourceTextBox_Scrolled;
						_sourceTextBox.Resize -= SourceTextBox_Resized;
						_sourceTextBox.SizeChanged -= SourceTextBox_Resized;

						// Remove control message filter if needed
						if (_messageFilter != null)
						{
							Application.RemoveMessageFilter(_messageFilter);
							_messageFilter.WndProc -= MessageFilter_WndProc;
							_messageFilter = null;
						}
					}

					_sourceTextBox = value;

					// Add new event handlers
					if (_sourceTextBox != null)
					{
						_sourceTextBox.TextChanged += SourceTextBox_Changed;
						_sourceTextBox.VScroll += SourceTextBox_Scrolled;
						_sourceTextBox.Resize += SourceTextBox_Resized;
						_sourceTextBox.SizeChanged += SourceTextBox_Resized;

						// Add a message filter to capture WM_VSCROLL messages
						_messageFilter = new TextBoxMessageFilter(_sourceTextBox.Handle);
						_messageFilter.WndProc += MessageFilter_WndProc;
						Application.AddMessageFilter(_messageFilter);

						// Store initial scroll position and size
						_lastScrollPosition = GetScrollPosition(_sourceTextBox);
						_lastSourceSize = _sourceTextBox.Size;
					}

					// Force redraw
					QueueUpdate(true);
				}
			}
		}

		// Message filter to capture scroll events that might be missed
		private TextBoxMessageFilter _messageFilter;

		private class TextBoxMessageFilter : IMessageFilter
		{
			private const int WM_VSCROLL = 0x115;
			private const int WM_MOUSEWHEEL = 0x20A;
			private IntPtr _handle;

			public event EventHandler<Message> WndProc;

			public TextBoxMessageFilter(IntPtr handle)
			{
				_handle = handle;
			}

			public bool PreFilterMessage(ref Message m)
			{
				// Check for scroll-related messages for our textbox
				if ((m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL) &&
					(m.HWnd == _handle))
				{
					WndProc?.Invoke(this, m);
				}

				return false; // Don't block the message
			}
		}

		private void MessageFilter_WndProc(object sender, Message m)
		{
			// Queue an update when scroll-related Windows messages are detected
			QueueUpdate(false);
		}

		private void SourceTextBox_Changed(object sender, EventArgs e)
		{
			QueueUpdate(false);
		}

		private void SourceTextBox_Scrolled(object sender, EventArgs e)
		{
			QueueUpdate(false);
		}

		private void SourceTextBox_Resized(object sender, EventArgs e)
		{
			// Immediately update if size changed
			UpdateReflection(true);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			UpdateReflection(true);
		}

		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			UpdateReflection(true);
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (this.Visible)
			{
				UpdateReflection(true);
			}
		}

		private void QueueUpdate(bool immediate)
		{
			if (immediate)
			{
				_updateTimer.Stop();
				UpdateReflection(true);
			}
			else
			{
				// Restart the timer to debounce rapid updates
				_updateTimer.Stop();
				_updateTimer.Start();
			}
		}

		private int GetScrollPosition(RichTextBox rtb)
		{
			// For a RichTextBox, we can use EM_GETFIRSTVISIBLELINE message
			const int EM_GETFIRSTVISIBLELINE = 0x00CE;
			return NativeMethods.SendMessage(rtb.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero).ToInt32();
		}

		private static class NativeMethods
		{
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
		}

		public void UpdateReflection(bool force)
		{
			if (_sourceTextBox == null ||
				_sourceTextBox.Width <= 0 ||
				_sourceTextBox.Height <= 0 ||
				this.Width <= 0 ||
				this.Height <= 0)
			{
				return;
			}

			// Get current scroll position
			int currentScrollPosition = GetScrollPosition(_sourceTextBox);

			// Only update if forced or something changed
			bool needsUpdate = force ||
							 _lastText != _sourceTextBox.Text ||
							 _lastScrollPosition != currentScrollPosition ||
							 _lastSourceSize != _sourceTextBox.Size ||
							 _reflectionBitmap == null ||
							 _reflectionBitmap.Width != this.Width ||
							 _reflectionBitmap.Height != this.Height;

			if (!needsUpdate)
			{
				return;
			}

			// Store current state
			_lastText = _sourceTextBox.Text;
			_lastScrollPosition = currentScrollPosition;
			_lastSourceSize = _sourceTextBox.Size;

			// Clean up old bitmap
			if (_reflectionBitmap != null)
			{
				_reflectionBitmap.Dispose();
			}

			// Create a new reflection bitmap at the size of our control
			_reflectionBitmap = new Bitmap(this.Width, this.Height);

			using (Graphics g = Graphics.FromImage(_reflectionBitmap))
			{
				// Clear with transparent background
				g.Clear(Color.Transparent);

				// Create a temporary bitmap of the source text box
				using (Bitmap sourceBitmap = new Bitmap(_sourceTextBox.Width, _sourceTextBox.Height))
				{
					// Draw the source text box to the bitmap
					_sourceTextBox.DrawToBitmap(sourceBitmap, new Rectangle(0, 0, _sourceTextBox.Width, _sourceTextBox.Height));

					// Set high quality rendering
					g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
					g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

					// Flip vertically and position at the top of our control
					g.ScaleTransform(1, -1);
					g.TranslateTransform(0, -this.Height);

					// Draw with transparency
					ColorMatrix colorMatrix = new ColorMatrix();
					colorMatrix.Matrix33 = 0.6f; // 60% opacity

					using (ImageAttributes imgAttrs = new ImageAttributes())
					{
						imgAttrs.SetColorMatrix(colorMatrix);

						// Draw the flipped source image
						g.DrawImage(
							sourceBitmap,
							new Rectangle(0, 0, this.Width, this.Height),
							0, 0, sourceBitmap.Width, sourceBitmap.Height,
							GraphicsUnit.Pixel,
							imgAttrs);
					}

					// Reset transform to draw the gradient
					g.ResetTransform();

					// Add the gradient overlay
					using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
						new Point(0, 0),
						new Point(0, this.Height),
						Color.FromArgb(1, this.Parent?.BackColor ?? Color.Black),    // Almost transparent at top
						Color.FromArgb(255, this.Parent?.BackColor ?? Color.Black))) // Completely opaque at bottom
					{
						g.FillRectangle(gradientBrush, 0, 0, this.Width, this.Height);
					}
				}
			}

			// Force a repaint
			this.Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (_reflectionBitmap != null)
			{
				e.Graphics.DrawImage(_reflectionBitmap, 0, 0);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Clean up resources
				if (_sourceTextBox != null)
				{
					_sourceTextBox.TextChanged -= SourceTextBox_Changed;
					_sourceTextBox.VScroll -= SourceTextBox_Scrolled;
					_sourceTextBox.Resize -= SourceTextBox_Resized;
					_sourceTextBox.SizeChanged -= SourceTextBox_Resized;
					_sourceTextBox = null;
				}

				if (_messageFilter != null)
				{
					Application.RemoveMessageFilter(_messageFilter);
					_messageFilter.WndProc -= MessageFilter_WndProc;
					_messageFilter = null;
				}

				if (_reflectionBitmap != null)
				{
					_reflectionBitmap.Dispose();
					_reflectionBitmap = null;
				}

				if (_updateTimer != null)
				{
					_updateTimer.Stop();
					_updateTimer.Dispose();
					_updateTimer = null;
				}
			}

			base.Dispose(disposing);
		}
	}

}
