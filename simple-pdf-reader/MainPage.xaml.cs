using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace simple_pdf_reader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private PdfDocument pdf;

        public MainPage()
        {
            this.InitializeComponent();
            PickFileAndDisplayPdf();
        }

        /// <summary>
        /// Opens a PDF file picker. If an exception is thrown, e.g. it's not a valid PDF,
        /// another file picker opens.
        /// </summary>
        private async void PickFileAndDisplayPdf() {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".pdf");
            StorageFile file = await picker.PickSingleFileAsync();

            try {
                pdf = await PdfDocument.LoadFromFileAsync(file);
                DisplaySinglePdfPage();
            } catch (Exception) {
                // Restart the function if the chosen file can't be read.
                PickFileAndDisplayPdf();
            }
        }

        /// <summary>
        /// Display a page of the current PDF document.
        /// </summary>
        /// <param name="pageToLoad">The page index to load, 0 by default.</param>
        private async void DisplaySinglePdfPage(uint pageToLoad = 0) {
            using(PdfPage page = pdf.GetPage(pageToLoad)) {
                var stream = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(stream);

                BitmapImage src = new BitmapImage();
                PdfViewArea.Source = src;
                await src.SetSourceAsync(stream);
            }
        }
    }
}
