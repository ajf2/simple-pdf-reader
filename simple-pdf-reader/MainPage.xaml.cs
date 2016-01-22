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
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public MainPage() {
            this.InitializeComponent();

            LoadSavedData();
            PickFileAndDisplayPdf();
        }

        /// <summary>
        /// Load data saved in the past.
        /// </summary>
        private void LoadSavedData() {
            try {
                currentPage = (uint)localSettings.Values["savedPage"];
            } catch(Exception ex) when(ex is NullReferenceException || ex is InvalidCastException) {
                currentPage = 1;
            }
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
                LoadAllPdfPagesInFlipView(currentPage);
            } catch(Exception ex) {
                // Restart the function if the chosen file can't be read.
                PickFileAndDisplayPdf();
            }
        }

        private void flipView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var page = ((uint)((FlipView)sender).SelectedIndex + 1);
            localSettings.Values["savedPage"] = page;
        }

        private IEnumerable<int> FanSort(IEnumerable<int> things, int index) {
            if(index < 0 || index >= things.Count()) {
                throw new ArgumentOutOfRangeException("index", "Parameter index is not within the range of items in the collection.");
            }

            List<int> oldList = things.ToList();
            List<int> newList = new List<int>(things.Count());
            newList.Add(oldList[index]);
            int numOfItemsToAdd = 1;
            int pincrementReached = 1;
            int nincrementReached = 1;
            while(newList.Count < oldList.Count) {
                for(int i = 0; i < numOfItemsToAdd; i++) {
                    if(index + pincrementReached < oldList.Count) {
                        newList.Add(oldList[index + pincrementReached]);
                        pincrementReached++;
                    } else {
                        break;
                    }
                }
                for(int i = 0; i < numOfItemsToAdd; i++) {
                    if(index - nincrementReached >= 0) {
                        newList.Add(oldList[index - nincrementReached]);
                        nincrementReached++;
                    } else {
                        break;
                    }
                }
                numOfItemsToAdd++;
            }
            return newList.AsEnumerable();
        }

        private void LoadAllPdfPagesInFlipView(uint pageToDisplay) {
            flipView.SelectionChanged += null;



            List<int> indices = new List<int>((int)pdf.PageCount);
            for(int i = 0; i < pdf.PageCount; i++) {
                indices.Add(i);
            }
            indices = FanSort(indices, (int)currentPage).ToList();








            // Initialise the FlipView with Image objects.
            for(uint i = 0; i < pdf.PageCount; i++) {
                flipView.Items.Add(new Image());
                //if(i + 1 == pageToDisplay) {
                    
                //} else {
                //    flipView.Items.Add(new Image());
                //}
            }




            for(int i = 0; i < indices.Count; i++) {
                
                    using(PdfPage page = pdf.GetPage((uint)indices[i])) {
                        RenderPdfPageToImageAsync(page, (Image)flipView.Items[indices[i]]);
                    }
                
            }





            using(PdfPage page = pdf.GetPage(pageToDisplay - 1)) {
                //Image image = new Image();
                RenderPdfPageToImageAsync(page, (Image)flipView.Items[(int)pageToDisplay - 1]);
                //if(GetPageOrientation(page) == PdfPageOrientation.Portrait) {
                //}
                //flipView.Items.Add(image);
            }
            if(pageToDisplay <= pdf.PageCount) {
                flipView.SelectedIndex = (int)pageToDisplay - 1;
            }
            for(uint i = 0; i < pdf.PageCount; i++) {
                if(i + 1 != pageToDisplay) {
                    using(PdfPage page = pdf.GetPage(i)) {
                        Image image = new Image();
                        RenderPdfPageToImageAsync(page, (Image)flipView.Items[(int)i]);
                    } 
                }
            }
            flipView.SelectionChanged += flipView_SelectionChanged;
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
