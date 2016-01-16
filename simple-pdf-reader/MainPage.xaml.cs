using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using simple_pdf_reader.Classes;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace simple_pdf_reader {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {
        private PdfDocument pdf;
        private uint currentPage;

        public MainPage() {
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
                LoadAllPdfPagesInFlipViewAsync();
            } catch(Exception) {
                // Restart the function if the chosen file can't be read.
                PickFileAndDisplayPdf();
            }
        }

        private void LoadAllPdfPagesInFlipViewAsync() {
            for(uint i = 0; i < pdf.PageCount; i++) {
                using(PdfPage page = pdf.GetPage(i)) {
                    Image image = new Image();
                    RenderPdfPageToImageAsync(page, image);
                    if(GetPageOrientation(page) == PdfPageOrientation.Portrait) {
                    }
                    flipView.Items.Add(image);
                }
            }
        }

        private async void RenderPdfPageToImageAsync(PdfPage page, Image image) {
            var stream = new InMemoryRandomAccessStream();
            await page.RenderToStreamAsync(stream);
            BitmapImage src = new BitmapImage();
            image.Source = src;
            await src.SetSourceAsync(stream);
        }

        private PdfPage GetPreviousPdfPage(uint currentPage) {
            if(currentPage > 0) {
                return pdf.GetPage(currentPage - 1);
            }
            return null;
        }

        /// <summary>
        /// Gets the index of the sibling page of the current page.
        /// A sibling page is the page either before or after the current page.
        /// Usually it is the previous page (to help avoid spoilers).
        /// If both adjacent pages are of different orientations, there is no sibling page.
        /// If the current page is the first page, the sibling will be page 2 if it's the same orientation.
        /// If only one of the current page's siblings is of the same orientation, that page is the sibling.
        /// </summary>
        /// <param name="currentPage">The index of the page to get the sibling page index for.</param>
        /// <returns>The index of the sibling page, or null if there is no sibling.</returns>
        private PdfPage GetSiblingPage(PdfPage currentPage) {
            if(currentPage == null) {
                throw new ArgumentNullException("currentPage", "The currentPage parameter cannot be null.");
            }

            PdfPage previousPage = currentPage.Index > 0 ? pdf.GetPage(currentPage.Index - 1) : null;
            PdfPage nextPage = currentPage.Index < pdf.PageCount - 1 ? pdf.GetPage(currentPage.Index + 1) : null;

            if(previousPage == null && nextPage == null) {
                // One page document.
                return null;
            }

            PdfPageOrientation currentPageOrientation = GetPageOrientation(currentPage);
            
            if(previousPage == null) {
                return (currentPageOrientation == GetPageOrientation(nextPage)) || GetPageOrientation(nextPage) == PdfPageOrientation.Square ? nextPage : null;
            } else if((currentPageOrientation == GetPageOrientation(previousPage)) || GetPageOrientation(previousPage) == PdfPageOrientation.Square) {
                return previousPage;
            } else {
                return (currentPageOrientation == GetPageOrientation(nextPage)) || GetPageOrientation(nextPage) == PdfPageOrientation.Square ? nextPage : null;
            }
        }

        /// <summary>
        /// Gets the orientation of a page, either portrait, landscape or square.
        /// </summary>
        /// <param name="page">The page to check.</param>
        /// <returns>The PdfPageOrientation of the page.</returns>
        private PdfPageOrientation GetPageOrientation(PdfPage page) {
            if(page == null) {
                throw new ArgumentNullException("page", "The page parameter cannot be null.");
            }
            double height = page.Dimensions.MediaBox.Height;
            double width = page.Dimensions.MediaBox.Width;

            if(height == width) {
                return PdfPageOrientation.Square;
            }
            return width > height ? PdfPageOrientation.Landscape : PdfPageOrientation.Portrait;
        }
    }
}
