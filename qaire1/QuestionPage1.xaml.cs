using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Text.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace qaire1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class QuestionPage1 : Page
    {
        public QuestionPage1()
        {
            this.InitializeComponent();
        }

        questionStructure mQLayout;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mQLayout = e.Parameter as questionStructure;

            MainPage.eventRecord.Enqueue(String.Format("{0} {1}",
                HighResolutionDateTime.now.ToString(),
                mQLayout.shortName + "_entered"));

            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame.CanGoBack)
            {
                // If we have pages in our in-app backstack and have opted in to showing back, do so
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
            else
            {
                // Remove the UI from the title bar if there are no pages in our in-app back stack
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            }

            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            backout();
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
                return;

            // Navigate back if possible, and if the event has not 
            // already been handled .
            if (rootFrame.CanGoBack && e.Handled == false)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        //call backout things here
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            backout();
            spQInput.Children.Clear();
            base.OnNavigatingFrom(e);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            qpTitle.Text = mQLayout.title;
            qpText.Text = mQLayout.header;
            foreach(var item in mQLayout.body)
            {
                if ((item is ListView) && (item as ListView).Items[0] is CheckBox)
                {
                    foreach (CheckBox cb in (item as ListView).Items)
                    {
                        if (mQLayout.cbData[cb] == "checked")
                        {
                            cb.IsChecked = true;
                        }
                        else
                        {
                            cb.IsChecked = false;
                        }
                        cb.Checked += QuestionPage1_Checked;
                        cb.Unchecked += QuestionPage1_Unchecked;
                    }
                } else if ((item is ListView) && (item as ListView).Items[0] is RadioButton) {
                    foreach (RadioButton cb in (item as ListView).Items)
                    {
                        cb.Checked += QuestionPage1_Checked;
                        cb.Unchecked += QuestionPage1_Unchecked;
                    }

                }
                else if (item is Grid && (item as Grid).Children.Count == 2 &&
                    (item as Grid).Children[0] is InkCanvas &&
                    (item as Grid).Children[1] is Button)
                {
                    ((item as Grid).Children[0] as InkCanvas).GotFocus += QuestionPage1_GotFocus;
                    ((item as Grid).Children[0] as InkCanvas).LostFocus += QuestionPage1_LostFocus;
                    ((item as Grid).Children[0] as InkCanvas).PointerEntered += QuestionPage1_PointerEntered;
                    ((item as Grid).Children[0] as InkCanvas).PointerExited += QuestionPage1_PointerExited;
                    ((item as Grid).Children[0] as InkCanvas).InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
                    ((item as Grid).Children[0] as InkCanvas).InkPresenter.StrokesErased += InkPresenter_StrokesErased;

                    ((item as Grid).Children[1] as Button).Click += QuestionPage1_extend;
                } else if (item is Grid && ((item as Grid).Tag as string) == "likert") {
                    foreach(var cb in (item as Grid).Children)
                    {
                        if (cb is RadioButton)
                        {
                            (cb as RadioButton).Checked += QuestionPage1_Checked;
                            (cb as RadioButton).Unchecked += QuestionPage1_Unchecked;
                        }
                    }
                }

                spQInput.Children.Add(item as UIElement);
            }
        }

        private double step = 200.0;
        private void QuestionPage1_extend(object sender, RoutedEventArgs e)
        {
            //(((e.OriginalSource as Button).Parent as Grid).Children[0] as InkCanvas).Height += 200;
            var curHeight = ((sender as Button).DataContext as Grid).RowDefinitions[0].Height.Value;
            ((sender as Button).DataContext as Grid).RowDefinitions[0].Height = new GridLength(curHeight + step);
            (((sender as Button).DataContext as Grid).Children[0] as InkCanvas).Height += step;

            //scrollViewerInput.ChangeView(null, scrollViewerInput.VerticalOffset + step, null);
        }

        private string currentInkCanvas = "";
        private void QuestionPage1_LostFocus(object sender, RoutedEventArgs e)
        {
            currentInkCanvas += "_left";
        }
        private void QuestionPage1_GotFocus(object sender, RoutedEventArgs e)
        {
            currentInkCanvas = (sender as InkCanvas).Tag as string;
        }
        private void QuestionPage1_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            currentInkCanvas += "_left";
        }
        private void QuestionPage1_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            currentInkCanvas = (sender as InkCanvas).Tag as string;
        }

        private void InkPresenter_StrokesErased(Windows.UI.Input.Inking.InkPresenter sender, Windows.UI.Input.Inking.InkStrokesErasedEventArgs args)
        {
            MainPage.eventRecord.Enqueue(String.Format("{0} {1}",
                HighResolutionDateTime.now.ToString(),
                currentInkCanvas + "_stroke_erased" + args.Strokes.Count.ToString()));
        }

        private void InkPresenter_StrokesCollected(Windows.UI.Input.Inking.InkPresenter sender, Windows.UI.Input.Inking.InkStrokesCollectedEventArgs args)
        {
            MainPage.eventRecord.Enqueue(String.Format("{0} {1}",
                HighResolutionDateTime.now.ToString(),
                currentInkCanvas + "_stroke_collected_" + args.Strokes.Count.ToString()));
        }

        private void QuestionPage1_Unchecked(object sender, RoutedEventArgs e)
        {
            string t = HighResolutionDateTime.now.ToString();
            if (sender is CheckBox)
            {
                MainPage.eventRecord.Enqueue(String.Format("{0} {1}",
                    t,
                    (sender as CheckBox).Tag as string + "_unchecked"));
                mQLayout.cbData[(sender as CheckBox)] = "unchecked";
            } else if (sender is RadioButton)
            {
                MainPage.eventRecord.Enqueue(String.Format("{0} {1}",
                    t,
                    (sender as RadioButton).Tag as string + "_unchecked"));
            }
        }

        private void QuestionPage1_Checked(object sender, RoutedEventArgs e)
        {
            string t = HighResolutionDateTime.now.ToString();
            if (sender is CheckBox)
            {
                MainPage.eventRecord.Enqueue(String.Format("{0} {1}",
                    t,
                    (sender as CheckBox).Tag as string + "_checked"));
                mQLayout.cbData[(sender as CheckBox)] = "checked";
            }
            else if (sender is RadioButton)
            {
                MainPage.eventRecord.Enqueue(String.Format("{0} {1}",
                    t,
                    (sender as RadioButton).Tag as string + "_checked"));
            }
        }

        //I'd rather just generate the BackRequested event, but I can't see how to do it (or if it is allowed?)
        private void qpBack_Click(object sender, RoutedEventArgs e)
        {
            backout();
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
                return;

            rootFrame.GoBack();
        }

        //clear the page
        //TODO: if I figure out how to trigger a BackRequested event, then add this to it
        //otherwise, be sure to call it when leaving the page
        private async void backout()
        {
            //copy the children so they don't get gc'd
            await saveStroke(spQInput.Children);
            await translateText(spQInput.Children);

            spQInput.Children.Clear();
            MainPage.eventRecord.Enqueue(String.Format("{0} {1}",
                HighResolutionDateTime.now.ToString(),
                mQLayout.shortName + "_exited"));
        }

        //this saves the stroke to an image file. It won't overwrite.
        private async Task saveStroke(UIElementCollection allChildren)
        {
            foreach (var item in allChildren)
            {
                if (item is InkCanvas)
                {
                    if ((item as InkCanvas).InkPresenter.StrokeContainer.GetStrokes().Count > 0)
                    {
                        var fil = await KnownFolders.DocumentsLibrary.CreateFileAsync("qaire\\inkcanvas.gif",
                            CreationCollisionOption.GenerateUniqueName);
                        using (IRandomAccessStream stream = await fil.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            await (item as InkCanvas).InkPresenter.StrokeContainer.SaveAsync(stream);
                        }
                    }
                }
            }
        }

        //this runs the handwriting recognition and saves to a text file
        InkRecognizerContainer inkRecognizerContainer;
        private async Task translateText(UIElementCollection allChildren)
        {
            inkRecognizerContainer = new InkRecognizerContainer();
            var recoView = inkRecognizerContainer.GetRecognizers();
            if (recoView.Count > 0)
            {
                SetDefaultRecognizerByCurrentInputMethodLanguageTag();
            }
            else
            {
                return; //nothing to do. you should install a translator
            }

            var fil = await KnownFolders.DocumentsLibrary.CreateFileAsync("qaire\\inkcanvas.dat",
                CreationCollisionOption.GenerateUniqueName);
            try
            {
                for(int i = 0; i < allChildren.Count; ++i)
                {
                    var item = allChildren[i];
                    if (item is InkCanvas)
                    {
                        var inkCanvas = (item as InkCanvas);
                        IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
                        if (currentStrokes.Count > 0)
                        {
                            var recognitionResults = await inkRecognizerContainer.RecognizeAsync(inkCanvas.InkPresenter.StrokeContainer, InkRecognitionTarget.All);

                            if (recognitionResults.Count > 0)
                            {
                                // Display recognition result
                                string str = "#" + HighResolutionDateTime.now.ToString() + " " + mQLayout.shortName + "_" + inkCanvas.Tag + " :";
                                foreach (var r in recognitionResults)
                                {
                                    str += " " + r.GetTextCandidates()[0];
                                }
                                MainPage.eventRecord.Enqueue(str);
                                //using (IRandomAccessStream stream = await fil.OpenAsync(FileAccessMode.ReadWrite))
                                //{
                                //    await FileIO.AppendTextAsync(fil, str);
                                //}

                            }
                        }
                    }
                }
            }
            catch {
            }
        }

        //stolen from msdn sample (Simple Ink)
        private CoreTextServicesManager textServiceManager = null;
        private void SetDefaultRecognizerByCurrentInputMethodLanguageTag()
        {
            //this can be set up to handle different languages (and switching to them)
            textServiceManager = CoreTextServicesManager.GetForCurrentView();

            // Query recognizer name based on current input method language tag (bcp47 tag)
            Language currentInputLanguage = textServiceManager.InputLanguage;

            string recognizerName = RecognizerHelper.LanguageTagToRecognizerName(currentInputLanguage.LanguageTag);

            if (recognizerName != string.Empty)
            {
                var recoView = inkRecognizerContainer.GetRecognizers();
                for (int index = 0; index < recoView.Count; index++)
                {
                    if (recoView[index].Name == recognizerName)
                    {
                        inkRecognizerContainer.SetDefaultRecognizer(recoView[index]);
                        break;
                    }
                }
            }
        }

    }
}
