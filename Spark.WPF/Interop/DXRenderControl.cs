using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ED8000
{
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media;

    using SharpDX.Direct3D11;

    public class DXRenderControl : FrameworkElement, IDisposable
    {
        private DXImageSource _source;
        private bool _disposed;

        public DXRenderControl()
        {
            this.SnapsToDevicePixels = true;

            if (!InDesignMode)
                _source = new DXImageSource();
        }

        /// <summary>
        /// Gibt an ob das Element momentan im Designmodus (Bearbeitung durch Visual Studio´s Designer oder Blend) ist.
        /// </summary>
        internal bool InDesignMode
        {
            get
            {
                return DesignerProperties.GetIsInDesignMode(this);
            }
        }

        /// <summary>
        /// Rendert das DXImageSoure-Element auf dieses Element.
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!InDesignMode)
                drawingContext.DrawImage(_source, new Rect(DesiredSize));
            else
                drawingContext.DrawText(
                    new FormattedText(
                        "DirectX Output Area",
                        new CultureInfo("de-DE"),
                        FlowDirection.RightToLeft,
                        new Typeface(new FontFamily("Arial,Courier New,Tahoma"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                        14,
                        new SolidColorBrush(Colors.Coral)),
                    new Point(Width / 2, Height / 2));
            base.OnRender(drawingContext);
        }

        /// <summary>
        /// Verhindert Anzeigefehler durch falsche Größen.
        /// </summary>
        /// <param name="availableSize"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(Math.Ceiling(availableSize.Width), Math.Ceiling(availableSize.Height));
        }

        /// <summary>
        /// Registriert eine Textur als Backbuffer. Wird null übergeben so wird die Referenz gelöscht und
        /// weitere Vorgänge ausgeführt.
        /// </summary>
        /// <param name="texture"></param>
        public void SetBackBuffer(Texture2D texture)
        {
            _source.SetBackBuffer(texture);
        }

        /// <summary>
        /// Updatet das DXImageSource Element nach einem Rendervorgang.
        /// Das Übergeben des BackBuffers (einer Textur) ist hier explizit nötig um unnötige Komplexität zu vermeiden.
        /// Hierfür wird eine DirectX11-Textur genutzt.
        /// </summary>
        public void UpdateSurface()
        {
            _source.Invalidate();
            this.InvalidateVisual();
        }

        /// <summary>
        /// Führt anwendungsspezifische Aufgaben durch, die mit der Freigabe, der Zurückgabe oder dem Zurücksetzen von nicht verwalteten Ressourcen zusammenhängen.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_disposed) return;

            _source.Dispose();
            _source = null;
            _disposed = true;
        }
    }
}
