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

        private TransformGroup transforms;
        private MatrixTransform previousTransform;
        private CompositeTransform deltaTransform;
        private bool forceManipulationsToEnd;

        public MainPage() {
            this.InitializeComponent();

            //ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            
            PickFileAndDisplayPdf();
            

            // Initialize the transforms that will be used to manipulate the shape
            InitManipulationTransforms();

            // Register for the various manipulation events that will occur on the shape
            CurrentPdfPageArea.ManipulationStarted += new ManipulationStartedEventHandler(ManipulateMe_ManipulationStarted);
            CurrentPdfPageArea.ManipulationDelta += new ManipulationDeltaEventHandler(ManipulateMe_ManipulationDelta);
            CurrentPdfPageArea.ManipulationCompleted += new ManipulationCompletedEventHandler(ManipulateMe_ManipulationCompleted);
            CurrentPdfPageArea.ManipulationInertiaStarting += new ManipulationInertiaStartingEventHandler(ManipulateMe_ManipulationInertiaStarting);

            // The ManipulationMode property dictates what manipulation events the element
            // will listen to.  This will set it to a limited subset of these events.
            CurrentPdfPageArea.ManipulationMode =
                ManipulationModes.TranslateX |
                ManipulationModes.TranslateY |
                ManipulationModes.Rotate |
                ManipulationModes.TranslateInertia |
                ManipulationModes.RotateInertia;
        }
        
        // When a manipulation begins, change the color of the object to reflect
        // that a manipulation is in progress
        void ManipulateMe_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e) {
            forceManipulationsToEnd = false;
        }

        // Process the change resulting from a manipulation
        void ManipulateMe_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e) {
            // If the reset button has been pressed, mark the manipulation as completed
            if(forceManipulationsToEnd) {
                e.Complete();
                return;
            }

            previousTransform.Matrix = transforms.Value;

            // Get center point for rotation
            Point center = previousTransform.TransformPoint(new Point(e.Position.X, e.Position.Y));
            deltaTransform.CenterX = center.X;
            deltaTransform.CenterY = center.Y;

            // Look at the Delta property of the ManipulationDeltaRoutedEventArgs to retrieve
            // the rotation, scale, X, and Y changes
            deltaTransform.Rotation = e.Delta.Rotation;
            deltaTransform.TranslateX = e.Delta.Translation.X;
            deltaTransform.TranslateY = e.Delta.Translation.Y;
        }

        // When a manipulation that's a result of inertia begins, change the color of the
        // the object to reflect that inertia has taken over
        void ManipulateMe_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e) {
            //manipulateMe.Background = new SolidColorBrush(Windows.UI.Colors.RoyalBlue);
        }

        // When a manipulation has finished, reset the color of the object
        void ManipulateMe_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) {
            //manipulateMe.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
        }

        private void InitManipulationTransforms() {
            transforms = new TransformGroup();
            previousTransform = new MatrixTransform() { Matrix = Matrix.Identity };
            deltaTransform = new CompositeTransform();

            transforms.Children.Add(previousTransform);
            transforms.Children.Add(deltaTransform);

            // Set the render transform on the rect
            CurrentPdfPageArea.RenderTransform = transforms;
            SiblingPdfPageArea.RenderTransform = transforms;
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
                DisplayPdfPageAndPrevious();
            } catch(Exception) {
                // Restart the function if the chosen file can't be read.
                PickFileAndDisplayPdf();
            }
        }

        private void DisplayPdfPageAndPrevious(uint pageToLoad = 10) {
            using(PdfPage page = pdf.GetPage(pageToLoad)) {
                RenderPdfPageToImageAsync(page, SiblingPdfPageArea);
            }
            using(PdfPage prevPage = GetPreviousPdfPage(pageToLoad)) {
                if(prevPage != null) {
                    RenderPdfPageToImageAsync(prevPage, CurrentPdfPageArea);
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
        /// Display a page of the current PDF document.
        /// </summary>
        /// <param name="pageToLoad">The page index to load, 0 by default.</param>
        private async void DisplaySinglePdfPage(uint pageToLoad = 0) {
            using(PdfPage page = pdf.GetPage(pageToLoad)) {
                Debug.WriteLine(GetPageOrientation(page));
                var stream = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(stream);

                BitmapImage src = new BitmapImage();
                CurrentPdfPageArea.Source = src;
                await src.SetSourceAsync(stream);
            }
        }

        /// <summary>
        /// Display two pages of the current PDF document.
        /// </summary>
        /// <param name="pageToLoad">The page index to load, 0 by default.</param>
        private async void DisplayPdfPageAndSibling(uint pageToLoad = 50) {
            using(PdfPage page = pdf.GetPage(pageToLoad)) {
                PdfPage sibling = GetSiblingPage(page);
                Debug.WriteLine(GetPageOrientation(page));
                var streamLeft = new InMemoryRandomAccessStream();
                var streamRight = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(streamLeft);

                BitmapImage srcLeft = new BitmapImage();
                BitmapImage srcRight = new BitmapImage();
                CurrentPdfPageArea.Source = srcLeft;
                await srcLeft.SetSourceAsync(streamLeft);

                await sibling.RenderToStreamAsync(streamRight);
                SiblingPdfPageArea.Source = srcRight;
                await srcRight.SetSourceAsync(streamRight);
            }
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

        private void button_Click(object sender, RoutedEventArgs e) {
            CurrentPdfPageArea.Width = this.Width / 2;
            CurrentPdfPageArea.Height = this.Height;
            CurrentPdfPageArea.HorizontalAlignment = HorizontalAlignment.Left;

            SiblingPdfPageArea.Width = this.Width / 2;
            SiblingPdfPageArea.Height = this.Height;
            SiblingPdfPageArea.HorizontalAlignment = HorizontalAlignment.Right;
        }

        private void prevButt_Click(object sender, RoutedEventArgs e) {
            currentPage--;
            DisplayPdfPageAndPrevious(currentPage);
        }

        private void nextButt_Click(object sender, RoutedEventArgs e) {
            currentPage++;
            DisplayPdfPageAndPrevious(currentPage);
        }
    }
}
